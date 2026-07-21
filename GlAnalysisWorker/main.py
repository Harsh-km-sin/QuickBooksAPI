"""Entry point — start the GL analysis worker."""
import logging
import sys

import config  # noqa: F401 — loads .env and configures logging
from worker import run_loop

logger = logging.getLogger(__name__)


def main() -> None:
    logger.info("GL Analysis Worker starting…")
    try:
        run_loop()
    except KeyboardInterrupt:
        logger.info("Worker stopped by user.")
        sys.exit(0)
    except Exception:
        logger.exception("Worker crashed.")
        sys.exit(1)


if __name__ == "__main__":
    main()
