"""
Parses a GL file (CSV or XLSX) and normalises every row to the canonical schema.

Handles two common amount layouts:
  - Single amount column (positive/negative or always-positive + posting_type column)
  - Separate Debit / Credit columns (one will be 0 or blank per row)
"""
import re
import logging
from datetime import date, datetime
from pathlib import Path

import pandas as pd

logger = logging.getLogger(__name__)

# ---------------------------------------------------------------------------
# Column alias map  →  canonical field name
# ---------------------------------------------------------------------------
_ALIASES: dict[str, list[str]] = {
    "transaction_date": [
        "transaction_date", "date", "txn_date", "txndate", "posting_date",
        "transaction date", "posting date", "gl date", "entry date",
    ],
    "account_id": [
        "account_id", "account_code", "account_number", "account code",
        "account number", "gl_account_code", "acct_id", "acct_code",
    ],
    "account_name": [
        "account_name", "account name", "account", "gl_account",
        "account_title", "account title", "gl account",
    ],
    "account_type": [
        "account_type", "account type", "gl_type", "category",
        "account_category", "account category",
    ],
    "posting_type": [
        "posting_type", "posting type", "type", "debit_credit",
        "dr_cr", "dr/cr", "dc",
    ],
    # Single amount column
    "amount": [
        "amount", "amt", "transaction_amount", "transaction amount",
        "net_amount", "net amount", "value",
    ],
    # Optional running balance — used only in-memory for ML balance-change
    # features (COPOD/ECOD); not persisted to the database.
    "balance": [
        "balance", "running_balance", "running balance", "closing_balance",
        "closing balance", "balance_amount", "balance amount", "gl_balance",
    ],
    # Separate debit/credit columns (detected below)
    "_debit": [
        "debit", "debit_amount", "debit amount", "dr", "dr_amount",
        "dr amount",
    ],
    "_credit": [
        "credit", "credit_amount", "credit amount", "cr", "cr_amount",
        "cr amount",
    ],
    "entity_name": [
        "entity_name", "entity name", "vendor", "vendor_name", "vendor name",
        "customer", "customer_name", "customer name", "payee", "name",
        "supplier", "party",
    ],
    "description": [
        "description", "memo", "narration", "details", "reference",
        "particulars", "notes", "remarks",
    ],
    "source_type": [
        "source_type", "source type", "source", "transaction_type",
        "transaction type", "entry_type", "entry type", "document_type",
    ],
    "created_by": [
        "created_by", "created by", "entered_by", "entered by",
        "posted_by", "posted by", "user", "username", "operator",
    ],
    "created_date": [
        "created_date", "created date", "entry_date", "entry date",
        "created_at", "creation_date", "creation date",
    ],
    "journal_entry_id": [
        "journal_entry_id", "journal entry id", "je_id", "je_number",
        "journal_id", "entry_id", "entry id", "document_no", "doc_no",
        "voucher_no", "ref_no",
    ],
}

_DATE_FORMATS = [
    "%Y-%m-%d", "%d/%m/%Y", "%m/%d/%Y", "%d-%m-%Y", "%m-%d-%Y",
    "%d-%b-%Y", "%d %b %Y", "%b %d, %Y", "%Y/%m/%d",
]


def _normalise_col(name: str) -> str:
    return re.sub(r"[\s_\-]+", "_", name.strip().lower())


def _build_col_map(df_cols: list[str]) -> dict[str, str]:
    """Map DataFrame column names → canonical field names."""
    normed = {_normalise_col(c): c for c in df_cols}
    result: dict[str, str] = {}

    for canonical, aliases in _ALIASES.items():
        for alias in aliases:
            key = _normalise_col(alias)
            if key in normed:
                result[canonical] = normed[key]
                break

    return result


def _parse_amount(value) -> float | None:
    if pd.isna(value):
        return None
    s = str(value).strip()
    if not s or s == "-":
        return None
    # Handle parenthesis negatives: (1,000.00) → -1000.00
    negative = s.startswith("(") and s.endswith(")")
    s = s.strip("()")
    # Strip currency symbols and commas
    s = re.sub(r"[,$£€₹\s]", "", s)
    try:
        result = float(s)
        return -result if negative else result
    except ValueError:
        return None


def _parse_date(value) -> date | None:
    if pd.isna(value):
        return None
    if isinstance(value, (date, datetime)):
        return value.date() if isinstance(value, datetime) else value
    s = str(value).strip()
    for fmt in _DATE_FORMATS:
        try:
            return datetime.strptime(s, fmt).date()
        except ValueError:
            continue
    logger.warning("Unparseable date: %r", s)
    return None


def _str_or_none(value) -> str | None:
    if pd.isna(value):
        return None
    s = str(value).strip()
    return s if s else None


# ---------------------------------------------------------------------------
# Public API
# ---------------------------------------------------------------------------

def parse_file(file_path: str) -> list[dict]:
    """
    Parse a GL CSV or XLSX file and return a list of normalised row dicts.
    Raises ValueError if required columns are missing.
    """
    path = Path(file_path)
    ext = path.suffix.lower()

    if ext == ".csv":
        df = pd.read_csv(file_path, dtype=str, keep_default_na=False)
    elif ext in (".xlsx", ".xls"):
        df = pd.read_excel(file_path, dtype=str, keep_default_na=False)
    else:
        raise ValueError(f"Unsupported file type: {ext}")

    # Replace blank strings with NaN for uniform handling
    df.replace("", pd.NA, inplace=True)
    df.columns = [str(c).strip() for c in df.columns]

    col_map = _build_col_map(df.columns.tolist())

    # Detect separate debit/credit layout
    has_debit = "_debit" in col_map
    has_credit = "_credit" in col_map
    has_amount = "amount" in col_map
    split_debit_credit = (has_debit or has_credit) and not has_amount

    # Validate required columns
    missing = []
    if "transaction_date" not in col_map:
        missing.append("transaction_date (or: date, txn_date, posting_date)")
    if "account_name" not in col_map and "account_id" not in col_map:
        missing.append("account_name (or: account, gl_account, account_code)")
    if not has_amount and not has_debit and not has_credit:
        missing.append("amount (or: debit/credit columns)")
    if missing:
        raise ValueError(
            "Required columns not found in uploaded file:\n  • "
            + "\n  • ".join(missing)
            + "\n\nPlease download the upload template from GL Review Settings."
        )

    rows: list[dict] = []

    for _, raw in df.iterrows():
        def get(field: str):
            col = col_map.get(field)
            return raw[col] if col else pd.NA

        # Amount + posting type resolution
        if split_debit_credit:
            debit_val = _parse_amount(get("_debit")) or 0.0
            credit_val = _parse_amount(get("_credit")) or 0.0
            if debit_val != 0:
                amount = abs(debit_val)
                posting_type = "Debit"
            elif credit_val != 0:
                amount = abs(credit_val)
                posting_type = "Credit"
            else:
                continue  # Both zero — skip row
        else:
            raw_amt = _parse_amount(get("amount"))
            if raw_amt is None:
                continue
            if raw_amt < 0:
                amount = abs(raw_amt)
                posting_type = "Credit"
            else:
                amount = raw_amt
                # Use explicit posting_type column if present
                pt = _str_or_none(get("posting_type"))
                posting_type = pt if pt in ("Debit", "Credit") else None

        txn_date = _parse_date(get("transaction_date"))
        if txn_date is None:
            continue  # Row without a parseable date is unusable

        # account_name falls back to account_id if name column absent
        account_name = (
            _str_or_none(get("account_name"))
            or _str_or_none(get("account_id"))
            or "Unknown"
        )

        rows.append({
            # NOTE: keys here MUST match what the detectors and pipeline consume
            # ("date"/"created_at"), not the internal column-map canonical names
            # ("transaction_date"/"created_date"). pipeline._to_db_rows bridges
            # these back to the DB column names.
            "date": txn_date,
            "account_id": _str_or_none(get("account_id")),
            "account_name": account_name,
            "account_type": _str_or_none(get("account_type")),
            "posting_type": posting_type,
            "amount": round(amount, 2),
            "entity_name": _str_or_none(get("entity_name")),
            "description": _str_or_none(get("description")),
            "source_type": _str_or_none(get("source_type")),
            "created_by": _str_or_none(get("created_by")),
            "created_at": _parse_date(get("created_date")),
            "journal_entry_id": _str_or_none(get("journal_entry_id")),
            "balance": _parse_amount(get("balance")),
        })

    if not rows:
        raise ValueError("File was parsed but no valid rows were found. Check date and amount columns.")

    logger.info("Normalised %d rows from %s", len(rows), path.name)
    return rows
