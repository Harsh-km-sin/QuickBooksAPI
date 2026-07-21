import os
import logging
from dotenv import load_dotenv

load_dotenv()

SERVICE_BUS_CONNECTION_STRING = os.environ.get("SERVICE_BUS_CONNECTION_STRING", "")
GL_ANALYSIS_QUEUE_NAME = os.environ.get("GL_ANALYSIS_QUEUE_NAME", "gl-analysis")
BLOB_STORAGE_CONNECTION_STRING = os.environ.get("BLOB_STORAGE_CONNECTION_STRING", "")
SQL_CONNECTION_STRING = os.environ.get("SQL_CONNECTION_STRING", "")
MAX_WAIT_SECONDS = int(os.environ.get("MAX_WAIT_SECONDS", "10"))

# Email (SMTP) for notifications
SMTP_HOST = os.environ.get("SMTP_HOST", "")
SMTP_PORT = os.environ.get("SMTP_PORT", "587")
SMTP_USER = os.environ.get("SMTP_USER", "")
SMTP_PASSWORD = os.environ.get("SMTP_PASSWORD", "")

# Deep-link base URL for alert emails/Slack
APP_BASE_URL = os.environ.get("APP_BASE_URL", "")

LOG_LEVEL = os.environ.get("LOG_LEVEL", "INFO").upper()
logging.basicConfig(
    level=getattr(logging, LOG_LEVEL, logging.INFO),
    format="%(asctime)s [%(levelname)s] %(name)s: %(message)s",
)
