"""
Tier 1 analysis engine — statistical anomaly detection on normalised GL rows.

Detections:
  ZScoreOutlier     — amount is >2.5σ from its account's mean
  RoundNumber       — amount is a round multiple of 500 and >= 1000
  WeekendPosting    — transaction posted on Saturday or Sunday
  PossibleDuplicate — same amount + same entity within 3 days of another row

Risk scoring (Tier 1 only, 0.0–1.0):
  Base from z-score:  min(abs(z) / 5.0, 0.65)
  Each extra flag:   +0.10 (capped at 1.0)

Risk tiers (from spec):
  Critical  >= 0.85
  High      >= 0.65
  Medium    >= 0.40
  Low       >= 0.15
  Normal    <  0.15
"""
import logging
from collections import defaultdict
from datetime import timedelta
from statistics import mean, stdev

logger = logging.getLogger(__name__)

_Z_THRESHOLD = 2.5
_DUPLICATE_WINDOW_DAYS = 3
_ROUND_MULTIPLE = 500
_ROUND_MIN = 1000.0


def _risk_tier(score: float) -> str:
    if score >= 0.85:
        return "Critical"
    if score >= 0.65:
        return "High"
    if score >= 0.40:
        return "Medium"
    if score >= 0.15:
        return "Low"
    return "Normal"


def _compute_risk_score(z_score: float | None, flags: list[str]) -> float:
    base = min(abs(z_score) / 5.0, 0.65) if z_score is not None else 0.0
    extra = max(0, len(flags) - (1 if z_score and abs(z_score) > _Z_THRESHOLD else 0)) * 0.10
    return min(base + extra, 1.0)


# ---------------------------------------------------------------------------
# Per-account z-score
# ---------------------------------------------------------------------------

def _z_scores_by_account(rows: list[dict]) -> dict[int, float | None]:
    """Return {row_index: z_score} mapping. None when std_dev == 0."""
    by_account: dict[str, list[tuple[int, float]]] = defaultdict(list)
    for i, r in enumerate(rows):
        by_account[r["account_name"]].append((i, float(r["amount"])))

    result: dict[int, float | None] = {}
    for account, entries in by_account.items():
        amounts = [amt for _, amt in entries]
        if len(amounts) < 2:
            for i, _ in entries:
                result[i] = None
            continue
        m = mean(amounts)
        s = stdev(amounts)
        for i, amt in entries:
            result[i] = (amt - m) / s if s > 0 else None
    return result


# ---------------------------------------------------------------------------
# Account stats (for GL_AccountStats table)
# ---------------------------------------------------------------------------

def compute_account_stats(rows: list[dict], z_scores: dict[int, float | None]) -> list[dict]:
    by_account: dict[str, list[tuple[int, float]]] = defaultdict(list)
    for i, r in enumerate(rows):
        by_account[r["account_name"]].append((i, float(r["amount"])))

    stats = []
    for account, entries in by_account.items():
        amounts = [amt for _, amt in entries]
        indices = [i for i, _ in entries]
        outlier_count = sum(
            1 for i in indices
            if z_scores.get(i) is not None and abs(z_scores[i]) > _Z_THRESHOLD
        )
        s = stdev(amounts) if len(amounts) > 1 else 0.0
        stats.append({
            "account_name": account,
            "transaction_count": len(amounts),
            "total_amount": round(sum(amounts), 2),
            "avg_amount": round(mean(amounts), 4),
            "std_dev": round(s, 4),
            "min_amount": round(min(amounts), 2),
            "max_amount": round(max(amounts), 2),
            "outlier_count": outlier_count,
        })
    return stats


# ---------------------------------------------------------------------------
# Duplicate detection
# ---------------------------------------------------------------------------

def _find_duplicates(rows: list[dict]) -> set[int]:
    """Return set of row indices that are likely duplicates of another row."""
    # Group by (amount, entity_name) then check date proximity
    groups: dict[tuple, list[tuple[int, object]]] = defaultdict(list)
    for i, r in enumerate(rows):
        key = (r["amount"], r.get("entity_name") or "")
        groups[key].append((i, r["transaction_date"]))

    duplicate_indices: set[int] = set()
    for entries in groups.values():
        if len(entries) < 2:
            continue
        entries_sorted = sorted(entries, key=lambda x: x[1])
        for j in range(len(entries_sorted)):
            for k in range(j + 1, len(entries_sorted)):
                idx_a, date_a = entries_sorted[j]
                idx_b, date_b = entries_sorted[k]
                if abs((date_b - date_a).days) <= _DUPLICATE_WINDOW_DAYS:
                    duplicate_indices.add(idx_a)
                    duplicate_indices.add(idx_b)
                else:
                    break  # sorted — no further match possible for j

    return duplicate_indices


# ---------------------------------------------------------------------------
# Main entry point
# ---------------------------------------------------------------------------

def analyse(rows: list[dict]) -> tuple[list[dict], list[dict]]:
    """
    Run Tier 1 analysis on normalised GL rows.

    Returns:
        (scored_rows, account_stats)
        scored_rows — same dicts with added keys:
            z_score, anomaly_flags, composite_risk_score, risk_tier
    """
    z_scores = _z_scores_by_account(rows)
    duplicate_indices = _find_duplicates(rows)

    scored: list[dict] = []
    for i, row in enumerate(rows):
        z = z_scores.get(i)
        flags: list[str] = []

        # ZScoreOutlier
        if z is not None and abs(z) > _Z_THRESHOLD:
            flags.append("ZScoreOutlier")

        # RoundNumber
        amt = float(row["amount"])
        if amt >= _ROUND_MIN and amt % _ROUND_MULTIPLE == 0:
            flags.append("RoundNumber")

        # WeekendPosting
        txn_date = row["transaction_date"]
        if txn_date.weekday() >= 5:  # 5 = Saturday, 6 = Sunday
            flags.append("WeekendPosting")

        # PossibleDuplicate
        if i in duplicate_indices:
            flags.append("PossibleDuplicate")

        score = _compute_risk_score(z, flags)
        tier = _risk_tier(score)

        scored.append({
            **row,
            "z_score": round(z, 4) if z is not None else None,
            "anomaly_flags": flags,
            "composite_risk_score": round(score, 4),
            "risk_tier": tier,
        })

    account_stats = compute_account_stats(rows, z_scores)

    flagged = sum(1 for r in scored if r["anomaly_flags"])
    logger.info(
        "Analysis complete: %d rows, %d flagged, %d account groups",
        len(scored), flagged, len(account_stats)
    )
    return scored, account_stats
