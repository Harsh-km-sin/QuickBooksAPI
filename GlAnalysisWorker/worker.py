"""
Service Bus consumer loop.

Each message triggers pipeline.run_pipeline() which executes all 9 phases.
"""
import json
import logging
import time

from azure.servicebus import ServiceBusClient

import pipeline
from config import GL_ANALYSIS_QUEUE_NAME, MAX_WAIT_SECONDS, SERVICE_BUS_CONNECTION_STRING

logger = logging.getLogger(__name__)


def run_loop() -> None:
    if not SERVICE_BUS_CONNECTION_STRING:
        logger.warning("SERVICE_BUS_CONNECTION_STRING not set — running in stub mode.")
        _stub_loop()
        return

    logger.info("Connecting to Service Bus queue: %s", GL_ANALYSIS_QUEUE_NAME)
    with ServiceBusClient.from_connection_string(SERVICE_BUS_CONNECTION_STRING) as sb_client:
        with sb_client.get_queue_receiver(
            queue_name=GL_ANALYSIS_QUEUE_NAME,
            max_wait_time=MAX_WAIT_SECONDS,
        ) as receiver:
            logger.info("Worker listening on queue '%s'…", GL_ANALYSIS_QUEUE_NAME)
            while True:
                messages = receiver.receive_messages(
                    max_message_count=1,
                    max_wait_time=MAX_WAIT_SECONDS,
                )
                if not messages:
                    continue

                msg = messages[0]
                try:
                    body = b"".join(msg.body).decode("utf-8")
                    payload = json.loads(body)
                    pipeline.run_pipeline(payload)
                    receiver.complete_message(msg)
                except Exception:
                    logger.exception("Pipeline failed — abandoning message for retry.")
                    receiver.abandon_message(msg)


def _stub_loop() -> None:
    logger.info("Stub mode — sleeping. Set SERVICE_BUS_CONNECTION_STRING to enable queue processing.")
    while True:
        time.sleep(60)
