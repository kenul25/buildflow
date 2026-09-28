"""Scheduling and validation agent for workforce/equipment scheduling."""
from __future__ import annotations

from datetime import datetime, timezone
from typing import Any

from agents.deterministic_control import AgentTask
from schemas.scheduling_schema import (
    SchedulingRequest,
    ScheduleProposal,
    SchedulingValidationResult,
)
from tools.scheduling_tools import build_schedule_report


class SchedulingValidationError(ValueError):
    pass


class SchedulingValidationAgent:
    name = "SchedulingValidationAgent"

    def execute(self, task: AgentTask | dict[str, Any]) -> dict[str, Any]:
        task_data = task.__dict__ if isinstance(task, AgentTask) else task
        task_id = task_data.get("task_id")

        try:
            if (
                not isinstance(task_data, dict)
                or task_data.get("agent") != self.name
                or task_data.get("action") != "ProposeAndValidateSchedule"
            ):
                raise SchedulingValidationError("unsupported_scheduling_task")

            if not isinstance(task_data.get("input"), dict):
                raise SchedulingValidationError("scheduling_input_required")

            request = SchedulingRequest.model_validate(task_data["input"])

            report = build_schedule_report(request.model_dump(mode="python"))

            if report["startTime"] is None or report["endTime"] is None:
                raise SchedulingValidationError("invalid_schedule_time")

            proposal = ScheduleProposal(
                schemaVersion="1.0",
                activityId=request.activityId,
                startTime=datetime.fromisoformat(report["startTime"]),
                endTime=datetime.fromisoformat(report["endTime"]),
                workerIds=report["workerIds"],
                equipmentIds=report["equipmentIds"],
                procurementCost=request.procurementCost,
                risks=report["risks"],
            )

            self._validate_proposal(request, proposal, report["validationErrors"])

            result = SchedulingValidationResult(
                schemaVersion="1.0",
                task_id=task_id,
                status="Completed",
                accepted=report["accepted"],
                approvalRequired=True,
                proposal=proposal,
                validationErrors=report["validationErrors"],
                risks=report["risks"],
                sideEffects=[],
                completedAt=datetime.now(timezone.utc),
            )

            return result.model_dump(mode="python")

        except Exception as error:
            code = (
                error.args[0]
                if isinstance(error, SchedulingValidationError) and error.args
                else "scheduling_validation_failed"
            )

            failed = SchedulingValidationResult(
                schemaVersion="1.0",
                task_id=task_id,
                status="Failed",
                accepted=False,
                approvalRequired=True,
                proposal=None,
                validationErrors=[],
                risks=[],
                sideEffects=[],
                completedAt=datetime.now(timezone.utc),
                error=str(code),
            )

            return failed.model_dump(mode="python")

    @staticmethod
    def _validate_proposal(
        request: SchedulingRequest,
        proposal: ScheduleProposal,
        validation_errors: list[str],
    ) -> None:
        if proposal.activityId != request.activityId:
            raise SchedulingValidationError("proposal_activity_mismatch")

        if proposal.startTime >= proposal.endTime:
            raise SchedulingValidationError("proposal_time_invalid")

        available_worker_ids = {
            worker["workerId"]
            for worker in request.model_dump(mode="python")["workers"]
            if worker.get("available")
        }

        if not set(proposal.workerIds).issubset(available_worker_ids):
            raise SchedulingValidationError("proposal_contains_unavailable_worker")

        available_equipment_ids = {
            item["equipmentId"]
            for item in request.model_dump(mode="python")["equipment"]
            if item.get("available") and item.get("status") == "Available"
        }

        if not set(proposal.equipmentIds).issubset(available_equipment_ids):
            raise SchedulingValidationError("proposal_contains_unavailable_equipment")

        if not validation_errors:
            if len(proposal.workerIds) < request.requiredWorkerCount:
                raise SchedulingValidationError("proposal_has_insufficient_workers")

            if request.budgetLimit is not None and request.procurementCost is not None:
                if request.procurementCost > request.budgetLimit:
                    raise SchedulingValidationError("procurement_cost_exceeds_budget")

    def propose(
        self,
        task: AgentTask | dict[str, Any],
    ) -> dict[str, Any]:
        """Synchronous compatibility wrapper."""
        return self.execute(task)
