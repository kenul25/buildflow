"""Schema-driven LangGraph Planning Agent; Gemini only proposes a plan."""
from __future__ import annotations

import asyncio
import json
import os
import time
from datetime import datetime, timezone
from typing import Any, Protocol, TypedDict

from langgraph.graph import END, START, StateGraph
from pydantic import ValidationError

from agents.deterministic_control import AgentTask, PlanningError, materialize_plan, validate_proposal
from agents.gemini_gateway import GeminiConfigurationError, GeminiGateway, ProposalGateway
from schemas.planning_schema import AcceptedPlan, ExecutionSummary, PlanningRequest, ProposedPlan, ValidationResult


# Keep the established wire names used by the Member 02–04 task-result endpoint.
AGENTS = ("InventoryAgent", "ProcurementAgent", "SchedulingValidationAgent")


class AgentPort(Protocol):
    """Existing downstream contract; PlanningAgent never invokes this port."""
    def execute(self, task: AgentTask) -> dict[str, Any]: ...


class PlanningFailure(PlanningError):
    def __init__(self, code: str, summary: ExecutionSummary):
        super().__init__(code)
        self.code = code
        self.summary = summary
        self.events: list[dict[str, str]] = []


class PlanningState(TypedDict, total=False):
    request: PlanningRequest
    raw: Any
    proposal: ProposedPlan | None
    plan: AcceptedPlan
    attempts: int
    errors: list[str]
    events: list[dict[str, str]]
    failure: str | None
    retryable: bool
    schema_valid: bool
    business_valid: bool


def _event(node: str, status: str) -> dict[str, str]:
    return {"node": node, "status": status, "at": datetime.now(timezone.utc).isoformat()}


class PlanningAgent:
    def __init__(self, gateway: ProposalGateway | None = None, *, model: str | None = None,
                 timeout_seconds: float | None = None, max_attempts: int | None = None,
                 retry_delay_seconds: float | None = None):
        self.gateway = gateway or GeminiGateway()
        self.model = model or os.environ.get("GEMINI_MODEL", "gemini-3.5-flash-lite")
        self.timeout_seconds = timeout_seconds or float(os.environ.get("GEMINI_TIMEOUT_SECONDS", "12"))
        self.max_attempts = max_attempts or int(os.environ.get("GEMINI_MAX_ATTEMPTS", "2"))
        self.retry_delay_seconds = retry_delay_seconds if retry_delay_seconds is not None else float(os.environ.get("GEMINI_RETRY_DELAY_SECONDS", "0.3"))
        if not 0 < self.timeout_seconds <= 30 or not 1 <= self.max_attempts <= 3 or not 0 <= self.retry_delay_seconds <= 2:
            raise ValueError("invalid planning timeout or retry configuration")
        graph = StateGraph(PlanningState)
        graph.add_node("call_gemini", self._call_gemini)
        graph.add_node("retry_delay", self._retry_delay)
        graph.add_node("validate_proposal", self._validate)
        graph.add_node("materialize_tasks", self._materialize)
        graph.add_node("safe_failure", self._safe_failure)
        graph.add_edge(START, "call_gemini")
        graph.add_conditional_edges("call_gemini", self._route_after_model,
                                    {"validate": "validate_proposal", "retry": "retry_delay", "fail": "safe_failure"})
        graph.add_edge("retry_delay", "call_gemini")
        graph.add_conditional_edges("validate_proposal", lambda state: "materialize" if state.get("proposal") else "fail",
                                    {"materialize": "materialize_tasks", "fail": "safe_failure"})
        graph.add_conditional_edges("materialize_tasks", lambda state: "done" if state.get("plan") else "fail",
                                    {"done": END, "fail": "safe_failure"})
        graph.add_edge("safe_failure", END)
        self.graph = graph.compile()

    async def _call_gemini(self, state: PlanningState) -> PlanningState:
        attempts = state.get("attempts", 0) + 1
        events = [*state.get("events", []), _event("call_gemini", "started")]
        try:
            raw = await asyncio.wait_for(self.gateway.generate(state["request"]), self.timeout_seconds)
            return {"raw": raw, "attempts": attempts, "retryable": False, "failure": None,
                    "events": [*events, _event("call_gemini", "completed")]}
        except asyncio.TimeoutError:
            code, retryable = "model_timeout", True
        except GeminiConfigurationError:
            code, retryable = "model_configuration_error", False
        except Exception:
            # SDK errors may contain request details. Keep only a safe category.
            code, retryable = "model_api_failure", True
        return {"attempts": attempts, "raw": None, "failure": code, "retryable": retryable,
                "errors": [*state.get("errors", []), code],
                "events": [*events, _event("call_gemini", code)]}

    def _route_after_model(self, state: PlanningState) -> str:
        if state.get("raw") is not None:
            return "validate"
        return "retry" if state.get("retryable") and state["attempts"] < self.max_attempts else "fail"

    async def _retry_delay(self, state: PlanningState) -> PlanningState:
        await asyncio.sleep(self.retry_delay_seconds)
        return {"events": [*state.get("events", []), _event("retry_delay", "completed")]}

    def _validate(self, state: PlanningState) -> PlanningState:
        try:
            raw = state["raw"]
            if isinstance(raw, ProposedPlan):
                proposal = raw
            elif isinstance(raw, (str, bytes)):
                proposal = ProposedPlan.model_validate_json(raw)
            else:
                proposal = ProposedPlan.model_validate(raw)
        except (ValidationError, ValueError, TypeError, json.JSONDecodeError):
            code = "proposal_schema_invalid"
            return {"proposal": None, "schema_valid": False, "business_valid": False, "failure": code,
                    "errors": [*state.get("errors", []), code],
                    "events": [*state.get("events", []), _event("validate_proposal", code)]}
        try:
            validate_proposal(state["request"], proposal)
        except PlanningError:
            code = "proposal_business_rule_invalid"
            return {"proposal": None, "schema_valid": True, "business_valid": False, "failure": code,
                    "errors": [*state.get("errors", []), code],
                    "events": [*state.get("events", []), _event("validate_proposal", code)]}
        return {"proposal": proposal, "schema_valid": True, "business_valid": True,
                "events": [*state.get("events", []), _event("validate_proposal", "accepted")]}

    def _materialize(self, state: PlanningState) -> PlanningState:
        try:
            plan = materialize_plan(state["request"], state["proposal"])
            return {"plan": plan, "events": [*state.get("events", []), _event("materialize_tasks", "completed")]}
        except Exception:
            # No task can leave this graph when trusted materialization fails.
            code = "plan_materialization_failed"
            return {"failure": code, "errors": [*state.get("errors", []), code],
                    "events": [*state.get("events", []), _event("materialize_tasks", code)]}

    def _safe_failure(self, state: PlanningState) -> PlanningState:
        return {"events": [*state.get("events", []), _event("safe_failure", "completed")]}

    async def aplan(self, request: PlanningRequest | dict[str, Any]) -> dict[str, Any]:
        try:
            validated = request if isinstance(request, PlanningRequest) else PlanningRequest.model_validate(request)
        except ValidationError as exc:
            raise PlanningError("planning request is invalid") from exc
        started = datetime.now(timezone.utc)
        started_clock = time.monotonic()
        state = await self.graph.ainvoke({"request": validated, "attempts": 0, "errors": [], "events": []})
        completed = datetime.now(timezone.utc)
        plan: AcceptedPlan | None = state.get("plan")
        summary = ExecutionSummary(
            workflowId=validated.workflowId, model=self.model,
            generatedPlan=plan.model_dump(mode="json", exclude={"executionSummary"}) if plan else None,
            validationResult=ValidationResult(schemaValid=state.get("schema_valid", False),
                                              businessRulesValid=state.get("business_valid", False),
                                              accepted=plan is not None),
            startedAt=started, completedAt=completed,
            durationMs=max(0, round((time.monotonic() - started_clock) * 1000)),
            retryCount=max(0, state.get("attempts", 0) - 1),
            errors=state.get("errors", []),
            events=state.get("events", []),
            finalPlanningStatus="AwaitingAgents" if plan else "Failed",
        )
        # Event history contains node names and statuses only, never prompts or model reasoning.
        if plan is None:
            failure = PlanningFailure(state.get("failure") or "planning_failed", summary)
            raise failure
        result = plan.model_dump(mode="json")
        result["executionSummary"] = summary.model_dump(mode="json")
        return result

    def plan(self, request: PlanningRequest | dict[str, Any]) -> dict[str, Any]:
        """Synchronous compatibility wrapper for callers outside an event loop."""
        return asyncio.run(self.aplan(request))
