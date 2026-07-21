"""
Tier-3 Business Rules — evaluate user-defined GL_BusinessRules against each row.

Each rule is stored in the DB with a Condition JSON blob:
  Field condition:
    {"type": "field", "field": "amount", "operator": "gt", "value": 50000}

  Account threshold condition:
    {"type": "account_threshold", "account": "Petty Cash",
     "direction": "debit", "amount": 500}

Severity → detector_score mapping:
  critical = 1.0,  high = 0.75,  medium = 0.50,  low = 0.30
"""
from __future__ import annotations

import json
import logging
from typing import Any

from .base import Detector, DetectorHit

logger = logging.getLogger(__name__)

_SEVERITY_SCORES = {
    "critical": 1.0,
    "high":     0.75,
    "medium":   0.50,
    "low":      0.30,
}

_OPERATORS = {
    "gt":  lambda a, b: a > b,
    "gte": lambda a, b: a >= b,
    "lt":  lambda a, b: a < b,
    "lte": lambda a, b: a <= b,
    "eq":  lambda a, b: a == b,
    "neq": lambda a, b: a != b,
    "contains": lambda a, b: str(b).lower() in str(a).lower(),
}


class BusinessRuleEvaluator(Detector):
    """
    Evaluates all active business rules for the run's user.
    Rules are injected at construction time from the DB.
    """
    name = "business_rule_breach"
    tier = 3
    weight = 1.0
    enabled_flag = ""

    def __init__(self, rules: list[dict]):
        """
        rules: list of dicts with keys:
            Id, RuleName, Condition (str JSON), Severity, IsActive
        """
        self.rules = [r for r in rules if r.get("IsActive", True)]

    def run(self, rows: list[dict], settings: dict) -> list[DetectorHit]:
        hits = []
        for rule in self.rules:
            condition_raw = rule.get("Condition") or "{}"
            try:
                condition = json.loads(condition_raw) if isinstance(condition_raw, str) else condition_raw
            except json.JSONDecodeError:
                logger.warning("Rule %s has invalid Condition JSON — skipped.", rule.get("Id"))
                continue

            severity = (rule.get("Severity") or "medium").lower()
            score = _SEVERITY_SCORES.get(severity, 0.50)
            rule_name = rule.get("RuleName") or f"Rule #{rule.get('Id')}"

            ctype = condition.get("type", "field")

            if ctype == "field":
                for i, row in enumerate(rows):
                    if _eval_field_condition(row, condition):
                        hits.append(DetectorHit(
                            row_index=i,
                            anomaly_type=self.name,
                            detector_score=score,
                            risk_reasons=[f"Business rule '{rule_name}' triggered ({severity})"],
                            metadata={
                                "rule_id": rule.get("Id"),
                                "rule_name": rule_name,
                                "severity": severity,
                                "condition": condition,
                            }
                        ))

            elif ctype == "account_threshold":
                for i, row in enumerate(rows):
                    if _eval_account_threshold(row, condition):
                        hits.append(DetectorHit(
                            row_index=i,
                            anomaly_type=self.name,
                            detector_score=score,
                            risk_reasons=[f"Business rule '{rule_name}' triggered ({severity})"],
                            metadata={
                                "rule_id": rule.get("Id"),
                                "rule_name": rule_name,
                                "severity": severity,
                                "condition": condition,
                            }
                        ))
            else:
                logger.warning("Unknown condition type '%s' in rule %s", ctype, rule.get("Id"))

        return hits


def _eval_field_condition(row: dict, cond: dict) -> bool:
    """Evaluate a field-based condition against one row."""
    field = cond.get("field", "")
    operator = cond.get("operator", "gt")
    value = cond.get("value")

    # Map GL field names to normalised row keys
    _field_aliases = {
        "amount": "amount",
        "account_name": "account_name",
        "entity_name": "entity_name",
        "description": "description",
        "posting_type": "posting_type",
        "source_type": "source_type",
        "created_by": "created_by",
    }

    row_key = _field_aliases.get(field.lower(), field.lower())
    row_value = row.get(row_key)

    if row_value is None or value is None:
        return False

    op_fn = _OPERATORS.get(operator)
    if op_fn is None:
        return False

    try:
        # Coerce numeric comparisons
        if operator in ("gt", "gte", "lt", "lte", "eq", "neq"):
            return op_fn(float(row_value), float(value))
        return op_fn(row_value, value)
    except (TypeError, ValueError):
        return False


def _eval_account_threshold(row: dict, cond: dict) -> bool:
    """Evaluate an account-threshold condition against one row."""
    account_pattern = (cond.get("account") or "").lower()
    direction = (cond.get("direction") or "").lower()
    threshold = cond.get("amount", 0)

    row_account = (row.get("account_name") or "").lower()
    row_direction = (row.get("posting_type") or "").lower()
    row_amount = row.get("amount", 0) or 0

    if account_pattern and account_pattern not in row_account:
        return False
    if direction and direction != row_direction:
        return False
    return float(row_amount) > float(threshold)


def load_business_rules_from_db(conn, user_id: int) -> list[dict]:
    """Fetch active business rules from DB for a user."""
    cursor = conn.cursor()
    cursor.execute(
        "SELECT Id, RuleName, Condition, Severity, IsActive "
        "FROM dbo.GL_BusinessRules WHERE UserId = ? AND IsActive = 1",
        user_id
    )
    cols = [d[0] for d in cursor.description]
    return [dict(zip(cols, row)) for row in cursor.fetchall()]
