"""
Tier-4 LLM — send Critical + High flagged entries to an LLM for narrative explanation.

Limits: max 100 entries per run (cost guard).
Returns structured JSON response with per-entry scores and explanations.
Model: claude-sonnet-4-6 (default), configurable via GL_Settings.
"""
from __future__ import annotations

import json
import logging
from typing import Any

import httpx

from .base import DetectorHit

logger = logging.getLogger(__name__)

MAX_ENTRIES_PER_RUN = 100

_SYSTEM_PROMPT = """You are a certified public accountant specialising in GL audit and fraud detection.
You will receive a batch of journal entries flagged by automated detectors.
For each entry, assess whether it represents a genuine anomaly worth further investigation.

Respond ONLY with valid JSON matching this schema:
{
  "assessments": [
    {
      "entry_index": <int>,    // matches the index in the input list
      "risk_score": <float>,   // 0.0-1.0 your confidence this is anomalous
      "explanation": "<str>",  // 1-2 sentence plain-English explanation
      "action": "<str>"        // "investigate" | "monitor" | "dismiss"
    }
  ]
}

Be conservative: only flag entries where you see a plausible audit concern.
Do not invent information not present in the entry data.
"""


def build_entry_prompt(entries: list[dict]) -> str:
    """Build the user prompt from a list of pre-aggregated entry dicts."""
    lines = []
    for i, e in enumerate(entries):
        lines.append(
            f"[{i}] Date:{e.get('date')} | Account:{e.get('account_name')} | "
            f"Amount:{e.get('amount')} | Entity:{e.get('entity_name')} | "
            f"Desc:{e.get('description')} | Flags:{', '.join(e.get('flags', []))}"
        )
    return "Assess these GL entries:\n" + "\n".join(lines)


async def run_tier4(
    flagged_rows: list[dict],
    settings: dict,
) -> list[DetectorHit]:
    """
    Entry point called from the pipeline.

    flagged_rows: list of dicts (normalised GL rows) that are Critical/High.
    Each dict should include a '_row_index' key (original position in the full rows list).
    """
    if not flagged_rows:
        return []

    provider = settings.get("llm_provider", "anthropic")
    api_key = _pick_key(provider, settings)
    if not api_key:
        logger.info("Tier-4 LLM skipped — no API key configured for provider '%s'.", provider)
        return []

    model = settings.get("llm_model") or "claude-sonnet-4-6"

    # Cap at 100 entries
    batch = flagged_rows[:MAX_ENTRIES_PER_RUN]

    entries_for_prompt = [
        {
            "date": str(r.get("date", "")),
            "account_name": r.get("account_name", ""),
            "amount": r.get("amount", 0),
            "entity_name": r.get("entity_name", ""),
            "description": r.get("description", ""),
            "flags": r.get("_flags", []),
        }
        for r in batch
    ]

    prompt = build_entry_prompt(entries_for_prompt)

    try:
        if provider == "anthropic":
            raw = await _call_anthropic(api_key, model, prompt)
        elif provider == "openai":
            raw = await _call_openai(api_key, model, prompt)
        else:
            logger.warning("Unsupported Tier-4 provider '%s'.", provider)
            return []
    except Exception:
        logger.exception("Tier-4 LLM call failed")
        return []

    try:
        parsed = _parse_response(raw)
    except Exception:
        logger.exception("Tier-4 JSON parse failed. Raw: %s", raw[:500])
        return []

    hits = []
    for assessment in parsed.get("assessments", []):
        entry_index = assessment.get("entry_index")
        if entry_index is None or entry_index >= len(batch):
            continue
        row_idx = batch[entry_index].get("_row_index", entry_index)
        score = float(assessment.get("risk_score", 0))
        explanation = assessment.get("explanation", "")
        action = assessment.get("action", "monitor")

        if score < 0.1:
            continue

        hits.append(DetectorHit(
            row_index=row_idx,
            anomaly_type="llm_flagged",
            detector_score=min(1.0, score),
            risk_reasons=[explanation] if explanation else ["LLM flagged this entry"],
            metadata={
                "action": action,
                "explanation": explanation,
                "model": model,
                "provider": provider,
            }
        ))

    logger.info("Tier-4 LLM processed %d entries, produced %d hits.", len(batch), len(hits))
    return hits


def _pick_key(provider: str, settings: dict) -> str | None:
    mapping = {
        "anthropic": "api_key_anthropic",
        "openai": "api_key_openai",
        "google": "api_key_google",
    }
    return settings.get(mapping.get(provider, ""), None)


async def _call_anthropic(api_key: str, model: str, prompt: str) -> str:
    async with httpx.AsyncClient(timeout=120) as client:
        resp = await client.post(
            "https://api.anthropic.com/v1/messages",
            headers={
                "x-api-key": api_key,
                "anthropic-version": "2023-06-01",
                "content-type": "application/json",
            },
            json={
                "model": model,
                "max_tokens": 4096,
                "system": _SYSTEM_PROMPT,
                "messages": [{"role": "user", "content": prompt}],
            },
        )
        resp.raise_for_status()
        data = resp.json()
        return data["content"][0]["text"]


async def _call_openai(api_key: str, model: str, prompt: str) -> str:
    async with httpx.AsyncClient(timeout=120) as client:
        resp = await client.post(
            "https://api.openai.com/v1/chat/completions",
            headers={
                "Authorization": f"Bearer {api_key}",
                "content-type": "application/json",
            },
            json={
                "model": model,
                "max_tokens": 4096,
                "messages": [
                    {"role": "system", "content": _SYSTEM_PROMPT},
                    {"role": "user", "content": prompt},
                ],
                "response_format": {"type": "json_object"},
            },
        )
        resp.raise_for_status()
        data = resp.json()
        return data["choices"][0]["message"]["content"]


def _parse_response(text: str) -> dict:
    """Extract JSON from LLM response even if wrapped in markdown code fences."""
    text = text.strip()
    if text.startswith("```"):
        lines = text.split("\n")
        # Strip opening ``` line and closing ``` line
        inner = "\n".join(lines[1:-1] if lines[-1].strip() == "```" else lines[1:])
        return json.loads(inner)
    return json.loads(text)
