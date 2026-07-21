"""
Load GL_Settings for a user from the database.
Returns a plain dict with snake_case keys matching the GL_Settings columns.
Falls back to hardcoded defaults when no row exists (new user, first run).
"""
import logging
import pyodbc

logger = logging.getLogger(__name__)

_DEFAULTS: dict = {
    "threshold_critical": 65,
    "threshold_high": 40,
    "threshold_medium": 20,
    "threshold_low": 8,
    "weight_tier1_statistical": 0.25,
    "weight_tier2_ml": 0.35,
    "weight_tier3_rules": 0.25,
    "weight_tier4_llm": 0.15,
    "llm_provider": "anthropic",
    "llm_model": "claude-sonnet-4-6",
    "api_key_anthropic": None,
    "api_key_openai": None,
    "api_key_google": None,
    "enable_benford_law": True,
    "enable_round_number": True,
    "enable_threshold_breach": True,
    "enable_backdating": True,
    "enable_period_end_cluster": True,
    "enable_fraud_patterns": True,
    "enable_near_duplicates": True,
    "enable_isolation_forest": True,
    "enable_dbscan": True,
    "enable_association_rule": True,
    "enable_copod": True,
    "enable_ecod": True,
    "enable_behavior_profiling": True,
    "email_alerts": False,
    "alert_email": None,
    "slack_webhook": None,
    "fiscal_year_start_month": 1,
    "related_party_list": "[]",
    "scheduled_enabled": False,
}

# Map DB column names (PascalCase) → settings dict keys (snake_case)
_COL_MAP = {
    "ThresholdCritical": "threshold_critical",
    "ThresholdHigh": "threshold_high",
    "ThresholdMedium": "threshold_medium",
    "ThresholdLow": "threshold_low",
    "WeightTier1Statistical": "weight_tier1_statistical",
    "WeightTier2Ml": "weight_tier2_ml",
    "WeightTier3Rules": "weight_tier3_rules",
    "WeightTier4Llm": "weight_tier4_llm",
    "LlmProvider": "llm_provider",
    "LlmModel": "llm_model",
    "ApiKeyAnthropic": "api_key_anthropic",
    "ApiKeyOpenai": "api_key_openai",
    "ApiKeyGoogle": "api_key_google",
    "EnableBenfordLaw": "enable_benford_law",
    "EnableRoundNumber": "enable_round_number",
    "EnableThresholdBreach": "enable_threshold_breach",
    "EnableBackdating": "enable_backdating",
    "EnablePeriodEndCluster": "enable_period_end_cluster",
    "EnableFraudPatterns": "enable_fraud_patterns",
    "EnableNearDuplicates": "enable_near_duplicates",
    "EnableIsolationForest": "enable_isolation_forest",
    "EnableDbscan": "enable_dbscan",
    "EnableAssociationRule": "enable_association_rule",
    "EnableCopod": "enable_copod",
    "EnableEcod": "enable_ecod",
    "EnableBehaviorProfiling": "enable_behavior_profiling",
    "EmailAlerts": "email_alerts",
    "AlertEmail": "alert_email",
    "SlackWebhook": "slack_webhook",
    "FiscalYearStartMonth": "fiscal_year_start_month",
    "RelatedPartyList": "related_party_list",
    "ScheduledEnabled": "scheduled_enabled",
}


def load_settings(conn: pyodbc.Connection, user_id: int) -> dict:
    """Return settings dict for user_id, falling back to defaults if no row."""
    cursor = conn.cursor()
    cursor.execute("SELECT * FROM dbo.GL_Settings WHERE UserId = ?", user_id)
    row = cursor.fetchone()

    if row is None:
        logger.info("No GL_Settings row for userId=%d — using defaults.", user_id)
        return dict(_DEFAULTS)

    cols = [desc[0] for desc in cursor.description]
    settings = dict(_DEFAULTS)  # start with defaults
    for col, val in zip(cols, row):
        key = _COL_MAP.get(col)
        if key:
            # Convert SQL bit (0/1) to bool for boolean keys
            if isinstance(val, int) and key.startswith("enable_"):
                settings[key] = bool(val)
            elif isinstance(val, int) and key in ("email_alerts", "scheduled_enabled"):
                settings[key] = bool(val)
            else:
                settings[key] = val

    logger.debug("Loaded GL_Settings for userId=%d: provider=%s model=%s",
                 user_id, settings["llm_provider"], settings["llm_model"])
    return settings
