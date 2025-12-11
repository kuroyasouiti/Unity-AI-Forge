"""
Resource for batch queue status.

Provides read-only access to the current batch execution queue state.
Thread-safe access is ensured by using the BatchQueueManager.
"""

from __future__ import annotations

import json
import logging

from mcp.types import Resource

logger = logging.getLogger(__name__)


def get_batch_queue_resources() -> list[Resource]:
    """Get batch queue resource definitions."""
    return [
        Resource(
            uri="batch://queue/status",
            name="Batch Queue Status",
            description="Current status of sequential batch execution queue",
            mimeType="application/json",
        )
    ]


async def read_batch_queue_resource(uri: str) -> str:
    """Read batch queue resource with thread-safe access.

    Args:
        uri: Resource URI (e.g., "batch://queue/status")

    Returns:
        JSON string with queue status
    """
    # Import here to avoid circular dependency
    from tools.batch_sequential import get_batch_manager

    if uri == "batch://queue/status":
        manager = get_batch_manager()

        # Acquire lock for thread-safe read
        async with manager.lock:
            state = manager.state
            status = state.to_dict()

            # Add helpful information
            if status["remaining_count"] > 0:
                status["next_operation"] = (
                    state.operations[state.current_index]
                    if state.current_index < len(state.operations)
                    else None
                )
                status["can_resume"] = True
                status["resume_hint"] = (
                    f"Call unity_batch_sequential_execute with resume=true to continue "
                    f"from operation {state.current_index + 1}/{status['total_count']}"
                )
            else:
                status["can_resume"] = False
                status["resume_hint"] = (
                    "No pending operations. Start a new batch by calling "
                    "unity_batch_sequential_execute with operations array."
                )

            return json.dumps(status, indent=2, ensure_ascii=False)

    return json.dumps({"error": f"Unknown resource URI: {uri}"}, indent=2)

