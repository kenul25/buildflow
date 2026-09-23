"""Versioned task result contract for Members 02–04."""
from typing import Any, Literal, NotRequired, TypedDict


class AgentResult(TypedDict):
    schemaVersion: Literal["1.0"]
    task_id: str
    agent: Literal["InventoryAgent", "ProcurementAgent", "SchedulingValidationAgent"]
    status: Literal["Completed", "Failed"]
    output: dict[str, Any]
    error: NotRequired[str | None]
