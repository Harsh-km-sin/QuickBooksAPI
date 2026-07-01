"""
Tier-1 Group B — Pattern & Rule-Based statistical detectors (16 of 32)

Relies only on Python stdlib + statistics module.
"""
from __future__ import annotations

import json
import re
import statistics
from collections import defaultdict
from datetime import date

from .base import Detector, DetectorHit


# ── Fraud Pattern Keywords ─────────────────────────────────────────────────────

class FraudPatternKeywords(Detector):
    """
    Scan description and entity name for known fraud-scheme keywords.
    Covers ghost vendor, fabricated invoices, payroll fraud, etc.
    """
    name = "fraud_pattern_keywords"
    tier = 1
    weight = 0.85
    enabled_flag = "enable_fraud_patterns"

    _HIGH_RISK = (
        "fictitious", "ghost", "shell", "dummy", "fake", "fabricated",
        "unauthorized", "unapproved", "kickback", "bribe", "embezzle",
        "fraudulent", "forged",
    )
    _MEDIUM_RISK = (
        "personal", "cash advance", "petty cash", "misc", "miscellaneous",
        "other expense", "n/a", "tbd", "temp", "test",
    )

    def run(self, rows: list[dict], settings: dict) -> list[DetectorHit]:
        if not self.is_enabled(settings):
            return []

        hits = []
        for i, row in enumerate(rows):
            text = " ".join([
                (row.get("description") or ""),
                (row.get("entity_name") or ""),
                (row.get("account_name") or ""),
            ]).lower()

            matched_high = [kw for kw in self._HIGH_RISK if kw in text]
            matched_med = [kw for kw in self._MEDIUM_RISK if kw in text]

            if matched_high:
                hits.append(DetectorHit(
                    row_index=i,
                    anomaly_type=self.name,
                    detector_score=0.90,
                    risk_reasons=[f"High-risk fraud keywords found: {', '.join(matched_high)}"],
                    metadata={"keywords": matched_high, "risk_level": "high"}
                ))
            elif matched_med:
                hits.append(DetectorHit(
                    row_index=i,
                    anomaly_type=self.name,
                    detector_score=0.55,
                    risk_reasons=[f"Vague/medium-risk keywords: {', '.join(matched_med)}"],
                    metadata={"keywords": matched_med, "risk_level": "medium"}
                ))
        return hits


# ── Unusual Account Combination ───────────────────────────────────────────────

class UnusualAccountCombination(Detector):
    """
    Flag JE lines where the debit/credit accounts are uncommonly paired.
    Builds expected-pair frequency map; flags pairs that appear only once.
    """
    name = "unusual_account_combo"
    tier = 1
    weight = 0.70

    def run(self, rows: list[dict], settings: dict) -> list[DetectorHit]:
        # Group by journal_entry_id to get debit/credit account pairs
        je_lines: dict[str, list[dict]] = defaultdict(list)
        for i, row in enumerate(rows):
            je_id = row.get("journal_entry_id") or f"__single_{i}"
            je_lines[je_id].append({**row, "_idx": i})

        pair_counts: dict[frozenset, int] = defaultdict(int)
        je_pairs: dict[str, list[frozenset]] = {}

        for je_id, lines in je_lines.items():
            debits = {(r.get("account_name") or "") for r in lines if (r.get("posting_type") or "").lower() == "debit"}
            credits = {(r.get("account_name") or "") for r in lines if (r.get("posting_type") or "").lower() == "credit"}
            pairs = []
            for d in debits:
                for c in credits:
                    p = frozenset([d, c])
                    pair_counts[p] += 1
                    pairs.append(p)
            je_pairs[je_id] = pairs

        hits = []
        for je_id, lines in je_lines.items():
            for pair in je_pairs.get(je_id, []):
                if pair_counts[pair] == 1:
                    for line in lines:
                        hits.append(DetectorHit(
                            row_index=line["_idx"],
                            anomaly_type=self.name,
                            detector_score=0.60,
                            risk_reasons=[
                                f"Unusual account pairing: {' / '.join(sorted(pair))} — appears only once in this dataset"
                            ],
                            metadata={"account_pair": list(pair)}
                        ))
        return hits


# ── Related Party Transaction ─────────────────────────────────────────────────

class RelatedPartyTransaction(Detector):
    """
    Flag entries whose entity_name matches a name in the user's related-party list.
    """
    name = "related_party_transaction"
    tier = 1
    weight = 0.80
    enabled_flag = "enable_fraud_patterns"

    def run(self, rows: list[dict], settings: dict) -> list[DetectorHit]:
        if not self.is_enabled(settings):
            return []

        related_raw = settings.get("related_party_list") or "[]"
        try:
            related_parties: list = json.loads(related_raw) if isinstance(related_raw, str) else related_raw
        except json.JSONDecodeError:
            return []

        party_names = set()
        for p in related_parties:
            if isinstance(p, dict):
                name = (p.get("name") or p.get("Name") or "").lower()
            else:
                name = str(p).lower()
            if name:
                party_names.add(name)

        if not party_names:
            return []

        hits = []
        for i, row in enumerate(rows):
            entity = (row.get("entity_name") or "").lower()
            for party in party_names:
                if party and (party in entity or entity in party):
                    hits.append(DetectorHit(
                        row_index=i,
                        anomaly_type=self.name,
                        detector_score=0.75,
                        risk_reasons=[f"Transaction involves related party: '{row.get('entity_name')}'"],
                        metadata={"entity_name": row.get("entity_name"), "matched_party": party}
                    ))
                    break
        return hits


# ── New Vendor / First-Time Entity ────────────────────────────────────────────

class NewVendor(Detector):
    """
    Flag entities that appear only once in the dataset AND the amount is above
    the dataset median — potential ghost vendors.
    """
    name = "new_vendor"
    tier = 1
    weight = 0.65

    def run(self, rows: list[dict], settings: dict) -> list[DetectorHit]:
        entity_count: dict[str, int] = defaultdict(int)
        for row in rows:
            entity = (row.get("entity_name") or "").strip()
            if entity:
                entity_count[entity] += 1

        amounts = [r["amount"] for r in rows if r.get("amount")]
        median_amount = statistics.median(amounts) if amounts else 0

        hits = []
        for i, row in enumerate(rows):
            entity = (row.get("entity_name") or "").strip()
            amt = row.get("amount", 0) or 0
            if entity and entity_count[entity] == 1 and amt > median_amount:
                hits.append(DetectorHit(
                    row_index=i,
                    anomaly_type=self.name,
                    detector_score=0.60,
                    risk_reasons=[
                        f"'{entity}' appears only once in this GL and the amount ({amt:,.2f}) exceeds median ({median_amount:,.2f})"
                    ],
                    metadata={"entity": entity, "amount": amt, "median": round(median_amount, 2)}
                ))
        return hits


# ── Manual Journal Entry Override ─────────────────────────────────────────────

class ManualJournalEntry(Detector):
    """
    Flag entries whose source_type indicates manual creation
    (source contains 'manual', 'journal', 'general journal', 'adj', etc.)
    AND amount is above median.
    """
    name = "manual_journal_entry"
    tier = 1
    weight = 0.55

    _MANUAL_KEYWORDS = ("manual", "journal", "general journal", "je ", "adjustment")

    def run(self, rows: list[dict], settings: dict) -> list[DetectorHit]:
        amounts = [r["amount"] for r in rows if r.get("amount")]
        median_amount = statistics.median(amounts) if amounts else 0

        hits = []
        for i, row in enumerate(rows):
            src = (row.get("source_type") or row.get("created_by") or "").lower()
            if any(kw in src for kw in self._MANUAL_KEYWORDS):
                amt = row.get("amount", 0) or 0
                score = 0.50 if amt <= median_amount else 0.65
                hits.append(DetectorHit(
                    row_index=i,
                    anomaly_type=self.name,
                    detector_score=score,
                    risk_reasons=[f"Manual journal entry detected (source: '{src}')"],
                    metadata={"source_type": src, "amount": amt}
                ))
        return hits


# ── Transaction Frequency Spike ───────────────────────────────────────────────

class TransactionFrequencySpike(Detector):
    """
    Flag weeks with significantly more entries than average (>2 SD).
    """
    name = "transaction_frequency_spike"
    tier = 1
    weight = 0.60

    def run(self, rows: list[dict], settings: dict) -> list[DetectorHit]:
        week_counts: dict[str, list[int]] = defaultdict(list)
        for i, row in enumerate(rows):
            d = row.get("date")
            if not isinstance(d, date):
                continue
            # ISO week key
            key = f"{d.isocalendar().year}-W{d.isocalendar().week:02d}"
            week_counts[key].append(i)

        counts = [len(v) for v in week_counts.values()]
        if len(counts) < 4:
            return []

        try:
            mu = statistics.mean(counts)
            sigma = statistics.stdev(counts)
        except statistics.StatisticsError:
            return []

        if sigma == 0:
            return []

        hits = []
        for key, indices in week_counts.items():
            n = len(indices)
            z = (n - mu) / sigma
            if z > 2.0:
                score = min(1.0, 0.5 + z * 0.1)
                for i in indices:
                    hits.append(DetectorHit(
                        row_index=i,
                        anomaly_type=self.name,
                        detector_score=round(score, 4),
                        risk_reasons=[
                            f"Week {key}: {n} entries ({z:.1f}σ above weekly average {mu:.0f})"
                        ],
                        metadata={"week": key, "count": n, "z_score": round(z, 2), "weekly_avg": round(mu, 1)}
                    ))
        return hits


# ── Contra Entry ──────────────────────────────────────────────────────────────

class ContraEntry(Detector):
    """
    Flag pairs of entries in the same account for the same amount that
    exactly offset each other (one debit, one credit).
    These may indicate hidden transactions being laundered through the GL.
    """
    name = "contra_entry"
    tier = 1
    weight = 0.70

    def run(self, rows: list[dict], settings: dict) -> list[DetectorHit]:
        debits: dict[tuple, list[int]] = defaultdict(list)
        credits: dict[tuple, list[int]] = defaultdict(list)

        for i, row in enumerate(rows):
            acct = (row.get("account_name") or "").lower()
            amt = round(row.get("amount", 0) or 0, 2)
            posting = (row.get("posting_type") or "").lower()
            if not acct or not amt:
                continue
            key = (acct, amt)
            if posting == "debit":
                debits[key].append(i)
            elif posting == "credit":
                credits[key].append(i)

        hits = []
        for key in debits:
            if key in credits:
                all_indices = debits[key] + credits[key]
                for i in all_indices:
                    hits.append(DetectorHit(
                        row_index=i,
                        anomaly_type=self.name,
                        detector_score=0.65,
                        risk_reasons=[
                            f"Contra entry: matching debit/credit of {key[1]:,.2f} in account '{key[0]}'"
                        ],
                        metadata={"account": key[0], "amount": key[1]}
                    ))
        return hits


# ── Suspense / Clearing Account ───────────────────────────────────────────────

class SuspenseAccountPosting(Detector):
    """
    Flag entries posted to accounts that are likely suspense/clearing accounts.
    """
    name = "suspense_account"
    tier = 1
    weight = 0.65

    _SUSPENSE_KEYWORDS = (
        "suspense", "clearing", "wash", "transit", "unallocated",
        "unidentified", "temp", "holding", "intercompany clearing",
    )

    def run(self, rows: list[dict], settings: dict) -> list[DetectorHit]:
        hits = []
        for i, row in enumerate(rows):
            acct = (row.get("account_name") or "").lower()
            if any(kw in acct for kw in self._SUSPENSE_KEYWORDS):
                hits.append(DetectorHit(
                    row_index=i,
                    anomaly_type=self.name,
                    detector_score=0.60,
                    risk_reasons=[f"Entry posted to suspense/clearing account: '{row.get('account_name')}'"],
                    metadata={"account_name": row.get("account_name")}
                ))
        return hits


# ── Dormant Account Activity ──────────────────────────────────────────────────

class DormantAccountActivity(Detector):
    """
    Flag entries to accounts that had no prior activity for ≥90 days
    (i.e., a long gap before this entry in the sorted timeline).
    """
    name = "dormant_account_activity"
    tier = 1
    weight = 0.70

    DORMANT_DAYS = 90

    def run(self, rows: list[dict], settings: dict) -> list[DetectorHit]:
        account_dates: dict[str, list[tuple[date, int]]] = defaultdict(list)
        for i, row in enumerate(rows):
            acct = (row.get("account_name") or "").lower()
            d = row.get("date")
            if acct and isinstance(d, date):
                account_dates[acct].append((d, i))

        hits = []
        for acct, pairs in account_dates.items():
            pairs.sort()
            for j in range(1, len(pairs)):
                prev_date, _ = pairs[j - 1]
                curr_date, idx = pairs[j]
                gap = (curr_date - prev_date).days
                if gap >= self.DORMANT_DAYS:
                    hits.append(DetectorHit(
                        row_index=idx,
                        anomaly_type=self.name,
                        detector_score=min(1.0, 0.5 + gap / 365),
                        risk_reasons=[
                            f"Account '{acct}' was dormant for {gap} days before this entry"
                        ],
                        metadata={"account": acct, "gap_days": gap,
                                  "prev_date": str(prev_date), "curr_date": str(curr_date)}
                    ))
        return hits


# ── Debit-Credit Mismatch per JE ─────────────────────────────────────────────

class DebitCreditMismatch(Detector):
    """
    Flag journal entries where total debits ≠ total credits (imbalanced JE).
    """
    name = "debit_credit_mismatch"
    tier = 1
    weight = 0.85

    def run(self, rows: list[dict], settings: dict) -> list[DetectorHit]:
        je_totals: dict[str, dict] = {}
        je_indices: dict[str, list[int]] = defaultdict(list)

        for i, row in enumerate(rows):
            je_id = row.get("journal_entry_id") or ""
            if not je_id:
                continue
            amt = row.get("amount", 0) or 0
            posting = (row.get("posting_type") or "").lower()
            if je_id not in je_totals:
                je_totals[je_id] = {"debit": 0.0, "credit": 0.0}
            if posting == "debit":
                je_totals[je_id]["debit"] += amt
            elif posting == "credit":
                je_totals[je_id]["credit"] += amt
            je_indices[je_id].append(i)

        hits = []
        for je_id, totals in je_totals.items():
            diff = abs(totals["debit"] - totals["credit"])
            if diff > 0.02:  # tolerance for floating point
                for i in je_indices[je_id]:
                    hits.append(DetectorHit(
                        row_index=i,
                        anomaly_type=self.name,
                        detector_score=0.85,
                        risk_reasons=[
                            f"JE {je_id}: debits {totals['debit']:,.2f} ≠ credits {totals['credit']:,.2f} (diff {diff:,.2f})"
                        ],
                        metadata={"journal_entry_id": je_id,
                                  "total_debits": round(totals["debit"], 2),
                                  "total_credits": round(totals["credit"], 2),
                                  "difference": round(diff, 2)}
                    ))
        return hits


# ── Intercompany Unusual ──────────────────────────────────────────────────────

class IntercompanyUnusual(Detector):
    """
    Flag intercompany entries (description/entity contains 'intercompany' or 'IC')
    where the amount is above the dataset 90th percentile.
    """
    name = "intercompany_unusual"
    tier = 1
    weight = 0.65

    _IC_KEYWORDS = ("intercompany", "inter-company", "inter company", " ic ", "/ic")

    def run(self, rows: list[dict], settings: dict) -> list[DetectorHit]:
        amounts = sorted(r["amount"] for r in rows if r.get("amount") and r["amount"] > 0)
        p90 = amounts[int(len(amounts) * 0.90)] if amounts else 0

        hits = []
        for i, row in enumerate(rows):
            text = " ".join([
                (row.get("description") or ""),
                (row.get("entity_name") or ""),
                (row.get("account_name") or ""),
            ]).lower()
            if any(kw in text for kw in self._IC_KEYWORDS):
                amt = row.get("amount", 0) or 0
                if amt > p90:
                    hits.append(DetectorHit(
                        row_index=i,
                        anomaly_type=self.name,
                        detector_score=0.65,
                        risk_reasons=[
                            f"Large intercompany entry: {amt:,.2f} (above 90th pct {p90:,.2f})"
                        ],
                        metadata={"amount": amt, "p90": round(p90, 2)}
                    ))
        return hits


# ── Thin Description ──────────────────────────────────────────────────────────

class ThinDescription(Detector):
    """
    Flag entries where the description is generic boilerplate
    (length 5–20 chars or matches generic patterns like 'expense', 'bill', 'inv').
    Separate from MissingDescription which catches <5 chars.
    """
    name = "thin_description"
    tier = 1
    weight = 0.45

    _GENERIC_PATTERNS = re.compile(
        r"^(expense|bill|invoice|inv|payment|check|misc|other|general|"
        r"service|services|fee|fees|cost|charge|charges|purchase|journal|"
        r"accrual|accrued|\d+)$",
        re.IGNORECASE
    )

    def run(self, rows: list[dict], settings: dict) -> list[DetectorHit]:
        hits = []
        for i, row in enumerate(rows):
            desc = (row.get("description") or "").strip()
            if 5 <= len(desc) <= 20 and self._GENERIC_PATTERNS.match(desc):
                hits.append(DetectorHit(
                    row_index=i,
                    anomaly_type=self.name,
                    detector_score=0.45,
                    risk_reasons=[f"Generic/thin description: '{desc}'"],
                    metadata={"description": desc}
                ))
        return hits


# ── High Frequency Same Vendor Same Amount ────────────────────────────────────

class HighFrequencySameVendorAmount(Detector):
    """
    Flag same vendor + same amount appearing more than 5 times.
    May indicate automated/batched fictitious payments.
    """
    name = "high_frequency_same_vendor_amount"
    tier = 1
    weight = 0.70

    def run(self, rows: list[dict], settings: dict) -> list[DetectorHit]:
        key_indices: dict[tuple, list[int]] = defaultdict(list)
        for i, row in enumerate(rows):
            entity = (row.get("entity_name") or "").lower()
            amt = round(row.get("amount", 0) or 0, 2)
            if entity and amt > 0:
                key_indices[(entity, amt)].append(i)

        hits = []
        for (entity, amt), indices in key_indices.items():
            if len(indices) > 5:
                score = min(1.0, 0.5 + len(indices) * 0.03)
                for i in indices:
                    hits.append(DetectorHit(
                        row_index=i,
                        anomaly_type=self.name,
                        detector_score=round(score, 4),
                        risk_reasons=[
                            f"'{entity}' has {len(indices)} identical entries of {amt:,.2f}"
                        ],
                        metadata={"entity": entity, "amount": amt, "count": len(indices)}
                    ))
        return hits


# ── Even Cent Amount Spike ────────────────────────────────────────────────────

class EvenCentAmountSpike(Detector):
    """
    Flag entries with exactly .00 cents when the account's typical entries
    have cents (non-round). High density of round amounts in a naturally
    non-round account suggests fabrication.
    """
    name = "even_cent_amount"
    tier = 1
    weight = 0.55

    def run(self, rows: list[dict], settings: dict) -> list[DetectorHit]:
        account_amts: dict[str, list[float]] = defaultdict(list)
        for row in rows:
            acct = (row.get("account_name") or "").lower()
            amt = row.get("amount", 0) or 0
            if acct and amt > 0:
                account_amts[acct].append(amt)

        hits = []
        for i, row in enumerate(rows):
            acct = (row.get("account_name") or "").lower()
            amt = row.get("amount", 0) or 0
            if not amt or not acct:
                continue
            if amt % 1.0 != 0:
                continue  # has cents — not a candidate

            acct_amounts = account_amts.get(acct, [])
            if len(acct_amounts) < 10:
                continue

            # What fraction of this account's amounts are NOT round?
            non_round = sum(1 for a in acct_amounts if a % 1.0 != 0)
            non_round_ratio = non_round / len(acct_amounts)

            if non_round_ratio > 0.7:  # normally non-round account
                hits.append(DetectorHit(
                    row_index=i,
                    anomaly_type=self.name,
                    detector_score=0.55,
                    risk_reasons=[
                        f"Round amount {amt:,.2f} in account '{acct}' which is normally non-round ({non_round_ratio:.0%} of entries have cents)"
                    ],
                    metadata={"account": acct, "amount": amt, "non_round_ratio": round(non_round_ratio, 2)}
                ))
        return hits


# ── Registry ──────────────────────────────────────────────────────────────────

TIER1_GROUP_B: list[Detector] = [
    FraudPatternKeywords(),
    UnusualAccountCombination(),
    RelatedPartyTransaction(),
    NewVendor(),
    ManualJournalEntry(),
    TransactionFrequencySpike(),
    ContraEntry(),
    SuspenseAccountPosting(),
    DormantAccountActivity(),
    DebitCreditMismatch(),
    IntercompanyUnusual(),
    ThinDescription(),
    HighFrequencySameVendorAmount(),
    EvenCentAmountSpike(),
]
