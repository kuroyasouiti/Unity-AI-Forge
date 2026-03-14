"""Tests for the _RateLimiter class in main.py."""

from __future__ import annotations

import time
from unittest.mock import patch

from main import _RateLimiter


class TestRateLimiter:
    """Tests for the sliding-window rate limiter."""

    def test_allows_requests_under_limit(self) -> None:
        limiter = _RateLimiter(max_requests=5, window_seconds=10.0)
        for _ in range(5):
            assert limiter.is_allowed() is True

    def test_blocks_requests_over_limit(self) -> None:
        limiter = _RateLimiter(max_requests=3, window_seconds=10.0)
        for _ in range(3):
            assert limiter.is_allowed() is True
        assert limiter.is_allowed() is False

    def test_allows_after_window_expires(self) -> None:
        limiter = _RateLimiter(max_requests=2, window_seconds=0.1)
        assert limiter.is_allowed() is True
        assert limiter.is_allowed() is True
        assert limiter.is_allowed() is False
        # Wait for window to expire
        time.sleep(0.15)
        assert limiter.is_allowed() is True

    def test_sliding_window_evicts_old_timestamps(self) -> None:
        limiter = _RateLimiter(max_requests=2, window_seconds=0.1)
        assert limiter.is_allowed() is True
        time.sleep(0.05)
        assert limiter.is_allowed() is True
        # At limit now; wait for first to expire
        time.sleep(0.06)
        # First timestamp should have expired
        assert limiter.is_allowed() is True

    def test_zero_max_requests_blocks_all(self) -> None:
        limiter = _RateLimiter(max_requests=0, window_seconds=10.0)
        assert limiter.is_allowed() is False

    def test_single_request_limit(self) -> None:
        limiter = _RateLimiter(max_requests=1, window_seconds=10.0)
        assert limiter.is_allowed() is True
        assert limiter.is_allowed() is False
