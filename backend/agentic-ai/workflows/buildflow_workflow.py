"""Advance a plan when external agents return versioned results.

This coordinator does not execute external agents or approve allocations.
The ASP.NET backend must persist results and validate them before approval.
"""
from copy import deepcopy

from agents.planning_agent import PlanningError
from schemas.agent_output_schema import AgentResult


def ready_tasks(plan: dict) -> list[dict]:
    complete = {task["task_id"] for task in plan["tasks"] if task["status"] == "Completed"}
    return [task for task in plan["tasks"] if task["status"] == "Pending"
            and all(dependency in complete for dependency in task["depends_on"])]


def apply_result(plan: dict, result: AgentResult) -> dict:
    if plan.get("schemaVersion") != "1.0" or result.get("schemaVersion") != "1.0":
        raise PlanningError("unsupported agent contract version")
    if plan.get("status") != "AwaitingAgents":
        raise PlanningError("workflow is not awaiting agents")
    updated = deepcopy(plan)
    task = next((item for item in updated["tasks"] if item["task_id"] == result.get("task_id")), None)
    if task is None or task["agent"] != result.get("agent"):
        raise PlanningError("agent result does not match a delegated task")
    if task not in ready_tasks(updated):
        raise PlanningError("task dependencies are incomplete or task already finished")
    if result.get("status") not in ("Completed", "Failed") or not isinstance(result.get("output"), dict):
        raise PlanningError("invalid agent result")
    task["status"] = result["status"]
    task["output"] = result["output"]
    task["error"] = result.get("error")
    if task["status"] == "Failed":
        updated["status"] = "Failed"
    elif all(item["status"] == "Completed" for item in updated["tasks"]):
        # Backend validation is still required; no automatic approval transition.
        updated["status"] = "AwaitingBackendValidation"
    return updated
