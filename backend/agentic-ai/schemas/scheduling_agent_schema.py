"""Schemas for the read-only SchedulingValidationAgent."""

from __future__ import annotations

from datetime import datetime
from typing import Any

from pydantic import BaseModel, ConfigDict, Field


class DeliverySnapshot(BaseModel):
    model_config = ConfigDict(extra="ignore")

    deliveryId: int = Field(gt=0)
    purchaseOrderId: int = Field(gt=0)
    status: str = Field(min_length=1)
    deliveryDate: datetime
    quantity: float


class PurchaseOrderSnapshot(BaseModel):
    model_config = ConfigDict(extra="ignore")

    purchaseOrderId: int = Field(gt=0)
    quantity: float = Field(ge=0)


class SchedulingTaskInput(BaseModel):
    model_config = ConfigDict(extra="ignore")

    workflowId: str = Field(min_length=1)
    delivery: DeliverySnapshot
    purchaseOrder: PurchaseOrderSnapshot | None = None


class SchedulingValidationResult(BaseModel):
    model_config = ConfigDict(extra="ignore")

    schemaVersion: str = "1.0"
    task_id: str | None = None
    workflowId: str | None = None
    agent: str = "SchedulingValidationAgent"
    status: str
    validationStatus: str
    message: str
    reasons: list[str] = []
    approvalRequired: bool = True
    sideEffects: list[Any] = []
    executionSummary: dict[str, Any]
    error: str | None = None
