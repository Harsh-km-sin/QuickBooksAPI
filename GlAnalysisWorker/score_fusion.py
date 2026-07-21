"""
Score fusion — combines per-detector hits from all tiers into a final
integer risk score (0-100) and risk tier string.

Algorithm (matching feat/GL-review branch):
  1. For each tier T with weight wT:
       tier_contribution = wT × (Σ signal_scores / count_active_detectors_in_T)
  2. If a tier has 0 active detectors, redistribute its weight proportionally
     to tiers that have active detectors.
  3. rawScore = min(1.0, Σ tier_contributions × SCORE_AMPLIFIER)
  4. finalRiskScore = round(rawScore × 100)   → integer 0-100
  5. riskTier = threshold lookup from settings

SCORE_AMPLIFIER (3.5) calibration: ensures a single strong Tier-1 signal
(score ~0.9, weight 0.25) produces a final score around 79 ("high") rather
than 22 ("medium"). Without amplification the weighted average stays too low
because only a fraction of detectors fire per entry.
"""
from __future__ import annotations

from detectors.base import DetectorHit

SCORE_AMPLIFIER = 3.5

_TIER_WEIGHT_KEYS = {
    1: "weight_tier1_statistical",
    2: "weight_tier2_ml",
    3: "weight_tier3_rules",
    4: "weight_tier4_llm",
}


def fuse(
    hits: list[DetectorHit],
    settings: dict,
    active_detector_counts: dict[int, int],
) -> tuple[int, str]:
    """
    Compute final (risk_score 0-100, risk_tier) for one entry.

    Parameters
    ----------
    hits:
        All DetectorHit objects fired for this entry (across all tiers).
    settings:
        User's GL_Settings dict (for tier weights and thresholds).
    active_detector_counts:
        {tier: count_of_enabled_detectors_in_tier} — used for per-tier
        normalisation. Tiers with 0 active detectors have their weight
        redistributed.
    """
    # Aggregate raw signal per tier
    tier_signals: dict[int, list[float]] = {1: [], 2: [], 3: [], 4: []}
    for hit in hits:
        tier_signals.setdefault(hit.tier if hasattr(hit, "tier") else _infer_tier(hit), []).append(hit.detector_score)

    # Base weights from settings
    weights = {
        tier: float(settings.get(key, 0.25))
        for tier, key in _TIER_WEIGHT_KEYS.items()
    }

    # Identify tiers with no active detectors
    inactive_tiers = {t for t, count in active_detector_counts.items() if count == 0}
    redistributable = sum(weights[t] for t in inactive_tiers)
    active_tiers = {t for t in weights if t not in inactive_tiers}

    if active_tiers and redistributable > 0:
        active_weight_sum = sum(weights[t] for t in active_tiers)
        for t in active_tiers:
            weights[t] += redistributable * (weights[t] / active_weight_sum)

    # Compute per-tier contribution
    total = 0.0
    for tier, signals in tier_signals.items():
        if not signals:
            continue
        n_active = max(active_detector_counts.get(tier, 1), 1)
        tier_contrib = weights[tier] * (sum(signals) / n_active)
        total += tier_contrib

    raw_score = min(1.0, total * SCORE_AMPLIFIER)
    final_risk_score = round(raw_score * 100)

    risk_tier = _tier_from_score(final_risk_score, settings)
    return final_risk_score, risk_tier


def _tier_from_score(score: int, settings: dict) -> str:
    if score >= settings.get("threshold_critical", 65):
        return "Critical"
    if score >= settings.get("threshold_high", 40):
        return "High"
    if score >= settings.get("threshold_medium", 20):
        return "Medium"
    if score >= settings.get("threshold_low", 8):
        return "Low"
    return "Normal"


def _infer_tier(hit: DetectorHit) -> int:
    """Fallback: infer tier from anomaly_type prefix."""
    t = hit.anomaly_type
    if t.startswith("isolation_forest") or t in (
        "dbscan", "association_rule", "copod", "ecod",
        "account_behavior", "entity_behavior"
    ):
        return 2
    if t == "business_rule_breach":
        return 3
    if t == "llm_flagged":
        return 4
    # Tier-1 statistical detectors (incl. JE-fraud split/reversal) — the default.
    return 1
