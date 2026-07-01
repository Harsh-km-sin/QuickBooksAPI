"""Download GL files from Azure Blob Storage (or local temp for dev)."""
import logging
import os
import tempfile
from pathlib import Path

from config import BLOB_STORAGE_CONNECTION_STRING

logger = logging.getLogger(__name__)


def download_to_temp(blob_path: str, original_filename: str) -> str:
    """
    Download the blob to a temp file and return the local path.
    Caller is responsible for deleting the file after use.

    blob_path format: "container/blobname"
    """
    suffix = Path(original_filename).suffix or ".tmp"
    tmp_file = tempfile.NamedTemporaryFile(delete=False, suffix=suffix)
    tmp_file.close()
    dest = tmp_file.name

    if BLOB_STORAGE_CONNECTION_STRING:
        _download_from_azure(blob_path, dest)
    else:
        _download_from_local(blob_path, dest)

    return dest


def _download_from_azure(blob_path: str, dest: str) -> None:
    from azure.storage.blob import BlobServiceClient

    container, blob_name = blob_path.split("/", 1)
    client = BlobServiceClient.from_connection_string(BLOB_STORAGE_CONNECTION_STRING)
    blob = client.get_blob_client(container=container, blob=blob_name)

    with open(dest, "wb") as f:
        blob.download_blob().readinto(f)

    logger.info("Downloaded blob %s → %s", blob_path, dest)


def _download_from_local(blob_path: str, dest: str) -> None:
    """Mirrors LocalFileBlobStorageService — reads from system temp folder."""
    src = Path(tempfile.gettempdir()) / "gl-uploads" / blob_path
    if not src.exists():
        raise FileNotFoundError(
            f"Local blob not found: {src}. "
            "Set BLOB_STORAGE_CONNECTION_STRING to use Azure Blob Storage."
        )
    import shutil
    shutil.copy2(src, dest)
    logger.info("Copied local blob %s → %s", src, dest)
