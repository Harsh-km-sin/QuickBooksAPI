"""Database access layer — writes GL analysis results directly to Azure SQL."""
import json
import logging
from datetime import datetime, date
from typing import Any

import pyodbc

from config import SQL_CONNECTION_STRING

logger = logging.getLogger(__name__)

BATCH_SIZE = 100  # anomaly insert batch size


def _connect() -> pyodbc.Connection:
    conn = pyodbc.connect(SQL_CONNECTION_STRING)
    conn.autocommit = False
    return conn


def update_run_status(
    run_id: int,
    status: str,
    *,
    total: int | None = None,
    flagged: int | None = None,
    critical: int | None = None,
    high: int | None = None,
    medium_count: int | None = None,
    low_count: int | None = None,
    normal_count: int | None = None,
    avg_risk: float | None = None,
    period_start: date | None = None,
    period_end: date | None = None,
    material_exposure: float | None = None,
    error: str | None = None,
) -> None:
    completed_at = datetime.utcnow() if status in ("Complete", "Failed") else None
    sql = """
        UPDATE dbo.GL_Runs
        SET Status = ?,
            TotalTransactions = ?,
            FlaggedCount = ?,
            CriticalCount = ?,
            HighCount = ?,
            MediumCount = ?,
            LowCount = ?,
            NormalCount = ?,
            AvgRiskScore = ?,
            PeriodStart = ?,
            PeriodEnd = ?,
            MaterialExposure = ?,
            CompletedAt = ?,
            ErrorMessage = ?
        WHERE Id = ?
    """
    with _connect() as conn:
        conn.execute(sql, (
            status, total, flagged, critical, high,
            medium_count, low_count, normal_count,
            avg_risk, period_start, period_end,
            material_exposure, completed_at, error, run_id
        ))
        conn.commit()
    logger.info("GL_Runs #%d → %s", run_id, status)


def bulk_insert_transactions(run_id: int, rows: list[dict]) -> None:
    if not rows:
        return

    sql = """
        INSERT INTO dbo.GL_Transactions (
            RunId, TransactionDate, AccountId, AccountName, AccountType,
            PostingType, Amount, EntityName, Description, SourceType,
            CreatedBy, CreatedDate, JournalEntryId,
            CompositeRiskScore, RiskTier, AnomalyFlags, ZScore
        ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
    """

    params = [
        (
            run_id,
            r["transaction_date"],
            r.get("account_id"),
            r["account_name"],
            r.get("account_type"),
            r.get("posting_type"),
            r["amount"],
            r.get("entity_name"),
            r.get("description"),
            r.get("source_type"),
            r.get("created_by"),
            r.get("created_date"),
            r.get("journal_entry_id"),
            r.get("composite_risk_score"),
            r.get("risk_tier"),
            json.dumps(r.get("anomaly_flags", [])),
            r.get("z_score"),
        )
        for r in rows
    ]

    with _connect() as conn:
        cursor = conn.cursor()
        cursor.fast_executemany = True
        cursor.executemany(sql, params)
        conn.commit()
    logger.info("Inserted %d transactions for run #%d", len(rows), run_id)


def insert_account_stats(run_id: int, stats: list[dict]) -> None:
    if not stats:
        return

    sql = """
        INSERT INTO dbo.GL_AccountStats (
            RunId, AccountName, TransactionCount, TotalAmount,
            AvgAmount, StdDev, MinAmount, MaxAmount, OutlierCount
        ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)
    """
    params = [
        (
            run_id,
            s["account_name"],
            s["transaction_count"],
            s["total_amount"],
            s["avg_amount"],
            s["std_dev"],
            s["min_amount"],
            s["max_amount"],
            s["outlier_count"],
        )
        for s in stats
    ]

    with _connect() as conn:
        cursor = conn.cursor()
        cursor.fast_executemany = True
        cursor.executemany(sql, params)
        conn.commit()
    logger.info("Inserted %d account stats for run #%d", len(stats), run_id)


def update_run_progress(run_id: int, progress_pct: int) -> None:
    """Lightweight progress update — called between pipeline phases."""
    with _connect() as conn:
        conn.execute(
            "UPDATE dbo.GL_Runs SET ProgressPercentage = ? WHERE Id = ?",
            (max(0, min(100, progress_pct)), run_id)
        )
        conn.commit()


def update_run_started(run_id: int) -> None:
    with _connect() as conn:
        conn.execute(
            "UPDATE dbo.GL_Runs SET StartedAt = ?, Status = 'Processing', ProgressPercentage = 0 WHERE Id = ?",
            (datetime.utcnow(), run_id)
        )
        conn.commit()


def update_transaction_scores(
    run_id: int,
    updates: list[dict],
) -> None:
    """
    Batch-update GL_Transactions with computed risk scores and AI explanations.
    Each dict in updates must have: id, risk_score, composite_risk_score,
    risk_tier, anomaly_flags (list), z_score, ai_explanation, risk_explanation (dict), status.
    """
    if not updates:
        return

    sql = """
        UPDATE dbo.GL_Transactions
        SET RiskScore = ?,
            CompositeRiskScore = ?,
            RiskTier = ?,
            AnomalyFlags = ?,
            ZScore = ?,
            AiExplanation = ?,
            RiskExplanation = ?,
            Status = 'scored'
        WHERE Id = ? AND RunId = ?
    """
    params = [
        (
            u["risk_score"],
            u.get("composite_risk_score"),
            u["risk_tier"],
            json.dumps(u.get("anomaly_flags", [])),
            u.get("z_score"),
            u.get("ai_explanation"),
            json.dumps(u.get("risk_explanation")) if u.get("risk_explanation") else None,
            u["id"],
            run_id,
        )
        for u in updates
    ]

    with _connect() as conn:
        cursor = conn.cursor()
        cursor.fast_executemany = True
        cursor.executemany(sql, params)
        conn.commit()
    logger.info("Updated scores for %d transactions in run #%d", len(updates), run_id)


def get_transaction_ids(run_id: int) -> list[tuple[int, dict]]:
    """
    Return list of (db_id, row_snapshot) for all transactions in the run.
    Used after bulk insert to retrieve auto-assigned IDs for anomaly linking.
    """
    with _connect() as conn:
        cursor = conn.cursor()
        cursor.execute(
            "SELECT Id, JournalEntryId, AccountName, TransactionDate, Amount "
            "FROM dbo.GL_Transactions WHERE RunId = ? ORDER BY Id",
            run_id
        )
        cols = [d[0] for d in cursor.description]
        return [(row[0], dict(zip(cols, row))) for row in cursor.fetchall()]


def bulk_insert_anomalies(run_id: int, user_id: int, anomalies: list[dict]) -> None:
    """
    Insert GL_Anomalies records in batches of BATCH_SIZE.
    Each dict: entry_id, anomaly_type, detector_score, risk_reasons (list), metadata (dict).
    """
    if not anomalies:
        return

    sql = """
        INSERT INTO dbo.GL_Anomalies
            (EntryId, RunId, UserId, AnomalyType, DetectorScore, RiskReasons, Metadata)
        VALUES (?, ?, ?, ?, ?, ?, ?)
    """
    params = [
        (
            a["entry_id"],
            run_id,
            user_id,
            a["anomaly_type"],
            a["detector_score"],
            json.dumps(a.get("risk_reasons", [])),
            json.dumps(a.get("metadata", {})),
        )
        for a in anomalies
    ]

    with _connect() as conn:
        cursor = conn.cursor()
        cursor.fast_executemany = True
        for i in range(0, len(params), BATCH_SIZE):
            cursor.executemany(sql, params[i:i + BATCH_SIZE])
        conn.commit()
    logger.info("Inserted %d anomaly records for run #%d", len(anomalies), run_id)


def update_run_executive_summary(run_id: int, summary: str) -> None:
    with _connect() as conn:
        conn.execute(
            "UPDATE dbo.GL_Runs SET AiExecutiveSummary = ? WHERE Id = ?",
            (summary, run_id)
        )
        conn.commit()
