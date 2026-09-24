"""Read-only inventory availability agent for the Component 2 workflow."""
from __future__ import annotations

from datetime import datetime, timezone
from typing import Any

from agents.deterministic_control import AgentTask
from schemas.inventory_agent_schema import InventoryAgentResult, InventoryTaskInput
from tools.inventory_tools import InventoryAnalysisError, build_report


class InventoryAgent:
	"""Compare material requirements with a host-provided stock snapshot.

	The host is responsible for reading current data from the inventory API.
	This agent deliberately has no database, HTTP, reservation, or mutation
	capability.
	"""

	name = "InventoryAgent"
	canonical_name = "InventoryAnalysisAgent"

	def execute(self, task: AgentTask | dict[str, Any]) -> dict[str, Any]:
		task_data = task.__dict__ if isinstance(task, AgentTask) else task
		task_id = task_data.get("task_id") if isinstance(task_data, dict) else None
		try:
			if not isinstance(task_data, dict) or task_data.get("agent") not in (self.name, self.canonical_name) or task_data.get("action") != "AnalyzeAvailability":
				raise InventoryAnalysisError("unsupported_inventory_task")
			if not isinstance(task_data.get("input"), dict) or task_data["input"].get("inventorySnapshot") is None:
				raise InventoryAnalysisError("inventory_snapshot_required")
			task_input = InventoryTaskInput.model_validate(task_data.get("input", {}))
			result = build_report(
				[requirement.model_dump(mode="json") for requirement in task_input.items],
				[snapshot.model_dump(mode="json", by_alias=True) for snapshot in task_input.inventory_snapshot],
			)
			materials = [{
				"materialId": item["materialId"], "name": item["name"], "unit": item["unit"],
				"requiredQuantity": item["requiredQuantity"], "availableQuantity": item["availableQuantity"],
				"shortageQuantity": item["shortageQuantity"], "status": item["status"],
			} for item in result["materials"]]
			validated = InventoryAgentResult.model_validate({
				"schemaVersion": "1.0", "task_id": task_id, "workflowId": task_input.workflow_id,
				"agent": self.name, "status": "Completed", "materials": materials,
				"overallStatus": result["overallStatus"], "approvalRequired": True, "sideEffects": [],
				"executionSummary": {"agentName": self.name, "validated": True, "sideEffects": [], "completedAt": datetime.now(timezone.utc)},
			})
			return validated.model_dump(mode="python", by_alias=True)
		except Exception as error:
			code = error.args[0] if isinstance(error, InventoryAnalysisError) and error.args else "inventory_analysis_failed"
			failed = InventoryAgentResult(
				schemaVersion="1.0", task_id=task_id, agent=self.name, status="Failed",
				overallStatus="FAILED", approvalRequired=True, sideEffects=[],
				executionSummary={"agentName": self.name, "validated": True, "sideEffects": [], "completedAt": datetime.now(timezone.utc)}, error=str(code)
			)
			return failed.model_dump(mode="python", by_alias=True)


def analyze_availability(requirements: list[dict[str, Any]], inventory_snapshot: list[dict[str, Any]]) -> dict[str, Any]:
	"""Convenience entry point for callers that do not use the task wrapper."""
	return build_report(requirements, inventory_snapshot)
