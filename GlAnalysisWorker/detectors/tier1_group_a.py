"""
Tier-1 Group A — Statistical & Mathematical detectors (16 of 32)

Each detector is a class that extends Detector and implements run().
All amounts are assumed to be absolute values from the normaliser.
Dates are datetime.date objects (field: "date").
"""
from __future__ import annotations

import math
import statistics
from collections import defaultdict
from datetime import date, timedelta

from .base import Detector, DetectorHit


# ── Z-Score Outlier ────────────────────────────────────────────────────────────

class ZScoreOutlier(Detector):
    """
    Leave-one-out z-score outlier within peer groups.

    Preferred grouping is per-vendor (account_name || entity_name) — "this amount
    is unusual for this vendor in this account". Falls back to per-account when the
    vendor group is too small. Leave-one-out (excluding the entry itself from the
    mean/stddev) prevents a large outlier from masking its own deviation.
    """
    name = "z_score_outlier"
    tier = 1
    weight = 1.0
    enabled_flag = ""  # always enabled

    THRESHOLD = 3.0
    MIN_GROUP_SIZE = 10

    @staticmethod
    def _amount(row: dict) -> float:
        return float(row.get("amount") or 0.0)

    @staticmethod
    def _sample_std(values: list[float], mean: float) -> float:
        if len(values) < 2:
            return 0.0
        variance = sum((v - mean) ** 2 for v in values) / (len(values) - 1)
        return math.sqrt(variance)

    def _score_group(
        self,
        group: list[tuple[int, dict]],
        group_label: str,
        group_level: str,
        hits: list[DetectorHit],
    ) -> None:
        amounts = [self._amount(r) for _i, r in group]
        all_amounts = amounts
        for k, (row_idx, _row) in enumerate(group):
            others = amounts[:k] + amounts[k + 1:]
            if not others:
                continue
            mean = sum(others) / len(others)
            std = self._sample_std(others, mean)
            amt = amounts[k]

            if std == 0:
                # Uniform background — z-score undefined. Use a ratio fallback.
                ratio = amt / mean if mean > 0 else 0.0
                if ratio >= 2.0:
                    z = (ratio - 1) * self.THRESHOLD
                    score = min(1.0, (ratio - 1) / 3)
                elif 0 < ratio <= 0.5:
                    z = (1 / ratio - 1) * self.THRESHOLD
                    score = min(1.0, (1 / ratio - 1) / 3)
                else:
                    continue
            else:
                z = abs(amt - mean) / std
                if z < self.THRESHOLD:
                    continue
                score = min(1.0, z / self.THRESHOLD)

            below = sum(1 for v in all_amounts if v < amt)
            percentile = round(below / len(all_amounts) * 100)
            hits.append(DetectorHit(
                row_index=row_idx,
                anomaly_type=self.name,
                detector_score=round(score, 4),
                risk_reasons=[
                    f"Amount {amt:,.2f} is {z:.1f}σ from {group_label} mean {mean:,.2f} "
                    f"(n={len(group)})"
                ],
                metadata={
                    "z_score": round(z, 2),
                    "group_mean": round(mean, 2),
                    "group_std": round(std, 2),
                    "sample_size": len(group),
                    "percentile": percentile,
                    "group_level": group_level,
                    "comparison_group_label": group_label,
                },
            ))

    def run(self, rows: list[dict], settings: dict) -> list[DetectorHit]:
        vendor_groups: dict[str, list[tuple[int, dict]]] = defaultdict(list)
        account_groups: dict[str, list[tuple[int, dict]]] = defaultdict(list)

        for i, row in enumerate(rows):
            if row.get("amount") is None:
                continue
            account = (row.get("account_name") or "__unknown__").strip()
            party = (row.get("entity_name") or "").strip().lower()
            if party and party not in ("unknown", "n/a", "-"):
                vendor_groups[f"{account}||{party}"].append((i, row))
            account_groups[account].append((i, row))

        hits: list[DetectorHit] = []
        covered: set[int] = set()

        # Pass 1: vendor-level where the group is large enough.
        for vendor_key, group in vendor_groups.items():
            if len(group) < self.MIN_GROUP_SIZE:
                continue
            account, party = vendor_key.split("||", 1)
            self._score_group(group, f"{account} — {party}", "vendor", hits)
            for idx, _row in group:
                covered.add(idx)

        # Pass 2: account-level fallback for entries not covered by a vendor group.
        for account, group in account_groups.items():
            fallback = [(i, r) for (i, r) in group if i not in covered]
            if len(fallback) < self.MIN_GROUP_SIZE:
                continue
            self._score_group(fallback, f"all entries in {account}", "account", hits)

        return hits


# ── Benford's Law ─────────────────────────────────────────────────────────────

class BenfordLaw(Detector):
    """
    Chi-square test of first-digit frequency against Benford distribution.
    Fires a per-entry hit for the most over-represented leading digit.
    """
    name = "benford_law"
    tier = 1
    weight = 0.8
    enabled_flag = "enable_benford_law"

    _EXPECTED = {d: math.log10(1 + 1 / d) for d in range(1, 10)}

    def run(self, rows: list[dict], settings: dict) -> list[DetectorHit]:
        if not self.is_enabled(settings):
            return []

        # Collect first-digit frequency for ALL entries
        digit_rows: dict[int, list[int]] = defaultdict(list)
        for i, row in enumerate(rows):
            amt = row.get("amount")
            if not amt or amt <= 0:
                continue
            d = int(str(abs(amt)).lstrip("0")[0])
            digit_rows[d].append(i)

        total = sum(len(v) for v in digit_rows.values())
        if total < 50:  # not enough data for Benford analysis
            return []

        observed_freq = {d: len(digit_rows[d]) / total for d in range(1, 10)}

        # Chi-square contribution per digit
        chi_sq: dict[int, float] = {}
        for d in range(1, 10):
            obs = digit_rows.get(d, [])
            expected_count = total * self._EXPECTED[d]
            if expected_count > 0:
                chi_sq[d] = ((len(obs) - expected_count) ** 2) / expected_count

        total_chi = sum(chi_sq.values())
        # Chi-sq critical value at 8 df, p<0.05 is 15.51; p<0.01 is 20.09
        if total_chi < 15.51:
            return []

        score = min(1.0, (total_chi - 15.51) / 84.49)  # normalise [0,1]

        hits = []
        for d, chi in sorted(chi_sq.items(), key=lambda x: -x[1]):
            if chi < 2.0:
                continue
            obs_pct = round(observed_freq.get(d, 0) * 100, 1)
            exp_pct = round(self._EXPECTED[d] * 100, 1)
            for row_i in digit_rows.get(d, []):
                hits.append(DetectorHit(
                    row_index=row_i,
                    anomaly_type=self.name,
                    detector_score=round(score, 4),
                    risk_reasons=[
                        f"Leading digit {d}: observed {obs_pct}% vs expected {exp_pct}% (Benford's Law)"
                    ],
                    metadata={"leading_digit": d, "chi_square": round(total_chi, 2),
                              "observed_pct": obs_pct, "expected_pct": exp_pct}
                ))
        return hits


# ── Round Number ──────────────────────────────────────────────────────────────

class RoundNumber(Detector):
    """Flag entries with suspiciously round amounts (multiples of 100, 1000…)."""
    name = "round_number"
    tier = 1
    weight = 0.7
    enabled_flag = "enable_round_number"

    def run(self, rows: list[dict], settings: dict) -> list[DetectorHit]:
        if not self.is_enabled(settings):
            return []

        hits = []
        for i, row in enumerate(rows):
            amt = row.get("amount", 0) or 0
            if amt <= 0:
                continue
            score = self._round_score(amt)
            if score > 0:
                hits.append(DetectorHit(
                    row_index=i,
                    anomaly_type=self.name,
                    detector_score=score,
                    risk_reasons=[f"Amount {amt:,.2f} is a suspiciously round number"],
                    metadata={"amount": amt}
                ))
        return hits

    @staticmethod
    def _round_score(amt: float) -> float:
        if amt == 0:
            return 0.0
        if amt % 10_000 == 0:
            return 0.9
        if amt % 1_000 == 0:
            return 0.75
        if amt % 100 == 0:
            return 0.6
        if amt % 50 == 0:
            return 0.45
        if amt % 10 == 0 and amt > 500:
            return 0.3
        return 0.0


# ── Large Unusual Amount ──────────────────────────────────────────────────────

class LargeUnusualAmount(Detector):
    """Flag amounts in the top 1% of the dataset."""
    name = "large_unusual_amount"
    tier = 1
    weight = 0.85

    def run(self, rows: list[dict], settings: dict) -> list[DetectorHit]:
        amounts = sorted(r["amount"] for r in rows if r.get("amount") and r["amount"] > 0)
        if len(amounts) < 10:
            return []

        p99 = amounts[int(len(amounts) * 0.99)]
        p995 = amounts[int(len(amounts) * 0.995)]

        hits = []
        for i, row in enumerate(rows):
            amt = row.get("amount", 0) or 0
            if amt >= p99:
                score = 0.7 if amt < p995 else 0.9
                hits.append(DetectorHit(
                    row_index=i,
                    anomaly_type=self.name,
                    detector_score=score,
                    risk_reasons=[f"Amount {amt:,.2f} is in the top 1% of this dataset (≥{p99:,.2f})"],
                    metadata={"amount": amt, "p99": round(p99, 2)}
                ))
        return hits


# ── Threshold Breach ──────────────────────────────────────────────────────────

class ThresholdBreach(Detector):
    """
    Flag amounts that cross account-type thresholds.
    Thresholds from RelatedPartyList / account config in settings.
    Falls back to a generic $50,000 threshold.
    """
    name = "threshold_breach"
    tier = 1
    weight = 0.9
    enabled_flag = "enable_threshold_breach"

    _DEFAULT_THRESHOLD = 50_000.0
    _ACCOUNT_THRESHOLDS: dict[str, float] = {
        "petty cash": 500.0,
        "cash": 10_000.0,
        "prepaid": 5_000.0,
        "travel": 2_000.0,
        "entertainment": 1_000.0,
    }

    def run(self, rows: list[dict], settings: dict) -> list[DetectorHit]:
        if not self.is_enabled(settings):
            return []

        hits = []
        for i, row in enumerate(rows):
            amt = row.get("amount", 0) or 0
            acct = (row.get("account_name") or "").lower()
            threshold = self._DEFAULT_THRESHOLD
            for keyword, t in self._ACCOUNT_THRESHOLDS.items():
                if keyword in acct:
                    threshold = t
                    break

            if amt > threshold:
                score = min(1.0, 0.6 + 0.3 * ((amt - threshold) / threshold))
                hits.append(DetectorHit(
                    row_index=i,
                    anomaly_type=self.name,
                    detector_score=round(score, 4),
                    risk_reasons=[f"Amount {amt:,.2f} exceeds threshold {threshold:,.2f} for account '{acct}'"],
                    metadata={"amount": amt, "threshold": threshold, "account": acct}
                ))
        return hits


# ── Weekend Posting ───────────────────────────────────────────────────────────

class WeekendPosting(Detector):
    """Flag entries posted on Saturday or Sunday."""
    name = "weekend_posting"
    tier = 1
    weight = 0.6

    def run(self, rows: list[dict], settings: dict) -> list[DetectorHit]:
        hits = []
        for i, row in enumerate(rows):
            d = row.get("date")
            if d is None:
                continue
            if isinstance(d, date) and d.weekday() >= 5:  # 5=Sat, 6=Sun
                hits.append(DetectorHit(
                    row_index=i,
                    anomaly_type=self.name,
                    detector_score=0.55,
                    risk_reasons=[f"Entry posted on {d.strftime('%A')} ({d})"],
                    metadata={"day_of_week": d.strftime("%A")}
                ))
        return hits


# ── Backdating ────────────────────────────────────────────────────────────────

class Backdating(Detector):
    """Flag entries where transaction date is >30 days before the file's max date."""
    name = "backdating"
    tier = 1
    weight = 0.8
    enabled_flag = "enable_backdating"

    DAYS_THRESHOLD = 30

    def run(self, rows: list[dict], settings: dict) -> list[DetectorHit]:
        if not self.is_enabled(settings):
            return []

        dates = [r["date"] for r in rows if isinstance(r.get("date"), date)]
        if not dates:
            return []

        max_date = max(dates)

        hits = []
        for i, row in enumerate(rows):
            d = row.get("date")
            if not isinstance(d, date):
                continue
            delta = (max_date - d).days
            if delta > self.DAYS_THRESHOLD:
                score = min(1.0, 0.5 + delta / 365)
                hits.append(DetectorHit(
                    row_index=i,
                    anomaly_type=self.name,
                    detector_score=round(score, 4),
                    risk_reasons=[f"Entry dated {d} is {delta} days before last entry in the file ({max_date})"],
                    metadata={"transaction_date": str(d), "max_date": str(max_date), "days_lag": delta}
                ))
        return hits


# ── Period-End Cluster ────────────────────────────────────────────────────────

class PeriodEndCluster(Detector):
    """
    Flag unusual clustering of entries at month/quarter end (last 3 days).
    Flags individual entries that are in the cluster if the cluster count
    exceeds 30% of monthly volume.
    """
    name = "period_end_cluster"
    tier = 1
    weight = 0.65
    enabled_flag = "enable_period_end_cluster"

    def run(self, rows: list[dict], settings: dict) -> list[DetectorHit]:
        if not self.is_enabled(settings):
            return []

        month_counts: dict[str, int] = defaultdict(int)
        period_end_indices: dict[str, list[int]] = defaultdict(list)

        for i, row in enumerate(rows):
            d = row.get("date")
            if not isinstance(d, date):
                continue
            key = d.strftime("%Y-%m")
            month_counts[key] += 1
            # last 3 calendar days of any month
            next_month_first = (d.replace(day=28) + timedelta(days=4)).replace(day=1)
            month_last = next_month_first - timedelta(days=1)
            if (month_last - d).days < 3:
                period_end_indices[key].append(i)

        hits = []
        for key, indices in period_end_indices.items():
            total = month_counts[key]
            if total == 0:
                continue
            ratio = len(indices) / total
            if ratio > 0.30:
                score = min(1.0, 0.5 + ratio)
                for i in indices:
                    hits.append(DetectorHit(
                        row_index=i,
                        anomaly_type=self.name,
                        detector_score=round(score, 4),
                        risk_reasons=[
                            f"{len(indices)} of {total} entries ({ratio:.0%}) in {key} are concentrated at period end"
                        ],
                        metadata={"period": key, "cluster_count": len(indices),
                                  "total_in_period": total, "ratio": round(ratio, 3)}
                    ))
        return hits


# ── Near Duplicate ────────────────────────────────────────────────────────────

class NearDuplicate(Detector):
    """
    Flag pairs of entries that share the same amount + account + date.
    Exact duplicates (same JE id) are excluded.
    """
    name = "near_duplicate"
    tier = 1
    weight = 0.85
    enabled_flag = "enable_near_duplicates"

    def run(self, rows: list[dict], settings: dict) -> list[DetectorHit]:
        if not self.is_enabled(settings):
            return []

        key_to_indices: dict[tuple, list[int]] = defaultdict(list)
        for i, row in enumerate(rows):
            k = (
                round(row.get("amount", 0) or 0, 2),
                (row.get("account_name") or "").lower(),
                str(row.get("date") or ""),
            )
            key_to_indices[k].append(i)

        hits = []
        for k, indices in key_to_indices.items():
            if len(indices) < 2:
                continue
            # Check that at least two have different JE ids (not the same entry)
            je_ids = set()
            for i in indices:
                je_id = rows[i].get("journal_entry_id") or ""
                je_ids.add(je_id)
            if len(je_ids) < 2:
                continue

            for i in indices:
                hits.append(DetectorHit(
                    row_index=i,
                    anomaly_type=self.name,
                    detector_score=0.8,
                    risk_reasons=[f"Entry appears {len(indices)}× with same amount/account/date"],
                    metadata={"duplicate_count": len(indices), "amount": k[0],
                              "account": k[1], "date": k[2]}
                ))
        return hits


# ── Sequential Journal Entry IDs with Round Amounts ───────────────────────────

class SequentialRoundJournalEntries(Detector):
    """
    Flag cases where 3+ sequential JE IDs all have round amounts —
    a pattern consistent with manufactured or inflated entries.
    """
    name = "sequential_round_je"
    tier = 1
    weight = 0.7

    def run(self, rows: list[dict], settings: dict) -> list[DetectorHit]:
        numeric_je: list[tuple[int, int, float]] = []
        for i, row in enumerate(rows):
            je = row.get("journal_entry_id") or ""
            try:
                je_num = int(je.strip().lstrip("#"))
            except (ValueError, AttributeError):
                continue
            amt = row.get("amount", 0) or 0
            numeric_je.append((i, je_num, amt))

        numeric_je.sort(key=lambda x: x[1])

        flagged = set()
        run_start = 0
        while run_start < len(numeric_je):
            run_end = run_start + 1
            while (run_end < len(numeric_je) and
                   numeric_je[run_end][1] == numeric_je[run_end - 1][1] + 1):
                run_end += 1
            run = numeric_je[run_start:run_end]
            if len(run) >= 3:
                round_in_run = [x for x in run if x[2] % 100 == 0 and x[2] > 0]
                if len(round_in_run) >= 3:
                    for item in round_in_run:
                        flagged.add(item[0])
            run_start = run_end

        hits = []
        for i in flagged:
            hits.append(DetectorHit(
                row_index=i,
                anomaly_type=self.name,
                detector_score=0.65,
                risk_reasons=["3+ sequential journal entry IDs all have round amounts — possible fabrication"],
                metadata={}
            ))
        return hits


# ── Excessive Adjustments ─────────────────────────────────────────────────────

class ExcessiveAdjustments(Detector):
    """
    Flag accounts with >5 adjusting entries (description contains 'adj' or 'correct')
    within the dataset.
    """
    name = "excessive_adjustments"
    tier = 1
    weight = 0.65

    _KEYWORDS = ("adj", "adjust", "correct", "correction", "reclass", "error",
                 "reverse", "reclassify")

    def run(self, rows: list[dict], settings: dict) -> list[DetectorHit]:
        account_adj_indices: dict[str, list[int]] = defaultdict(list)

        for i, row in enumerate(rows):
            desc = (row.get("description") or "").lower()
            if any(kw in desc for kw in self._KEYWORDS):
                acct = (row.get("account_name") or "unknown").lower()
                account_adj_indices[acct].append(i)

        hits = []
        for acct, indices in account_adj_indices.items():
            if len(indices) > 5:
                score = min(1.0, 0.4 + len(indices) * 0.04)
                for i in indices:
                    hits.append(DetectorHit(
                        row_index=i,
                        anomaly_type=self.name,
                        detector_score=round(score, 4),
                        risk_reasons=[f"Account '{acct}' has {len(indices)} adjustment entries"],
                        metadata={"account": acct, "adjustment_count": len(indices)}
                    ))
        return hits


# ── Split Transaction ─────────────────────────────────────────────────────────

class SplitTransaction(Detector):
    """
    Split-avoidance detection: 2–6 same-vendor entries inside a short window whose
    amounts each fall below an approval threshold but which *sum* to just over it —
    a classic pattern for splitting an invoice to dodge approval limits.
    """
    name = "split_transaction"
    tier = 1
    weight = 0.75

    WINDOW_DAYS = 7
    MIN_GROUP = 2
    MAX_GROUP = 6
    MAX_EXCESS_RATIO = 1.5      # sum must land in (threshold, threshold × 1.5)
    SCORE_PAIR = 0.70
    SCORE_GROUP = 0.85
    DEFAULT_THRESHOLD = 99_000.0

    def run(self, rows: list[dict], settings: dict) -> list[DetectorHit]:
        threshold = float(settings.get("split_approval_threshold", self.DEFAULT_THRESHOLD))

        groups: dict[tuple, list[int]] = defaultdict(list)
        for i, row in enumerate(rows):
            entity = (row.get("entity_name") or "").lower()
            account = (row.get("account_name") or "").lower()
            if entity and isinstance(row.get("date"), date):
                groups[(account, entity)].append(i)

        best: dict[int, tuple[float, dict]] = {}  # row_idx → (score, metadata)

        for indices in groups.values():
            ordered = sorted(indices, key=lambda i: rows[i]["date"])
            for s in range(len(ordered)):
                anchor_date = rows[ordered[s]]["date"]
                window: list[int] = []
                for j in range(s, len(ordered)):
                    if (rows[ordered[j]]["date"] - anchor_date).days > self.WINDOW_DAYS:
                        break
                    amt = rows[ordered[j]].get("amount", 0) or 0
                    if amt >= threshold:
                        continue  # already at the limit on its own — not avoidance
                    window.append(ordered[j])
                    if len(window) > self.MAX_GROUP:
                        break

                if len(window) < self.MIN_GROUP:
                    continue
                self._flag_combinations(window, rows, threshold, best)

        hits: list[DetectorHit] = []
        for row_idx, (score, meta) in best.items():
            hits.append(DetectorHit(
                row_index=row_idx,
                anomaly_type=self.name,
                detector_score=score,
                risk_reasons=[
                    f"{len(meta['related_entries'])} entries sum to {meta['combined_amount']:,.2f}, "
                    f"just over the {threshold:,.0f} approval threshold — possible split to avoid approval"
                ],
                metadata=meta,
            ))
        return hits

    def _flag_combinations(
        self,
        window: list[int],
        rows: list[dict],
        threshold: float,
        best: dict[int, tuple[float, dict]],
    ) -> None:
        n = len(window)
        combo: list[int] = []

        def recurse(start: int, size: int) -> None:
            if len(combo) == size:
                total = sum(rows[i].get("amount", 0) or 0 for i in combo)
                if threshold < total < threshold * self.MAX_EXCESS_RATIO:
                    score = self.SCORE_GROUP if size >= 3 else self.SCORE_PAIR
                    meta = {
                        "pattern": "split_avoidance",
                        "related_entries": list(combo),
                        "combined_amount": round(total, 2),
                        "threshold": threshold,
                        "window_days": self.WINDOW_DAYS,
                    }
                    for i in combo:
                        if i not in best or best[i][0] < score:
                            best[i] = (score, meta)
                return
            for k in range(start, n - (size - len(combo)) + 1):
                combo.append(window[k])
                recurse(k + 1, size)
                combo.pop()

        for size in range(self.MIN_GROUP, min(self.MAX_GROUP, n) + 1):
            recurse(0, size)


class Reversal(Detector):
    """
    Flag opposing debit/credit entries in the same account, equal in amount
    (within 0.1%) and close in time (≤15 days). Reversals straddling a period
    boundary score higher — a hallmark of window-dressing / earnings management.
    """
    name = "reversal"
    tier = 1
    weight = 0.7

    WINDOW_DAYS = 15
    AMOUNT_TOLERANCE = 0.001     # 0.1%
    PERIOD_DAYS = 5
    SCORE_BOUNDARY = 0.80
    SCORE_OTHER = 0.65

    @staticmethod
    def _is_debit(row: dict) -> bool:
        return (row.get("posting_type") or "").lower() == "debit"

    def _near_period_boundary(self, d: date) -> bool:
        # Year-end / start
        if d.month == 12 and d.day == 31:
            return True
        if d.month == 1 and d.day <= 3:
            return True
        # Quarter-end
        for m, max_day in ((3, 31), (6, 30), (9, 30), (12, 31)):
            if d.month == m and d.day >= max_day - 1:
                return True
        # Month-end / month-start
        next_first = (d.replace(day=28) + timedelta(days=4)).replace(day=1)
        days_in_month = (next_first - timedelta(days=1)).day
        return d.day >= days_in_month - self.PERIOD_DAYS or d.day <= self.PERIOD_DAYS

    def run(self, rows: list[dict], settings: dict) -> list[DetectorHit]:
        account_groups: dict[str, list[int]] = defaultdict(list)
        for i, row in enumerate(rows):
            if isinstance(row.get("date"), date) and (row.get("amount") or 0) > 0:
                account_groups[(row.get("account_name") or "__unknown__").lower()].append(i)

        best: dict[int, tuple[float, dict]] = {}
        for indices in account_groups.values():
            ordered = sorted(indices, key=lambda i: rows[i]["date"])
            for a in range(len(ordered)):
                ra = rows[ordered[a]]
                amt_a = ra.get("amount", 0) or 0
                debit_a = self._is_debit(ra)
                for b in range(a + 1, len(ordered)):
                    rb = rows[ordered[b]]
                    if (rb["date"] - ra["date"]).days > self.WINDOW_DAYS:
                        break
                    if self._is_debit(rb) == debit_a:
                        continue  # must be opposite directions
                    amt_b = rb.get("amount", 0) or 0
                    larger = max(amt_a, amt_b)
                    smaller = min(amt_a, amt_b)
                    if larger == 0 or (larger - smaller) / larger > self.AMOUNT_TOLERANCE:
                        continue
                    at_boundary = self._near_period_boundary(ra["date"]) or self._near_period_boundary(rb["date"])
                    score = self.SCORE_BOUNDARY if at_boundary else self.SCORE_OTHER
                    meta = {
                        "pattern": "reversal",
                        "related_entries": [ordered[a], ordered[b]],
                        "reversal_amount": round(amt_a, 2),
                        "is_period_boundary": at_boundary,
                        "window_days": self.WINDOW_DAYS,
                    }
                    for idx in (ordered[a], ordered[b]):
                        if idx not in best or best[idx][0] < score:
                            best[idx] = (score, meta)

        hits: list[DetectorHit] = []
        for row_idx, (score, meta) in best.items():
            boundary_note = " across a period boundary" if meta["is_period_boundary"] else ""
            hits.append(DetectorHit(
                row_index=row_idx,
                anomaly_type=self.name,
                detector_score=score,
                risk_reasons=[
                    f"Reversing debit/credit of {meta['reversal_amount']:,.2f} within "
                    f"{self.WINDOW_DAYS} days{boundary_note}"
                ],
                metadata=meta,
            ))
        return hits


# ── Prior Period Adjustment ───────────────────────────────────────────────────

class PriorPeriodAdjustment(Detector):
    """
    Flag entries whose year is more than 1 year prior to the most recent year
    in the dataset — these are late prior-period entries.
    """
    name = "prior_period_adjustment"
    tier = 1
    weight = 0.65
    enabled_flag = "enable_backdating"

    def run(self, rows: list[dict], settings: dict) -> list[DetectorHit]:
        if not self.is_enabled(settings):
            return []

        years = [r["date"].year for r in rows if isinstance(r.get("date"), date)]
        if not years:
            return []
        max_year = max(years)

        hits = []
        for i, row in enumerate(rows):
            d = row.get("date")
            if not isinstance(d, date):
                continue
            if max_year - d.year > 1:
                hits.append(DetectorHit(
                    row_index=i,
                    anomaly_type=self.name,
                    detector_score=0.70,
                    risk_reasons=[f"Entry dated {d} is a prior-period adjustment ({max_year - d.year} years ago)"],
                    metadata={"date": str(d), "max_year": max_year, "years_prior": max_year - d.year}
                ))
        return hits


# ── Unusual Hour Posting ──────────────────────────────────────────────────────

class UnusualHourPosting(Detector):
    """
    Flag entries posted between midnight and 05:00 (if timestamps are available).
    Field: 'created_at' datetime, or skip if not present.
    """
    name = "unusual_hour_posting"
    tier = 1
    weight = 0.55

    def run(self, rows: list[dict], settings: dict) -> list[DetectorHit]:
        from datetime import datetime
        hits = []
        for i, row in enumerate(rows):
            ts = row.get("created_at")
            if ts is None:
                continue
            if isinstance(ts, str):
                try:
                    ts = datetime.fromisoformat(ts)
                except ValueError:
                    continue
            if hasattr(ts, "hour") and ts.hour < 5:
                hits.append(DetectorHit(
                    row_index=i,
                    anomaly_type=self.name,
                    detector_score=0.60,
                    risk_reasons=[f"Entry created at {ts.strftime('%H:%M')} — outside normal business hours"],
                    metadata={"created_at": str(ts), "hour": ts.hour}
                ))
        return hits


# ── Missing Description ───────────────────────────────────────────────────────

class MissingDescription(Detector):
    """Flag entries with no description or a description shorter than 5 chars."""
    name = "missing_description"
    tier = 1
    weight = 0.50

    def run(self, rows: list[dict], settings: dict) -> list[DetectorHit]:
        hits = []
        for i, row in enumerate(rows):
            desc = (row.get("description") or "").strip()
            if len(desc) < 5:
                hits.append(DetectorHit(
                    row_index=i,
                    anomaly_type=self.name,
                    detector_score=0.50,
                    risk_reasons=["Entry has no or very short description — lacks audit trail"],
                    metadata={"description_length": len(desc)}
                ))
        return hits


# ── Excessive Memo Repetition ─────────────────────────────────────────────────

class ExcessiveMemoRepetition(Detector):
    """
    Flag entries whose exact description appears more than 20 times.
    Real business entries rarely have dozens of identical memos.
    """
    name = "excessive_memo_repetition"
    tier = 1
    weight = 0.55

    THRESHOLD = 20

    def run(self, rows: list[dict], settings: dict) -> list[DetectorHit]:
        memo_counts: dict[str, list[int]] = defaultdict(list)
        for i, row in enumerate(rows):
            desc = (row.get("description") or "").strip().lower()
            if len(desc) >= 5:
                memo_counts[desc].append(i)

        hits = []
        for desc, indices in memo_counts.items():
            if len(indices) > self.THRESHOLD:
                score = min(1.0, 0.4 + len(indices) / 200)
                for i in indices:
                    hits.append(DetectorHit(
                        row_index=i,
                        anomaly_type=self.name,
                        detector_score=round(score, 4),
                        risk_reasons=[f"Memo '{desc[:60]}' repeated {len(indices)} times"],
                        metadata={"memo": desc[:100], "repetition_count": len(indices)}
                    ))
        return hits


# ── Registry ──────────────────────────────────────────────────────────────────

TIER1_GROUP_A: list[Detector] = [
    ZScoreOutlier(),
    BenfordLaw(),
    RoundNumber(),
    LargeUnusualAmount(),
    ThresholdBreach(),
    WeekendPosting(),
    Backdating(),
    PeriodEndCluster(),
    NearDuplicate(),
    SequentialRoundJournalEntries(),
    ExcessiveAdjustments(),
    SplitTransaction(),
    Reversal(),
    PriorPeriodAdjustment(),
    UnusualHourPosting(),
    MissingDescription(),
    ExcessiveMemoRepetition(),
]
