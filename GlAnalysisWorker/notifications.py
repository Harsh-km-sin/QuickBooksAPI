"""
Notifications — send email / Slack alerts after a GL run completes.
Only fires when settings.email_alerts=True or settings.slack_webhook is set.
"""
from __future__ import annotations

import json
import logging
import smtplib
from email.mime.text import MIMEText

import httpx

from config import SMTP_HOST, SMTP_PORT, SMTP_USER, SMTP_PASSWORD, APP_BASE_URL

logger = logging.getLogger(__name__)


def send_completion_alerts(
    run_id: int,
    file_name: str,
    critical_count: int,
    high_count: int,
    total: int,
    settings: dict,
) -> None:
    if not (critical_count + high_count):
        return  # No high-priority findings — no alert needed

    if settings.get("email_alerts") and settings.get("alert_email"):
        _send_email(run_id, file_name, critical_count, high_count, total, settings["alert_email"])

    if settings.get("slack_webhook"):
        _send_slack(run_id, file_name, critical_count, high_count, total, settings["slack_webhook"])


def _build_subject(critical: int, high: int, file_name: str) -> str:
    level = "CRITICAL" if critical else "HIGH"
    return f"[GL Alert] {level} findings in {file_name}"


def _build_body_text(run_id: int, file_name: str, critical: int, high: int, total: int) -> str:
    url = f"{APP_BASE_URL}/gl-review/{run_id}" if APP_BASE_URL else f"(run #{run_id})"
    return (
        f"GL Analysis Complete — {file_name}\n\n"
        f"  Total entries : {total}\n"
        f"  Critical       : {critical}\n"
        f"  High           : {high}\n\n"
        f"Review: {url}\n"
    )


def _send_email(
    run_id: int, file_name: str, critical: int, high: int, total: int, recipient: str
) -> None:
    if not all([SMTP_HOST, SMTP_USER, SMTP_PASSWORD]):
        logger.warning("SMTP not configured — email alert skipped.")
        return

    subject = _build_subject(critical, high, file_name)
    body = _build_body_text(run_id, file_name, critical, high, total)

    msg = MIMEText(body)
    msg["Subject"] = subject
    msg["From"] = SMTP_USER
    msg["To"] = recipient

    try:
        with smtplib.SMTP(SMTP_HOST, int(SMTP_PORT or 587)) as server:
            server.ehlo()
            server.starttls()
            server.login(SMTP_USER, SMTP_PASSWORD)
            server.sendmail(SMTP_USER, [recipient], msg.as_string())
        logger.info("Email alert sent to %s for run #%d", recipient, run_id)
    except Exception:
        logger.exception("Failed to send email alert for run #%d", run_id)


def _send_slack(
    run_id: int, file_name: str, critical: int, high: int, total: int, webhook_url: str
) -> None:
    url = f"{APP_BASE_URL}/gl-review/{run_id}" if APP_BASE_URL else f"run #{run_id}"
    color = "#FF0000" if critical else "#FFA500"
    payload = {
        "attachments": [
            {
                "color": color,
                "title": f"GL Analysis Alert — {file_name}",
                "text": (
                    f"*Critical:* {critical}  |  *High:* {high}  |  *Total:* {total}\n"
                    f"<{url}|View Report>"
                ),
                "footer": "CFO Assistant · GL Review",
                "mrkdwn_in": ["text"],
            }
        ]
    }

    try:
        response = httpx.post(webhook_url, json=payload, timeout=10)
        response.raise_for_status()
        logger.info("Slack alert sent for run #%d", run_id)
    except Exception:
        logger.exception("Failed to send Slack alert for run #%d", run_id)
