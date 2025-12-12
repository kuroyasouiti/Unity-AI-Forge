from __future__ import annotations

import logging

TRACE_LEVEL = 5

if not hasattr(logging, "TRACE"):
    logging.addLevelName(TRACE_LEVEL, "TRACE")

    def trace(self: logging.Logger, message: str, *args, **kwargs) -> None:
        if self.isEnabledFor(TRACE_LEVEL):
            self._log(TRACE_LEVEL, message, args, **kwargs)

    logging.Logger.trace = trace  # type: ignore[attr-defined]


_LEVEL_MAP = {
    "fatal": logging.CRITICAL,
    "error": logging.ERROR,
    "warn": logging.WARNING,
    "info": logging.INFO,
    "debug": logging.DEBUG,
    "trace": TRACE_LEVEL,
    "silent": TRACE_LEVEL,
}

# Flag to track if logging has been configured
_logging_configured = False


def _configure_logging_once() -> None:
    """Configure logging lazily on first use.

    This defers env access until after CLI arguments have been applied.
    """
    global _logging_configured
    if _logging_configured:
        return
    _logging_configured = True

    # Import env here to defer access until after CLI args are applied
    from config.env import env

    level = _LEVEL_MAP.get(env.log_level, logging.INFO)

    logging.basicConfig(
        level=TRACE_LEVEL if env.log_level == "trace" else level,
        format="%(asctime)s %(levelname)s [%(name)s] %(message)s",
        datefmt="%Y-%m-%d %H:%M:%S",
    )

    if env.log_level == "silent":
        logging.disable(logging.CRITICAL)


class _LazyLogger:
    """Lazy logger that defers configuration until first use.

    This allows CLI arguments to be applied before logging is configured,
    ensuring the bridge_token and other settings are correctly loaded.
    """

    def __init__(self, name: str) -> None:
        self._name = name
        self._logger: logging.Logger | None = None

    def _get_logger(self) -> logging.Logger:
        if self._logger is None:
            _configure_logging_once()
            self._logger = logging.getLogger(self._name)
        return self._logger

    def __getattr__(self, name: str) -> object:
        return getattr(self._get_logger(), name)


logger: logging.Logger = _LazyLogger("unity-mcp-server")  # type: ignore[assignment]

__all__ = ["logger", "TRACE_LEVEL"]
