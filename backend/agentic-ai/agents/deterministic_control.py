"""Business validation and wire-task materialization. No model or tools run here."""
from __future__ import annotations

import re
from dataclasses import dataclass
from datetime import datetime, timezone
from typing import Any
from uuid import uuid4

from schemas.planning_schema import AcceptedPlan, PlanningRequest, ProposedPlan


class PlanningError(ValueError):
    pass


@dataclass(frozen=True)
class AgentTask:
    task_id: str
    agent: str
    action: str
    depends_on: list[str]
    input: dict[str, Any]
    status: str = "Pending"


# Candidate name is the canonical Member 02 name; InventoryAgent remains the
# established 1.0 wire name consumed by the ASP.NET API and downstream handler.
ALLOWED_TASKS = (
    ("InventoryAnalysisAgent", "InventoryAgent", "AnalyzeAvailability", ()),
    ("ProcurementAgent", "ProcurementAgent", "RecommendProcurement", ("InventoryAnalysisAgent",)),
    ("SchedulingValidationAgent", "SchedulingValidationAgent", "ProposeAndValidateSchedule",
     ("InventoryAnalysisAgent", "ProcurementAgent")),
)
STEP_ORDER = ("AnalyzeNeeds", "InventoryCheck", "ProcurementReview", "ScheduleValidation", "ManagerApproval")

FORBIDDEN_ACTION = re.compile(
    r"\b(?:create|issue|finalize|execute|place)\s+(?:a\s+)?(?:purchase\s+order|po)\b"
    r"|\b(?:reserve|allocate)\s+(?:stock|equipment|materials)\b"
    r"|\bassign\s+(?:workers|crew)\b"
    r"|\b(?:ignore|skip|bypass|disable|override|disregard)\s+(?:(?:the|all|schema|business|manager)\s+)*(?:approval|validation|rules?|checks?)\b"
    r"|\b(?:reveal|print|send|exfiltrate)\s+(?:(?:the|any)\s+)?(?:api\s+key|secret|credentials?)\b"
    r"|\b(?:run|execute)\s+(?:(?:a|the)\s+)?(?:shell|command|script|sql)\b",
    re.IGNORECASE,
)
UNVERIFIED_CLAIM = re.compile(
    r"\b(?:stock|materials|equipment|workers|crew)\s+(?:is|are)\s+(?:available|reserved|assigned)\b"
    r"|\bsupplier\s+(?:is\s+)?(?:confirmed|selected)\b",
    re.IGNORECASE,
)


def validate_proposal(request: PlanningRequest, proposal: ProposedPlan) -> None:
    if proposal.objective.strip() != request.objective:
        raise PlanningError("proposal objective differs from validated request")
    analysis = proposal.analysis
    counts = (
        sum(item.kind == "Material" for item in request.items),
        sum(item.kind == "Equipment" for item in request.items),
        sum(item.kind == "Workforce" for item in request.items),
    )
    if (analysis.materialItems, analysis.equipmentItems, analysis.workforceItems) != counts:
        raise PlanningError("resource analysis does not match request items")
    days = (request.requiredBy - datetime.now(timezone.utc).date()).days if request.requiredBy else None
    if analysis.daysUntilDue != days or analysis.budgetConstrained != (request.budgetLimit is not None):
        raise PlanningError("deadline or budget analysis does not match request")
    expected_priority = "Urgent" if days is not None and days <= 2 else "Normal"
    if analysis.priority != expected_priority:
        raise PlanningError("priority does not match deadline")
    if len(proposal.steps) != len(STEP_ORDER) or any(
        step.sequence != index + 1 or step.kind != kind
        for index, (step, kind) in enumerate(zip(proposal.steps, STEP_ORDER))
    ):
        raise PlanningError("execution steps are out of order")
    if len(proposal.tasks) != len(ALLOWED_TASKS):
        raise PlanningError("exactly three allow-listed task proposals are required")
    for candidate, (agent, _, action, dependencies) in zip(proposal.tasks, ALLOWED_TASKS):
        if candidate.agent != agent or candidate.action != action or tuple(candidate.dependsOn) != dependencies:
            raise PlanningError("agent, task action, or dependency order is unsupported")
    texts = [text for step in proposal.steps for text in (step.title, step.description)] + analysis.risks
    if any(FORBIDDEN_ACTION.search(text) or UNVERIFIED_CLAIM.search(text) for text in texts):
        raise PlanningError("proposal requests a high-impact action or claims unverified availability")
    if "approv" not in (proposal.steps[-1].title + proposal.steps[-1].description).lower():
        raise PlanningError("final step must require manager approval")


def materialize_plan(request: PlanningRequest, proposal: ProposedPlan) -> AcceptedPlan:
    """Create all task IDs, inputs and dependencies in trusted code."""
    validate_proposal(request, proposal)
    analysis = proposal.analysis.model_dump(mode="json")
    items = [
        {"kind": item.kind, "name": item.name, "quantity": float(item.quantity), "unit": item.unit}
        for item in request.items
    ]
    common = {
        "workflowId": str(request.workflowId), "requestId": str(request.requestId),
        "projectId": str(request.projectId), "siteId": str(request.siteId),
        "activityId": str(request.activityId), "objective": request.objective,
        "projectName": request.projectName, "siteName": request.siteName,
        "siteAddress": request.siteAddress, "activityName": request.activityName,
        "activityDueDate": request.activityDueDate.isoformat() if request.activityDueDate else None,
        "requiredBy": request.requiredBy.isoformat() if request.requiredBy else None,
        "budgetLimit": float(request.budgetLimit) if request.budgetLimit is not None else None,
        "analysis": analysis,
        "inventorySnapshot": [item.model_dump(mode="json") for item in request.inventorySnapshot],
    }
    inventory_id, procurement_id, scheduling_id = (str(uuid4()) for _ in range(3))
    material_items = [item for item in items if item["kind"] == "Material"]
    tasks = [
        AgentTask(inventory_id, "InventoryAgent", "AnalyzeAvailability", [],
                  {**common, "items": material_items}),
        AgentTask(procurement_id, "ProcurementAgent", "RecommendProcurement", [inventory_id],
                  {**common, "shortagesFromTaskId": inventory_id}),
        AgentTask(scheduling_id, "SchedulingValidationAgent", "ProposeAndValidateSchedule",
                  [inventory_id, procurement_id],
                  {**common, "items": items, "inventoryTaskId": inventory_id,
                   "procurementTaskId": procurement_id}),
    ]
    return AcceptedPlan.model_validate({
        "schemaVersion": "1.0", "workflowId": request.workflowId,
        "status": "AwaitingAgents", "objective": request.objective,
        "analysis": analysis,
        "steps": [
            {"sequence": step.sequence, "title": step.title, "description": step.description}
            for step in proposal.steps
        ],
        "tasks": [task.__dict__ for task in tasks],
        "approvalRequired": True,
    })
