"""
Tier-2 ML detectors — 7 detectors.

Feature engineering is shared via detectors/_features.py so IsolationForest,
COPOD and ECOD operate on consistent, richer vectors (parity with the
feat/GL-review TS implementation):

  - IsolationForest : 9-dim matrix, sklearn, with per-feature attribution.
  - COPOD / ECOD    : 8-dim vectors, hand-written empirical-CDF copula
                      (no PyOD dependency — works out of the box).
  - DBSCAN          : 3-dim, sklearn, noise points flagged.
  - AssociationRule / Account- / Entity-behavior : frequency & profiling.

sklearn imports are guarded so a missing package degrades to [] rather than
crashing the pipeline.
"""
from __future__ import annotations

import logging
import math
import statistics as _stats
from collections import defaultdict
from datetime import date as _date

import numpy as np

from .base import Detector, DetectorHit
from ._features import (
    COPOD_ECOD_FEATURES,
    build_copod_ecod_rows,
    build_if_matrix,
    empirical_left_cdf,
    empirical_right_cdf,
    encode_account_type,
)

logger = logging.getLogger(__name__)

_EPSILON = 1e-10


# ── Isolation Forest ──────────────────────────────────────────────────────────

class IsolationForestDetector(Detector):
    """
    sklearn IsolationForest on a 9-dim feature matrix.
    Anomaly score normalised to [0, 1]; entries above SCORE_CUTOFF are flagged,
    with the top deviating features surfaced as human-readable drivers.
    """
    name = "isolation_forest"
    tier = 2
    weight = 1.0
    enabled_flag = "enable_isolation_forest"

    MIN_ENTRIES = 10
    N_ESTIMATORS = 100
    SUBSAMPLE = 256
    SCORE_CUTOFF = 0.55

    _FEATURE_LABEL = {
        "log_amount":            "unusually large amount",
        "amount_to_account_avg": "far above account average",
        "is_month_end":          "month-end timing",
        "is_quarter_end":        "quarter-end timing",
        "is_round_amount":       "round number amount",
        "day_of_week":           "atypical day of week",
        "day_of_month":          "atypical day of month",
        "description_length":    "sparse description",
        "account_type":          "unusual account type",
    }
    _BINARY_GATE = {
        "month-end timing":   ("is_month_end", 1.0),
        "quarter-end timing": ("is_quarter_end", 1.0),
        "round number amount": ("is_round_amount", 1.0),
    }

    def run(self, rows: list[dict], settings: dict) -> list[DetectorHit]:
        if not self.is_enabled(settings):
            return []

        try:
            from sklearn.ensemble import IsolationForest as _IF
        except ImportError:
            logger.warning("scikit-learn not installed — IsolationForestDetector skipped.")
            return []

        X, valid_indices = build_if_matrix(rows)
        if X.shape[0] < self.MIN_ENTRIES:
            return []

        try:
            subsample = min(self.SUBSAMPLE, X.shape[0])
            clf = _IF(n_estimators=self.N_ESTIMATORS, max_samples=subsample, random_state=42)
            clf.fit(X)
            raw = clf.score_samples(X)   # higher = more normal
        except Exception:
            logger.exception("IsolationForest failed")
            return []

        # Normalise to anomaly score in [0, 1] (higher = more anomalous).
        min_s, max_s = float(raw.min()), float(raw.max())
        span = (max_s - min_s) or 1.0
        anomaly = 1.0 - (raw - min_s) / span

        # Per-feature population stats for |z|-based attribution.
        # Column order matches IF_FEATURES (build_if_matrix).
        from ._features import IF_FEATURES
        col_index = {name: i for i, name in enumerate(IF_FEATURES)}
        means = X.mean(axis=0)
        stds = X.std(axis=0)

        hits: list[DetectorHit] = []
        for j, row_idx in enumerate(valid_indices):
            score = float(anomaly[j])
            if score <= self.SCORE_CUTOFF:
                continue

            # Rank features by deviation from population mean.
            deviations = []
            for name in IF_FEATURES:
                c = col_index[name]
                std = stds[c]
                dev = abs(X[j, c] - means[c]) / std if std > 0 else 0.0
                if dev > 1.5:
                    deviations.append((name, dev))
            deviations.sort(key=lambda t: -t[1])

            drivers: list[str] = []
            for name, _dev in deviations[:3]:
                label = self._FEATURE_LABEL.get(name, name.replace("_", " "))
                gate = self._BINARY_GATE.get(label)
                if gate:
                    col, want = gate
                    if X[j, col_index[col]] != want:
                        continue
                drivers.append(label)
            if not drivers:
                drivers = ["multivariate pattern"]

            hits.append(DetectorHit(
                row_index=row_idx,
                anomaly_type=self.name,
                detector_score=round(score, 4),
                risk_reasons=[f"Isolation Forest outlier — {', '.join(drivers)}"],
                metadata={"anomalyScore": round(score, 4), "topDrivers": drivers},
            ))
        return hits


# ── DBSCAN Clustering ─────────────────────────────────────────────────────────

class DbscanDetector(Detector):
    """
    DBSCAN on a 3-dim [amount, day_of_week, account_type] space.
    Noise points (label = -1) that fit no cluster are flagged.
    """
    name = "dbscan"
    tier = 2
    weight = 0.85
    enabled_flag = "enable_dbscan"

    MIN_ENTRIES = 5
    EPS = 0.17           # ~10% of the sqrt(3) diagonal of the unit cube
    NOISE_SCORE = 0.65

    def run(self, rows: list[dict], settings: dict) -> list[DetectorHit]:
        if not self.is_enabled(settings):
            return []

        try:
            from sklearn.cluster import DBSCAN
        except ImportError:
            logger.warning("scikit-learn not installed — DbscanDetector skipped.")
            return []

        amounts = [float(r.get("amount") or 0.0) for r in rows]
        amount_max = max(amounts) or 1.0

        feats = []
        for r, amt in zip(rows, amounts):
            d = r.get("date")
            dow = (d.weekday() / 6) if isinstance(d, _date) else 0.5
            feats.append([
                amt / amount_max,
                dow,
                encode_account_type(r.get("account_type")),
            ])
        if len(feats) < self.MIN_ENTRIES:
            return []

        X = np.array(feats, dtype=np.float64)
        min_pts = max(2, int(len(feats) * 0.03))
        try:
            labels = DBSCAN(eps=self.EPS, min_samples=min_pts).fit_predict(X)
        except Exception:
            logger.exception("DBSCAN failed")
            return []

        hits: list[DetectorHit] = []
        for i, label in enumerate(labels):
            if label == -1:
                hits.append(DetectorHit(
                    row_index=i,
                    anomaly_type=self.name,
                    detector_score=self.NOISE_SCORE,
                    risk_reasons=["DBSCAN: entry is a noise point — does not belong to any transaction cluster"],
                    metadata={"cluster": -1, "isNoise": True},
                ))
        return hits


# ── COPOD (Copula-Based Outlier Detection) ────────────────────────────────────

class CopodDetector(Detector):
    """
    Copula-based outlier detection, implemented from scratch via per-dimension
    empirical CDFs. score = -mean( log(min(leftCdf, rightCdf)) ).
    """
    name = "copod"
    tier = 2
    weight = 0.90
    enabled_flag = "enable_copod"

    MIN_ENTRIES = 15
    SCORE_CUTOFF = 0.82

    def run(self, rows: list[dict], settings: dict) -> list[DetectorHit]:
        if not self.is_enabled(settings):
            return []
        n = len(rows)
        if n < self.MIN_ENTRIES:
            return []

        vectors = build_copod_ecod_rows(rows)
        dims = len(COPOD_ECOD_FEATURES)

        # Per-dimension tail probabilities.
        left_cdfs: list[list[float]] = []
        right_cdfs: list[list[float]] = []
        for feat in COPOD_ECOD_FEATURES:
            col = [v[feat] for v in vectors]
            left = empirical_left_cdf(col)
            left_cdfs.append(left)
            right_cdfs.append(empirical_right_cdf(left))

        raw = [0.0] * n
        top_features: list[list[str]] = [[] for _ in range(n)]
        for i in range(n):
            contributions = []
            acc = 0.0
            for d in range(dims):
                tail = min(left_cdfs[d][i], right_cdfs[d][i])
                acc += math.log(tail + _EPSILON)
                contributions.append((COPOD_ECOD_FEATURES[d], tail))
            raw[i] = -acc / dims
            contributions.sort(key=lambda t: t[1])  # smallest tail = most extreme
            top_features[i] = [contributions[0][0], contributions[1][0]]

        normalised = _min_max(raw)

        hits: list[DetectorHit] = []
        for i in range(n):
            if normalised[i] >= self.SCORE_CUTOFF:
                hits.append(DetectorHit(
                    row_index=i,
                    anomaly_type=self.name,
                    detector_score=round(normalised[i], 4),
                    risk_reasons=[
                        f"COPOD multivariate outlier — extreme on {', '.join(top_features[i])}"
                    ],
                    metadata={"rawCopodScore": round(raw[i], 4), "topFeatures": top_features[i]},
                ))
        return hits


# ── ECOD (Empirical-Cumulative-Distribution Outlier Detection) ─────────────────

class EcodDetector(Detector):
    """
    ECOD via per-dimension empirical CDF tail scores. Aggregates the top-3 most
    extreme dimensions (average of their -log tail scores).
    """
    name = "ecod"
    tier = 2
    weight = 0.90
    enabled_flag = "enable_ecod"

    MIN_ENTRIES = 15
    SCORE_CUTOFF = 0.92
    TOP_DIMS = 3

    def run(self, rows: list[dict], settings: dict) -> list[DetectorHit]:
        if not self.is_enabled(settings):
            return []
        n = len(rows)
        if n < self.MIN_ENTRIES:
            return []

        vectors = build_copod_ecod_rows(rows)
        dims = len(COPOD_ECOD_FEATURES)

        # Per-dimension left/right -log tail scores.
        left_scores: list[list[float]] = [[0.0] * dims for _ in range(n)]
        right_scores: list[list[float]] = [[0.0] * dims for _ in range(n)]
        for d, feat in enumerate(COPOD_ECOD_FEATURES):
            col = [v[feat] for v in vectors]
            left = empirical_left_cdf(col)
            for i in range(n):
                right = 1 - left[i] + 1 / n
                left_scores[i][d] = -math.log(left[i] + _EPSILON)
                right_scores[i][d] = -math.log(right + _EPSILON)

        raw = [0.0] * n
        extreme_dims: list[list[str]] = [[] for _ in range(n)]
        for i in range(n):
            dim_scores = [
                (COPOD_ECOD_FEATURES[d], max(left_scores[i][d], right_scores[i][d]))
                for d in range(dims)
            ]
            dim_scores.sort(key=lambda t: -t[1])
            top = dim_scores[: self.TOP_DIMS]
            raw[i] = sum(s for _, s in top) / self.TOP_DIMS
            extreme_dims[i] = [name for name, _ in top]

        normalised = _min_max(raw)

        hits: list[DetectorHit] = []
        for i in range(n):
            if normalised[i] >= self.SCORE_CUTOFF:
                hits.append(DetectorHit(
                    row_index=i,
                    anomaly_type=self.name,
                    detector_score=round(normalised[i], 4),
                    risk_reasons=[
                        f"ECOD: extreme tail values on {', '.join(extreme_dims[i])}"
                    ],
                    metadata={"rawEcodScore": round(raw[i], 4), "extremeDimensions": extreme_dims[i]},
                ))
        return hits


def _min_max(raw: list[float]) -> list[float]:
    lo, hi = min(raw), max(raw)
    if hi == lo:
        return [0.0] * len(raw)
    span = hi - lo
    return [(v - lo) / span for v in raw]


# ── Association Rule Violation ────────────────────────────────────────────────

class AssociationRuleDetector(Detector):
    """
    Flag rare (account_name, entity_name) pairs that appear with high amounts —
    uncommon combinations of common entities/accounts are suspicious.
    """
    name = "association_rule"
    tier = 2
    weight = 0.75
    enabled_flag = "enable_association_rule"

    def run(self, rows: list[dict], settings: dict) -> list[DetectorHit]:
        if not self.is_enabled(settings):
            return []

        amounts = [r.get("amount", 0) or 0 for r in rows]
        p75 = float(np.percentile(amounts, 75)) if amounts else 0

        pair_counts: dict[tuple, int] = defaultdict(int)
        for row in rows:
            acct = (row.get("account_name") or "").lower()
            entity = (row.get("entity_name") or "").lower()
            if acct and entity:
                pair_counts[(acct, entity)] += 1

        total = len(rows) or 1
        hits = []
        for i, row in enumerate(rows):
            acct = (row.get("account_name") or "").lower()
            entity = (row.get("entity_name") or "").lower()
            if not acct or not entity:
                continue
            freq = pair_counts[(acct, entity)] / total
            amt = row.get("amount", 0) or 0
            if freq < 0.01 and amt > p75:
                score = min(1.0, 0.5 + (1 - freq) * 0.5)
                hits.append(DetectorHit(
                    row_index=i,
                    anomaly_type=self.name,
                    detector_score=round(score, 4),
                    risk_reasons=[
                        f"Rare combination: account '{acct}' × entity '{entity}' "
                        f"(freq {freq:.2%}) with high amount {amt:,.2f}"
                    ],
                    metadata={"account": acct, "entity": entity,
                              "pair_frequency": round(freq, 4), "amount": amt}
                ))
        return hits


# ── Account Behavior Profiling ────────────────────────────────────────────────

class AccountBehaviorProfiling(Detector):
    """
    Per-account monthly transaction profile (mean ± 2 SD).
    Flag months where account volume is outside the historical band.
    """
    name = "account_behavior"
    tier = 2
    weight = 0.85
    enabled_flag = "enable_behavior_profiling"
    _group_field = "account_name"
    _label = "Account"

    def run(self, rows: list[dict], settings: dict) -> list[DetectorHit]:
        if not self.is_enabled(settings):
            return []

        period_data: dict[tuple, list[tuple[float, int]]] = defaultdict(list)
        for i, row in enumerate(rows):
            key_val = (row.get(self._group_field) or "").lower()
            d = row.get("date")
            amt = row.get("amount", 0) or 0
            if not key_val or not isinstance(d, _date):
                continue
            period_data[(key_val, d.year * 12 + d.month)].append((amt, i))

        monthly_totals: dict[str, list[float]] = defaultdict(list)
        for (key_val, _period), entries in period_data.items():
            monthly_totals[key_val].append(sum(e[0] for e in entries))

        hits = []
        for (key_val, _period), entries in period_data.items():
            totals = monthly_totals[key_val]
            if len(totals) < 3:
                continue
            mu = _stats.mean(totals)
            sigma = _stats.stdev(totals)
            if sigma == 0:
                continue
            period_total = sum(e[0] for e in entries)
            z = abs(period_total - mu) / sigma
            if z > 2.0:
                score = min(1.0, 0.5 + z * 0.1)
                for _amt, row_idx in entries:
                    hits.append(DetectorHit(
                        row_index=row_idx,
                        anomaly_type=self.name,
                        detector_score=round(score, 4),
                        risk_reasons=[
                            f"{self._label} '{key_val}': monthly volume {period_total:,.2f} is {z:.1f}σ "
                            f"from historical mean {mu:,.2f}"
                        ],
                        metadata={self._group_field: key_val, "monthly_total": round(period_total, 2),
                                  "z_score": round(z, 2), "historical_mean": round(mu, 2)}
                    ))
        return hits


# ── Entity Behavior Profiling ─────────────────────────────────────────────────

class EntityBehaviorProfiling(AccountBehaviorProfiling):
    """Same monthly-volume profiling as AccountBehaviorProfiling, grouped by entity_name."""
    name = "entity_behavior"
    tier = 2
    weight = 0.80
    enabled_flag = "enable_behavior_profiling"
    _group_field = "entity_name"
    _label = "Entity"


# ── Registry ──────────────────────────────────────────────────────────────────

TIER2_DETECTORS: list[Detector] = [
    IsolationForestDetector(),
    DbscanDetector(),
    CopodDetector(),
    EcodDetector(),
    AssociationRuleDetector(),
    AccountBehaviorProfiling(),
    EntityBehaviorProfiling(),
]
