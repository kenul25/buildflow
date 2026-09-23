"""Persisted planning states and public task shape."""
from typing import Any, Literal, TypedDict

WorkflowStatus = Literal["Queued", "AwaitingAgents", "AwaitingBackendValidation", "PendingProjectManagerApproval",
                         "Approved", "Rejected", "RevisionRequested", "Failed"]


class AgentTaskSchema(TypedDict):
    task_id: str
    agent: str
    action: str
    depends_on: list[str]
    input: dict[str, Any]
    status: str
