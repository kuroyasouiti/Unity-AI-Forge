from __future__ import annotations

import logging

from config.env import env

TRACE_LEVEL = 5

if not hasattr(logging, "TRACE"):
    logging.addLevelName(TRACE_LEVEL, "TRACE")

    def trace(self: logging.Logger, message: str, *args, **kwargs) -> None:
        if self.isEnabledFor(TRACE_LEVEL):
            self._log(TRACE_LEVEL, message, args, **kwargs)

    setattr(logging.Logger, "trace", trace)  # type: ignore[attr-defined]


_LEVEL_MAP = {
    "fatal": logging.CRITICAL,
    "error": logging.ERROR,
    "warn": logging.WARNING,
    "info": logging.INFO,
    "debug": logging.DEBUG,
    "trace": TRACE_LEVEL,
    "silent": TRACE_LEVEL,
}


def _configure_logging() -> logging.Logger:
    level = _LEVEL_MAP.get(env.log_level, logging.INFO)

    logging.basicConfig(
        level=TRACE_LEVEL if env.log_level == "trace" else level,
        format="%(asctime)s %(levelname)s [%(name)s] %(message)s",
        datefmt="%Y-%m-%d %H:%M:%S",
    )

    if env.log_level == "silent":
        logging.disable(logging.CRITICAL)

    return logging.getLogger("unity-mcp-server")


logger = _configure_logging()

__all__ = ["logger", "TRACE_LEVEL"]
