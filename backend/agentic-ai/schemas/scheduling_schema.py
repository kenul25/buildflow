from __future__ import annotations

from datetime import date, datetime
from decimal import Decimal
from typing import Literal
from uuid import UUID

from pydantic import BaseModel, Field


class WorkerCandidate(BaseModel):
    workerId: UUID
    name: str = Field(min_length=1, max_length=160)
    skills: list[str] = Field(default_factory=list)
    available: bool
    shiftStart: datetime | None = None
    shiftEnd: datetime | None = None


class EquipmentCandidate(BaseModel):
    equipmentId: UUID
    code: str = Field(min_length=1, max_length=50)
    name: str = Field(min_length=1, max_length=160)
    status: str = Field(min_length=1, max_length=50)
    available: bool


class SchedulingRequest(BaseModel):
    schemaVersion: Literal["1.0"] = "1.0"
    workflowId: UUID
    taskId: UUID
    activityId: UUID
    activityName: str = Field(min_length=1, max_length=160)
    requiredSkills: list[str] = Field(default_factory=list, max_length=50)
    requiredWorkerCount: int = Field(default=1, ge=1, le=100)
    workers: list[WorkerCandidate] = Field(default_factory=list, max_length=500)
    equipment: list[EquipmentCandidate] = Field(default_factory=list, max_length=500)
    proposedStart: datetime
    proposedEnd: datetime
    dependencyReady: bool = True
    supplierDeliveryDate: date | None = None
    constructionDeadline: date | None = None
    procurementCost: Decimal | None = Field(default=None, ge=0)
    budgetLimit: Decimal | None = Field(default=None, ge=0)


class ScheduleProposal(BaseModel):
    schemaVersion: Literal["1.0"] = "1.0"
    activityId: UUID
    startTime: datetime
    endTime: datetime
    workerIds: list[UUID]
    equipmentIds: list[UUID]
    procurementCost: Decimal | None = None
    risks: list[str] = Field(default_factory=list, max_length=20)


class SchedulingValidationResult(BaseModel):
    schemaVersion: Literal["1.0"] = "1.0"
    task_id: UUID
    agent: Literal["SchedulingValidationAgent"] = "SchedulingValidationAgent"
    status: Literal["Completed", "Failed"]
    accepted: bool
    approvalRequired: Literal[True] = True
    proposal: ScheduleProposal | None = None
    validationErrors: list[str] = Field(default_factory=list)
    risks: list[str] = Field(default_factory=list)
    sideEffects: list[str] = Field(default_factory=list)
    completedAt: datetime
    error: str | None = None
