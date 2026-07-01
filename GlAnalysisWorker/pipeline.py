"""
9-Phase GL Analysis Pipeline

Phase 1  — Mark run as Processing, record StartedAt
Phase 2  — Download blob → temp file
Phase 3  — Normalise (CSV/XLSX → canonical rows)
Phase 4  — Bulk-insert raw transactions, retrieve DB IDs
Phase 5  — Tier-1 statistical detectors (30 detectors)
Phase 6  — Tier-2 ML detectors (7 detectors)
Phase 7  — Tier-3 business rule evaluator
Phase 8  — Tier-4 LLM (Critical+High only, max 100)
Phase 9  — Fuse scores, update transactions, insert anomalies,
            generate executive summary, send notifications
"""
from __future__ import annotations

import asyncio
import json
import logging
import os
from collections import defaultdict
from datetime import datetime

import db
import blob_client
import normalizer
import notifications
from config import APP_BASE_URL
from detectors.base import DetectorHit
from detectors.tier1_group_a import TIER1_GROUP_A
from detectors.tier1_group_b import TIER1_GROUP_B
from detectors.tier2_ml import TIER2_DETECTORS
from detectors.tier3_rules import BusinessRuleEvaluator, load_business_rules_from_db
from detectors.tier4_llm import run_tier4
from score_fusion import fuse
from settings_loader import load_settings

logger = logging.getLogger(__name__)

ALL_TIER1 = TIER1_GROUP_A + TIER1_GROUP_B
ALL_TIER2 = TIER2_DETECTORS

_TIER_DETECTORS = {
    1: ALL_TIER1,
    2: ALL_TIER2,
}


def run_pipeline(payload: dict) -> None:
    """Synchronous entry point called from the Service Bus consumer."""
    run_id: int = payload["runId"]
    user_id: int = payload["userId"]
    blob_path: str = payload["blobPath"]
    file_name: str = payload["fileName"]

    logger.info("=== GL Pipeline start: run #%d user #%d file '%s' ===", run_id, user_id, file_name)
    tmp_path: str | None = None

    try:
        # ── Phase 1: Mark Processing ──────────────────────────────────────────
        db.update_run_started(run_id)
        db.update_run_progress(run_id, 5)

        # ── Phase 2: Download blob ────────────────────────────────────────────
        logger.info("[Phase 2] Downloading blob: %s", blob_path)
        tmp_path = blob_client.download_to_temp(blob_path, file_name)
        db.update_run_progress(run_id, 10)

        # ── Phase 3: Normalise ────────────────────────────────────────────────
        logger.info("[Phase 3] Normalising file: %s", tmp_path)
        rows = normalizer.parse_file(tmp_path)
        if not rows:
            raise ValueError("File produced 0 parseable rows.")
        logger.info("[Phase 3] Parsed %d rows.", len(rows))
        db.update_run_progress(run_id, 20)

        # ── Phase 4: Bulk-insert raw transactions ─────────────────────────────
        logger.info("[Phase 4] Inserting %d raw transactions.", len(rows))
        raw_for_db = _to_db_rows(rows)
        db.bulk_insert_transactions(run_id, raw_for_db)
        db_ids = db.get_transaction_ids(run_id)  # [(id, snapshot), ...]
        logger.info("[Phase 4] Retrieved %d DB IDs.", len(db_ids))
        db.update_run_progress(run_id, 30)

        # Load user settings (for enabled flags, tier weights, thresholds)
        conn = _get_conn_for_settings()
        settings = load_settings(conn, user_id)
        conn.close()

        # ── Phase 5: Tier-1 statistical detectors ────────────────────────────
        logger.info("[Phase 5] Running %d Tier-1 detectors.", len(ALL_TIER1))
        hits_by_row: dict[int, list[DetectorHit]] = defaultdict(list)
        active_t1 = 0
        for det in ALL_TIER1:
            if not det.is_enabled(settings):
                continue
            active_t1 += 1
            try:
                for hit in det.run(rows, settings):
                    hits_by_row[hit.row_index].append(hit)
            except Exception:
                logger.exception("Tier-1 detector '%s' raised", det.name)
        db.update_run_progress(run_id, 45)

        # ── Phase 6: Tier-2 ML detectors ─────────────────────────────────────
        logger.info("[Phase 6] Running %d Tier-2 ML detectors.", len(ALL_TIER2))
        active_t2 = 0
        for det in ALL_TIER2:
            if not det.is_enabled(settings):
                continue
            active_t2 += 1
            try:
                for hit in det.run(rows, settings):
                    hits_by_row[hit.row_index].append(hit)
            except Exception:
                logger.exception("Tier-2 detector '%s' raised", det.name)
        db.update_run_progress(run_id, 60)

        # ── Phase 7: Tier-3 business rules ────────────────────────────────────
        logger.info("[Phase 7] Loading business rules for user #%d.", user_id)
        conn2 = _get_conn_for_settings()
        business_rules = load_business_rules_from_db(conn2, user_id)
        conn2.close()
        active_t3 = len(business_rules)
        if business_rules:
            evaluator = BusinessRuleEvaluator(business_rules)
            try:
                for hit in evaluator.run(rows, settings):
                    hits_by_row[hit.row_index].append(hit)
            except Exception:
                logger.exception("Tier-3 evaluator raised")
        db.update_run_progress(run_id, 70)

        # ── Phase 8: Tier-4 LLM (Critical+High candidates) ───────────────────
        active_t4 = 1 if (settings.get("api_key_anthropic") or settings.get("api_key_openai")) else 0

        # Quick pre-score pass to identify Critical/High candidates for LLM
        pre_fused: dict[int, tuple[int, str]] = {}
        active_counts = {1: active_t1, 2: active_t2, 3: active_t3, 4: 0}
        for row_idx, h_list in hits_by_row.items():
            score, tier = fuse(h_list, settings, active_counts)
            pre_fused[row_idx] = (score, tier)

        flagged_for_llm = [
            {**rows[i], "_row_index": i, "_flags": [h.anomaly_type for h in hits_by_row[i]]}
            for i, (score, tier) in pre_fused.items()
            if tier in ("Critical", "High")
        ]

        llm_hits: list[DetectorHit] = []
        if flagged_for_llm and active_t4:
            logger.info("[Phase 8] Sending %d entries to LLM.", len(flagged_for_llm))
            try:
                llm_hits = asyncio.run(run_tier4(flagged_for_llm, settings))
                for hit in llm_hits:
                    hits_by_row[hit.row_index].append(hit)
            except Exception:
                logger.exception("Tier-4 LLM raised")
        else:
            logger.info("[Phase 8] LLM skipped (%d candidates, active=%d).", len(flagged_for_llm), active_t4)
        db.update_run_progress(run_id, 80)

        # ── Phase 9: Fuse, persist, summarise, notify ─────────────────────────
        logger.info("[Phase 9] Fusing scores for all %d rows.", len(rows))
        active_counts[4] = active_t4

        # Build row_idx → db_id map from the ordered list returned by DB
        row_to_db_id: dict[int, int] = {}
        for pos, (db_id, _snap) in enumerate(db_ids):
            row_to_db_id[pos] = db_id

        score_updates: list[dict] = []
        anomaly_records: list[dict] = []

        for row_idx, row in enumerate(rows):
            h_list = hits_by_row.get(row_idx, [])
            risk_score, risk_tier = fuse(h_list, settings, active_counts)
            composite = risk_score / 100.0

            db_id = row_to_db_id.get(row_idx)
            if db_id is None:
                continue

            # Collect unique anomaly types for the AnomalyFlags JSON column
            anomaly_flags = list({h.anomaly_type for h in h_list})

            # Build a short AI explanation from LLM hits (if any)
            llm_explanations = [
                h.metadata.get("explanation", "")
                for h in h_list if h.anomaly_type == "llm_flagged" and h.metadata.get("explanation")
            ]
            ai_explanation = llm_explanations[0] if llm_explanations else None

            # Risk explanation: grouped by anomaly type
            risk_explanation: dict = {}
            for h in h_list:
                risk_explanation.setdefault(h.anomaly_type, []).extend(h.risk_reasons)

            # Z-score (from ZScoreOutlier hit metadata, if present)
            z_score = next(
                (h.metadata.get("z_score") for h in h_list if h.anomaly_type == "z_score_outlier"),
                None
            )

            score_updates.append({
                "id": db_id,
                "risk_score": risk_score,
                "composite_risk_score": round(composite, 4),
                "risk_tier": risk_tier,
                "anomaly_flags": anomaly_flags,
                "z_score": z_score,
                "ai_explanation": ai_explanation,
                "risk_explanation": risk_explanation if risk_explanation else None,
            })

            # One anomaly record per hit
            for hit in h_list:
                anomaly_records.append({
                    "entry_id": db_id,
                    "anomaly_type": hit.anomaly_type,
                    "detector_score": hit.detector_score,
                    "risk_reasons": hit.risk_reasons,
                    "metadata": hit.metadata,
                })

        db.update_transaction_scores(run_id, score_updates)
        db.bulk_insert_anomalies(run_id, user_id, anomaly_records)
        db.update_run_progress(run_id, 90)

        # Compute summary stats
        scored = score_updates
        total = len(rows)
        flagged_count = sum(1 for s in scored if s["risk_tier"] not in ("Normal", "Low"))
        critical_count = sum(1 for s in scored if s["risk_tier"] == "Critical")
        high_count = sum(1 for s in scored if s["risk_tier"] == "High")
        medium_count = sum(1 for s in scored if s["risk_tier"] == "Medium")
        low_count = sum(1 for s in scored if s["risk_tier"] == "Low")
        normal_count = sum(1 for s in scored if s["risk_tier"] == "Normal")

        risk_scores = [s["risk_score"] for s in scored]
        avg_risk = round(sum(risk_scores) / len(risk_scores), 1) if risk_scores else None

        material_exposure = sum(
            abs(rows[i].get("amount", 0) or 0)
            for i, s in enumerate(scored)
            if s["risk_tier"] in ("Critical", "High")
        )

        dates = [r.get("date") for r in rows if r.get("date")]

        db.update_run_status(
            run_id, "Complete",
            total=total,
            flagged=flagged_count,
            critical=critical_count,
            high=high_count,
            avg_risk=avg_risk,
            period_start=min(dates) if dates else None,
            period_end=max(dates) if dates else None,
            medium_count=medium_count,
            low_count=low_count,
            normal_count=normal_count,
            material_exposure=round(material_exposure, 2),
        )

        # Generate AI executive summary
        if settings.get("api_key_anthropic") or settings.get("api_key_openai"):
            try:
                summary = asyncio.run(_generate_executive_summary(
                    run_id, total, flagged_count, critical_count, high_count,
                    avg_risk, material_exposure, settings
                ))
                db.update_run_executive_summary(run_id, summary)
            except Exception:
                logger.exception("Executive summary generation failed")

        db.update_run_progress(run_id, 100)
        logger.info(
            "=== GL Pipeline complete: run #%d | %d rows | %d flagged | %d critical ===",
            run_id, total, flagged_count, critical_count
        )

        # Notifications
        try:
            notifications.send_completion_alerts(
                run_id, payload["fileName"],
                critical_count, high_count, total, settings
            )
        except Exception:
            logger.exception("Notification failed for run #%d", run_id)

    except Exception as exc:
        logger.exception("GL Pipeline FAILED for run #%d", run_id)
        db.update_run_status(run_id, "Failed", error=str(exc)[:900])
        raise

    finally:
        if tmp_path and os.path.exists(tmp_path):
            os.unlink(tmp_path)


# ── Helpers ────────────────────────────────────────────────────────────────────

def _to_db_rows(rows: list[dict]) -> list[dict]:
    """Convert normalised rows to the format expected by db.bulk_insert_transactions."""
    out = []
    for r in rows:
        out.append({
            "transaction_date": r.get("date"),
            "account_id": r.get("account_id"),
            "account_name": r.get("account_name"),
            "account_type": r.get("account_type"),
            "posting_type": r.get("posting_type"),
            "amount": r.get("amount"),
            "entity_name": r.get("entity_name"),
            "description": r.get("description"),
            "source_type": r.get("source_type"),
            "created_by": r.get("created_by"),
            "created_date": r.get("created_at"),
            "journal_entry_id": r.get("journal_entry_id"),
            "composite_risk_score": None,
            "risk_tier": "pending",
            "anomaly_flags": [],
            "z_score": None,
        })
    return out


def _get_conn_for_settings():
    """Open a plain pyodbc connection for settings/rules lookups."""
    import pyodbc
    from config import SQL_CONNECTION_STRING
    return pyodbc.connect(SQL_CONNECTION_STRING)


async def _generate_executive_summary(
    run_id: int, total: int, flagged: int, critical: int, high: int,
    avg_risk: float | None, material_exposure: float, settings: dict
) -> str:
    import httpx
    provider = settings.get("llm_provider", "anthropic")
    model = settings.get("llm_model") or "claude-sonnet-4-6"
    api_key = settings.get(
        "api_key_anthropic" if provider == "anthropic" else "api_key_openai"
    )
    if not api_key:
        return ""

    prompt = (
        f"Write a 3-sentence CFO-level executive summary of this GL audit:\n"
        f"Total entries: {total}, Flagged: {flagged}, "
        f"Critical: {critical}, High: {high}, "
        f"Avg risk score: {avg_risk or 0}/100, "
        f"Material exposure: ${material_exposure:,.2f}. "
        f"Be concise and use professional financial language."
    )

    try:
        if provider == "anthropic":
            async with httpx.AsyncClient(timeout=30) as client:
                resp = await client.post(
                    "https://api.anthropic.com/v1/messages",
                    headers={"x-api-key": api_key, "anthropic-version": "2023-06-01"},
                    json={
                        "model": model, "max_tokens": 300,
                        "messages": [{"role": "user", "content": prompt}]
                    }
                )
                resp.raise_for_status()
                return resp.json()["content"][0]["text"]

        if provider == "openai":
            async with httpx.AsyncClient(timeout=30) as client:
                resp = await client.post(
                    "https://api.openai.com/v1/chat/completions",
                    headers={"Authorization": f"Bearer {api_key}"},
                    json={
                        "model": model, "max_tokens": 300,
                        "messages": [{"role": "user", "content": prompt}]
                    }
                )
                resp.raise_for_status()
                return resp.json()["choices"][0]["message"]["content"]
    except Exception:
        logger.exception("Executive summary LLM call failed")

    return ""
