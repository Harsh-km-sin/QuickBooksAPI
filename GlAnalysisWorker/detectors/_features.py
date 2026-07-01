"""
Shared feature engineering for the Tier-2 ML detectors.

Centralises the feature vectors used by IsolationForest / COPOD / ECOD so the
three detectors cannot drift apart (the TS feat/GL-review code duplicated
`buildFeatureVectors`/`encodeAccountType` across COPOD and ECOD — this avoids that).

Canonical normalised row fields (see normalizer.py):
    amount        float   absolute value
    posting_type  str     "Debit" | "Credit" | None
    account_name  str
    account_type  str | None
    entity_name   str | None      (party / vendor)
    description   str | None
    date          datetime.date | None
    balance       float | None    (optional running balance; sentinel 0.5 when absent)
"""
from __future__ import annotations

from bisect import bisect_right
from collections import defaultdict
from datetime import date as _date
from math import log10

import numpy as np

# ── Account-type ordinal encoding ───────────────────────────────────────────────
# Mirrors feat/GL-review encodeAccountType. NOTE: this imposes an ordering on a
# categorical field (income "closer to" expense than to equity); the distance-based
# detectors read it literally. Kept for parity with the canonical implementation.

_ACCOUNT_TYPE_ENCODING: dict[str, float] = {
    "income": 0.1, "revenue": 0.1,
    "expense": 0.2, "cost": 0.2,
    "asset": 0.3,
    "liability": 0.4,
    "equity": 0.5, "capital": 0.5,
    "bank": 0.6, "cash": 0.6,
}


def encode_account_type(account_type: str | None) -> float:
    if not account_type:
        return 0.35
    lower = account_type.lower()
    for key, val in _ACCOUNT_TYPE_ENCODING.items():
        if key in lower:
            return val
    return 0.35


def _is_debit(row: dict) -> float:
    return 1.0 if (row.get("posting_type") or "").lower() == "debit" else 0.0


# ── 8-dim COPOD / ECOD feature vectors ──────────────────────────────────────────

COPOD_ECOD_FEATURES = (
    "logAmount",
    "dayOfWeek",
    "dayOfMonth",
    "accountTypeCode",
    "partyFrequency",
    "accountFrequency",
    "isDebit",
    "balanceChangeAbsLog",
)


def build_copod_ecod_rows(rows: list[dict]) -> list[dict[str, float]]:
    """Build the 8-dim feature dict per row (all features pre-normalised to [0, 1])."""
    n = len(rows)
    amounts = [float(r.get("amount") or 0.0) for r in rows]
    log_amounts = [log10(a + 1) for a in amounts]
    log_amount_max = max(log_amounts) or 1.0

    party_count: dict[str, int] = defaultdict(int)
    account_count: dict[str, int] = defaultdict(int)
    for r in rows:
        party_count[(r.get("entity_name") or "__none__")] += 1
        account_count[(r.get("account_name") or "__none__")] += 1
    total = n or 1

    # Balance-change log, normalised. Sequential diff over the row order.
    bal_change_logs: list[float] = []
    for i, r in enumerate(rows):
        if i == 0:
            bal_change_logs.append(0.0)
            continue
        prev = rows[i - 1].get("balance")
        curr = r.get("balance")
        if prev is None or curr is None:
            bal_change_logs.append(0.0)
        else:
            bal_change_logs.append(log10(abs(float(curr) - float(prev)) + 1))
    bal_change_max = max(bal_change_logs) or 1.0

    out: list[dict[str, float]] = []
    for i, r in enumerate(rows):
        d = r.get("date")
        valid = isinstance(d, _date)
        has_balance = r.get("balance") is not None
        out.append({
            "logAmount":           log_amounts[i] / log_amount_max if log_amount_max > 0 else 0.0,
            "dayOfWeek":           (d.weekday() / 6) if valid else 0.5,
            "dayOfMonth":          ((d.day - 1) / 30) if valid else 0.5,
            "accountTypeCode":     encode_account_type(r.get("account_type")),
            "partyFrequency":      party_count[(r.get("entity_name") or "__none__")] / total,
            "accountFrequency":    account_count[(r.get("account_name") or "__none__")] / total,
            "isDebit":             _is_debit(r),
            "balanceChangeAbsLog": (bal_change_logs[i] / bal_change_max) if has_balance else 0.5,
        })
    return out


# ── 9-dim Isolation Forest feature matrix ───────────────────────────────────────

IF_FEATURES = (
    "log_amount",
    "day_of_week",
    "day_of_month",
    "account_type",
    "amount_to_account_avg",
    "is_month_end",
    "is_quarter_end",
    "is_round_amount",
    "description_length",
)


def build_if_matrix(rows: list[dict]) -> tuple[np.ndarray, list[int]]:
    """
    Build the (N, 9) Isolation Forest feature matrix.
    Returns (matrix, valid_indices) — rows missing an amount are skipped.
    """
    amounts = [float(r.get("amount") or 0.0) for r in rows]
    log_amounts = [log10(a + 1) if a > 0 else 0.0 for a in amounts]
    log_amount_max = max(log_amounts) or 1.0

    # Per-account average amount (for amount_to_account_avg).
    acct_amounts: dict[str, list[float]] = defaultdict(list)
    for r, a in zip(rows, amounts):
        acct_amounts[(r.get("account_name") or "__unknown__")].append(a)
    acct_avg = {
        acct: (sum(vals) / len(vals) if vals else 0.0)
        for acct, vals in acct_amounts.items()
    }

    valid: list[int] = []
    feats: list[list[float]] = []
    for i, r in enumerate(rows):
        if r.get("amount") is None:
            continue
        amount = amounts[i]
        d = r.get("date")
        valid_date = isinstance(d, _date)
        day = d.day if valid_date else 1
        month = d.month if valid_date else 1

        avg = acct_avg.get((r.get("account_name") or "__unknown__")) or amount or 1.0
        amount_to_avg_raw = amount / avg if avg > 0 else 1.0
        amount_to_account_avg = min(10.0, amount_to_avg_raw) / 10.0

        is_month_end = 1.0 if day >= 28 else 0.0
        is_quarter_end = 1.0 if (month % 3 == 0 and day >= 28) else 0.0
        is_round_amount = 1.0 if (amount > 0 and amount % 100 == 0) else 0.0

        words = (r.get("description") or "").split()
        description_length = min(len(words), 20) / 20.0

        feats.append([
            log_amounts[i] / log_amount_max if log_amount_max > 0 else 0.0,
            (d.weekday() / 6) if valid_date else 0.5,
            ((day - 1) / 30) if valid_date else 0.5,
            encode_account_type(r.get("account_type")),
            amount_to_account_avg,
            is_month_end,
            is_quarter_end,
            is_round_amount,
            description_length,
        ])
        valid.append(i)

    if not feats:
        return np.empty((0, len(IF_FEATURES))), []
    return np.array(feats, dtype=np.float64), valid


# ── Empirical CDF helpers (COPOD / ECOD) ────────────────────────────────────────

def empirical_left_cdf(values: list[float]) -> list[float]:
    """Left-tail empirical CDF: P(X <= x_i) = rank(x_i) / n, via binary search."""
    n = len(values)
    if n == 0:
        return []
    ordered = sorted(values)
    return [bisect_right(ordered, v) / n for v in values]


def empirical_right_cdf(left_cdf: list[float]) -> list[float]:
    """Right-tail with 1/n smoothing to avoid log(0)."""
    n = len(left_cdf) or 1
    return [1 - l + 1 / n for l in left_cdf]
