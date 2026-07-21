"""
Detector framework base types.

Every detector receives the full normalised GL row list and a settings dict,
and returns a list of DetectorHit — one per (row_index, anomaly) pair.
"""
from __future__ import annotations

from abc import ABC, abstractmethod
from dataclasses import dataclass, field


@dataclass
class DetectorHit:
    row_index: int
    anomaly_type: str       # detector code e.g. "z_score_outlier"
    detector_score: float   # 0.0–1.0 raw signal strength
    risk_reasons: list[str] = field(default_factory=list)
    metadata: dict = field(default_factory=dict)


class Detector(ABC):
    """Base class for all GL anomaly detectors."""

    # Subclasses must set these
    name: str = ""          # unique detector code string
    tier: int = 1           # 1 | 2 | 3 | 4
    weight: float = 1.0     # relative weight within the tier
    enabled_flag: str = ""  # GL_Settings key that gates this detector
                            # empty string = always enabled

    @abstractmethod
    def run(self, rows: list[dict], settings: dict) -> list[DetectorHit]:
        """
        Analyse all rows and return DetectorHit objects for anomalous entries.
        Must not raise — wrap internal errors with try/except and return [].
        """
        ...

    def is_enabled(self, settings: dict) -> bool:
        if not self.enabled_flag:
            return True
        return bool(settings.get(self.enabled_flag, True))
