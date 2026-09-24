"""Strict, versioned contracts for the read-only inventory analysis agent."""
from __future__ import annotations

from decimal import Decimal
from datetime import datetime
from typing import Any, Literal

from pydantic import BaseModel, ConfigDict, Field, model_validator


class InventoryContract(BaseModel):
    model_config = ConfigDict(extra="forbid", populate_by_name=True)


class InventoryRequirement(InventoryContract):
    name: str = Field(min_length=2, max_length=160)
    quantity: Decimal = Field(gt=0, le=999999999, allow_inf_nan=False)
    unit: str = Field(min_length=1, max_length=32)


class InventorySnapshot(InventoryContract):
    material_id: str | None = Field(default=None, alias="materialId", min_length=1, max_length=128)
    name: str = Field(min_length=2, max_length=160)
    unit: str = Field(min_length=1, max_length=32)
    current_stock: Decimal = Field(alias="currentStock", ge=0, le=999999999, allow_inf_nan=False)
    reserved_stock: Decimal = Field(alias="reservedStock", ge=0, le=999999999, allow_inf_nan=False)

    @model_validator(mode="after")
    def reserved_within_current(self) -> "InventorySnapshot":
        if self.reserved_stock > self.current_stock:
            raise ValueError("reservedStock cannot exceed currentStock")
        return self


class InventoryTaskInput(BaseModel):
    model_config = ConfigDict(extra="ignore", populate_by_name=True)

    workflow_id: str | None = Field(default=None, alias="workflowId", min_length=1, max_length=128)
    project_id: str | None = Field(default=None, alias="projectId", min_length=1, max_length=128)
    site_id: str | None = Field(default=None, alias="siteId", min_length=1, max_length=128)
    items: list[InventoryRequirement] = Field(min_length=1, max_length=50)
    inventory_snapshot: list[InventorySnapshot] = Field(alias="inventorySnapshot", min_length=1, max_length=10000)


class InventoryMaterialResult(InventoryContract):
    material_id: str | None = Field(default=None, alias="materialId")
    name: str
    unit: str
    required_quantity: Decimal = Field(alias="requiredQuantity", ge=0)
    available_quantity: Decimal = Field(alias="availableQuantity", ge=0)
    shortage_quantity: Decimal = Field(alias="shortageQuantity", ge=0)
    status: Literal["AVAILABLE", "SHORTAGE"]


class InventoryExecutionSummary(InventoryContract):
    agent_name: Literal["InventoryAgent"] = Field(alias="agentName")
    validated: bool
    side_effects: list[Any] = Field(default_factory=list, alias="sideEffects")
    completed_at: datetime = Field(alias="completedAt")


class InventoryAgentResult(InventoryContract):
    schema_version: Literal["1.0"] = Field(alias="schemaVersion")
    task_id: str | None = None
    workflow_id: str | None = Field(default=None, alias="workflowId")
    agent: Literal["InventoryAgent"]
    status: Literal["Completed", "Failed"]
    materials: list[InventoryMaterialResult] = Field(default_factory=list)
    overall_status: Literal["AVAILABLE", "SHORTAGE_FOUND", "FAILED"] = Field(alias="overallStatus")
    approval_required: Literal[True] = Field(alias="approvalRequired")
    side_effects: list[Any] = Field(default_factory=list, alias="sideEffects")
    execution_summary: InventoryExecutionSummary = Field(alias="executionSummary")
    error: str | None = None