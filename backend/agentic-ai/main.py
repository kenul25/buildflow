"""Private FastAPI entry point for ASP.NET Core planning requests."""
from __future__ import annotations

import os
from hmac import compare_digest
from pathlib import Path

from dotenv import load_dotenv
from fastapi import Depends, FastAPI, Header, HTTPException, Request
from fastapi.responses import JSONResponse

from agents.planning_agent import PlanningAgent, PlanningFailure
from schemas.planning_schema import AcceptedPlan, PlanningRequest


def load_local_env(path: Path | None = None) -> None:
    """Load local server settings without overriding deployed environment values."""
    load_dotenv(path or Path(__file__).with_name(".env"), override=False, interpolate=False)


load_local_env()
app = FastAPI(title="BuildFlow internal planning service", docs_url=None, redoc_url=None, openapi_url=None)


@app.middleware("http")
async def limit_body(request: Request, call_next):
    if request.method == "POST":
        body = await request.body()
        limit = 1048576 if request.url.path == "/internal/agents" else 65536
        if len(body) > limit:
            return JSONResponse(status_code=413, content={"error": "request_too_large"})
    return await call_next(request)


def require_internal_key(x_buildflow_key: str | None = Header(default=None)) -> None:
    expected = os.environ.get("BUILDFLOW_INTERNAL_KEY")
    if not expected or expected.startswith("replace-with-") or not x_buildflow_key or not compare_digest(x_buildflow_key, expected):
        raise HTTPException(status_code=401, detail="invalid internal key")


def planner_for(request: Request) -> PlanningAgent:
    planner = getattr(request.app.state, "planner", None)
    if planner is None:
        planner = PlanningAgent()
        request.app.state.planner = planner
    return planner


@app.exception_handler(PlanningFailure)
async def planning_failure_handler(_request: Request, error: PlanningFailure):
    status = 422 if error.code in ("proposal_schema_invalid", "proposal_business_rule_invalid") else 503
    return JSONResponse(status_code=status, content={
        "error": error.code,
        "executionSummary": error.summary.model_dump(mode="json"),
    })


@app.post("/internal/plans", response_model=AcceptedPlan,
          dependencies=[Depends(require_internal_key)])
async def internal_plan(request: PlanningRequest, planner: PlanningAgent = Depends(planner_for)):
    return await planner.aplan(request)


@app.get("/health")
async def health():
    return {"status": "ok"}


@app.post("/internal/agents", dependencies=[Depends(require_internal_key)])
async def execute_agent(task: dict):
    from agents.inventory_agent import InventoryAgent
    from agents.procurement_agent import ProcurementAgent
    from agents.scheduling_validation_agent import SchedulingValidationAgent
    agents = {"InventoryAgent": InventoryAgent(), "ProcurementAgent": ProcurementAgent(), "SchedulingValidationAgent": SchedulingValidationAgent()}
    agent = agents.get(task.get("agent"))
    if agent is None:
        raise HTTPException(status_code=400, detail="unsupported_agent")
    if task.get("agent") == "InventoryAgent":
        if task.get("action") != "AnalyzeAvailability" or not isinstance(task.get("input"), dict):
            raise HTTPException(status_code=400, detail="unsupported_inventory_task")
        data = task.get("input", {})
        data["items"] = [{"name": i["name"], "quantity": i["quantity"], "unit": i["unit"]} for i in data.get("items", []) if i.get("kind", "Material") == "Material"]
        if not data["items"] or data.get("inventorySnapshot") == []:
            items = [{"name": i["name"], "unit": i["unit"], "requiredQuantity": i["quantity"], "availableQuantity": 0, "shortageQuantity": i["quantity"]} for i in data["items"]]
            return {"schemaVersion": "1.0", "task_id": task.get("task_id"), "agent": "InventoryAgent", "status": "Completed", "output": {"items": items}, "error": None}
        result = agent.execute(task)
        return {"schemaVersion": "1.0", "task_id": result["task_id"], "agent": "InventoryAgent", "status": result["status"], "output": {"items": result.get("materials", [])}, "error": result.get("error")}
    return agent.execute(task)


if __name__ == "__main__":
    import uvicorn

    internal_key = os.environ.get("BUILDFLOW_INTERNAL_KEY")
    if not internal_key or internal_key.startswith("replace-with-"):
        raise SystemExit("A real BUILDFLOW_INTERNAL_KEY is required")
    uvicorn.run(app, host="127.0.0.1", port=int(os.environ.get("PORT", "8000")), access_log=False)
