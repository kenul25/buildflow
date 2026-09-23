"""Versioned HTTP, Gemini proposal, and accepted planning contracts."""
from __future__ import annotations

from datetime import date, datetime, timezone
from decimal import Decimal
from typing import Any, Literal
from uuid import UUID

from pydantic import BaseModel, ConfigDict, Field, field_validator, model_validator


class WireModel(BaseModel):
    model_config = ConfigDict(extra="ignore", populate_by_name=True)


class ResourceItem(WireModel):
    kind: Literal["Material", "Equipment", "Workforce"]
    name: str = Field(min_length=2, max_length=160)
    quantity: Decimal = Field(gt=0, le=999999999, allow_inf_nan=False)
    unit: str = Field(min_length=1, max_length=32)

    @field_validator("name", "unit")
    @classmethod
    def not_blank(cls, value: str) -> str:
        value = value.strip()
        if not value:
            raise ValueError("resource name and unit cannot be blank")
        return value


class PlanningRequest(WireModel):
    workflowId: UUID
    requestId: UUID
    projectId: UUID
    siteId: UUID
    activityId: UUID
    objective: str = Field(min_length=10, max_length=2000)
    requiredBy: date | None = None
    budgetLimit: Decimal | None = Field(default=None, ge=0, allow_inf_nan=False)
    projectName: str | None = Field(default=None, max_length=160)
    siteName: str | None = Field(default=None, max_length=160)
    siteAddress: str | None = Field(default=None, max_length=500)
    activityName: str | None = Field(default=None, max_length=160)
    activityDueDate: date | None = None
    items: list[ResourceItem] = Field(min_length=1, max_length=50)

    @field_validator("objective")
    @classmethod
    def clean_objective(cls, value: str) -> str:
        value = value.strip()
        if len(value) < 10:
            raise ValueError("objective must contain at least ten characters")
        return value

    @model_validator(mode="after")
    def future_deadline(self) -> "PlanningRequest":
        if self.requiredBy is not None and self.requiredBy < datetime.now(timezone.utc).date():
            raise ValueError("requiredBy cannot be in the past")
        return self


TaskAgent = Literal["InventoryAnalysisAgent", "ProcurementAgent", "SchedulingValidationAgent"]
TaskAction = Literal["AnalyzeAvailability", "RecommendProcurement", "ProposeAndValidateSchedule"]
StepKind = Literal["AnalyzeNeeds", "InventoryCheck", "ProcurementReview", "ScheduleValidation", "ManagerApproval"]


class StrictProposalModel(BaseModel):
    model_config = ConfigDict(extra="forbid")


class ProposedAnalysis(StrictProposalModel):
    materialItems: int = Field(ge=0, le=50)
    equipmentItems: int = Field(ge=0, le=50)
    workforceItems: int = Field(ge=0, le=50)
    daysUntilDue: int | None = Field(default=None, ge=0)
    budgetConstrained: bool
    priority: Literal["Urgent", "Normal"]
    risks: list[str] = Field(default_factory=list, max_length=8)

    @field_validator("risks")
    @classmethod
    def bounded_risks(cls, values: list[str]) -> list[str]:
        if any(not 5 <= len(value.strip()) <= 300 for value in values):
            raise ValueError("risk notes must be 5 to 300 characters")
        return values


class ProposedStep(StrictProposalModel):
    sequence: int = Field(ge=1, le=5)
    kind: StepKind
    title: str = Field(min_length=5, max_length=160)
    description: str = Field(min_length=10, max_length=500)


class ProposedTask(StrictProposalModel):
    agent: TaskAgent
    action: TaskAction
    dependsOn: list[TaskAgent] = Field(max_length=2)


class ProposedPlan(StrictProposalModel):
    schemaVersion: Literal["1.0"]
    objective: str = Field(min_length=10, max_length=2000)
    analysis: ProposedAnalysis
    steps: list[ProposedStep] = Field(min_length=5, max_length=5)
    tasks: list[ProposedTask] = Field(min_length=3, max_length=3)
    approvalRequired: Literal[True]


class AcceptedStep(WireModel):
    sequence: int
    title: str
    description: str


class AcceptedTask(WireModel):
    task_id: UUID
    agent: Literal["InventoryAgent", "ProcurementAgent", "SchedulingValidationAgent"]
    action: TaskAction
    depends_on: list[UUID]
    input: dict[str, Any]
    status: Literal["Pending"] = "Pending"


class ValidationResult(WireModel):
    schemaValid: bool
    businessRulesValid: bool
    accepted: bool


class ExecutionSummary(WireModel):
    agentName: Literal["PlanningAgent"] = "PlanningAgent"
    workflowId: UUID
    model: str
    generatedPlan: dict[str, Any] | None
    validationResult: ValidationResult
    startedAt: datetime
    completedAt: datetime
    durationMs: int = Field(ge=0)
    retryCount: int = Field(ge=0)
    errors: list[str]
    events: list[dict[str, str]] = Field(default_factory=list)
    finalPlanningStatus: Literal["AwaitingAgents", "Failed"]


class AcceptedPlan(WireModel):
    schemaVersion: Literal["1.0"] = "1.0"
    workflowId: UUID
    status: Literal["AwaitingAgents"] = "AwaitingAgents"
    objective: str
    analysis: ProposedAnalysis
    steps: list[AcceptedStep] = Field(min_length=5, max_length=5)
    tasks: list[AcceptedTask] = Field(min_length=3, max_length=3)
    approvalRequired: Literal[True] = True
    executionSummary: ExecutionSummary | None = None
