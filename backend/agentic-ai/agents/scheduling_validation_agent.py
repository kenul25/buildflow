"""Read-only SchedulingValidationAgent for delivery validation."""

from __future__ import annotations

from datetime import datetime, timezone
from typing import Any

from agents.deterministic_control import AgentTask
from schemas.scheduling_agent_schema import (
    SchedulingTaskInput,
    SchedulingValidationResult,
)
from tools.scheduling_tools import (
    SchedulingValidationError,
    validate_delivery,
)


class SchedulingValidationAgent:
    """Validate verified delivery data without database side effects."""

    name = "SchedulingValidationAgent"
    canonical_name = "SchedulingValidationAgent"

    def execute(
        self,
        task: AgentTask | dict[str, Any],
    ) -> dict[str, Any]:

        task_data = (
            task.__dict__
            if isinstance(task, AgentTask)
            else task
        )

        task_id = (
            task_data.get("task_id")
            if isinstance(task_data, dict)
            else None
        )

        try:
            if not isinstance(task_data, dict):
                raise SchedulingValidationError(
                    "invalid_scheduling_task"
                )

            if task_data.get("agent") not in (
                self.name,
                self.canonical_name,
            ):
                raise SchedulingValidationError(
                    "unsupported_scheduling_agent"
                )

            if task_data.get("action") != "ProposeAndValidateSchedule":
                raise SchedulingValidationError(
                    "unsupported_scheduling_action"
                )

            if not isinstance(task_data.get("input"), dict):
                raise SchedulingValidationError(
                    "scheduling_input_required"
                )

            if "schedulingSnapshot" in task_data["input"]:
                from agents.resource_scheduler import propose
                result = propose(task_data["input"])
                return {"schemaVersion": "1.0", "task_id": task_id, "agent": self.name, "status": "Completed", "output": result, "error": None}

            task_input = SchedulingTaskInput.model_validate(
                task_data["input"]
            )

            result = validate_delivery(
                task_input.delivery.model_dump(mode="json"),
                (
                    task_input.purchaseOrder.model_dump(mode="json")
                    if task_input.purchaseOrder is not None
                    else None
                ),
            )

            validated = SchedulingValidationResult(
                schemaVersion="1.0",
                task_id=task_id,
                workflowId=task_input.workflowId,
                agent=self.name,
                status="Completed",
                validationStatus=result["validationStatus"],
                message=result["message"],
                reasons=result["reasons"],
                approvalRequired=True,
                sideEffects=[],
                executionSummary={
                    "agentName": self.name,
                    "validated": True,
                    "sideEffects": [],
                    "completedAt": datetime.now(timezone.utc),
                },
            )

            return validated.model_dump(mode="json")

        except Exception as error:

            code = (
                error.args[0]
                if isinstance(error, SchedulingValidationError)
                and error.args
                else "scheduling_validation_failed"
            )

            failed = SchedulingValidationResult(
                schemaVersion="1.0",
                task_id=task_id,
                agent=self.name,
                status="Failed",
                validationStatus="FAILED",
                message="Scheduling validation failed",
                reasons=[],
                approvalRequired=True,
                sideEffects=[],
                executionSummary={
                    "agentName": self.name,
                    "validated": True,
                    "sideEffects": [],
                    "completedAt": datetime.now(timezone.utc),
                },
                error=str(code),
            )

            return failed.model_dump(mode="json")


def propose_and_validate_schedule(
    delivery: dict[str, Any],
    purchase_order: dict[str, Any] | None,
) -> dict[str, Any]:
    """Convenience entry point for direct callers."""

    return validate_delivery(delivery, purchase_order)
