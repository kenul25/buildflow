"""Read-only procurement recommendation agent.

The host/backend provides verified quotation data. This agent compares
eligible quotations and returns a recommendation without creating orders,
changing suppliers, or mutating the database.
"""
from __future__ import annotations

from datetime import datetime, timezone
from decimal import Decimal
from typing import Any

from agents.deterministic_control import AgentTask
from pydantic import BaseModel, ConfigDict, Field


class ProcurementAnalysisError(ValueError):
    """Raised when procurement input cannot be safely analysed."""


class Quotation(BaseModel):
    model_config = ConfigDict(extra="ignore")

    quotationId: int
    supplierId: int
    supplierName: str
    materialName: str
    quantity: Decimal = Field(gt=0)
    unitPrice: Decimal = Field(ge=0)
    deliveryDate: datetime
    totalPrice: Decimal = Field(ge=0)


class ProcurementInput(BaseModel):
    model_config = ConfigDict(extra="ignore")

    workflowId: str
    materialName: str = Field(min_length=2)
    requiredQuantity: Decimal = Field(gt=0)
    quotations: list[Quotation] = Field(default_factory=list)


class ProcurementAgent:
    """Compare verified supplier quotations without side effects."""

    name = "ProcurementAgent"
    canonical_name = "ProcurementAgent"

    def execute(self, task: AgentTask | dict[str, Any]) -> dict[str, Any]:
        task_data = task.__dict__ if isinstance(task, AgentTask) else task

        task_id = (
            task_data.get("task_id")
            if isinstance(task_data, dict)
            else None
        )

        try:
            if not isinstance(task_data, dict):
                raise ProcurementAnalysisError("invalid_procurement_task")

            if task_data.get("agent") not in (
                self.name,
                self.canonical_name,
            ):
                raise ProcurementAnalysisError("unsupported_procurement_agent")

            if task_data.get("action") != "RecommendProcurement":
                raise ProcurementAnalysisError("unsupported_procurement_action")

            if not isinstance(task_data.get("input"), dict):
                raise ProcurementAnalysisError("procurement_input_required")

            task_input = ProcurementInput.model_validate(task_data["input"])

            result = recommend_procurement(task_input)

            return {
                "schemaVersion": "1.0",
                "task_id": task_id,
                "workflowId": task_input.workflowId,
                "agent": self.name,
                "status": "Completed",
                "output": result,
                "error": None,
                "approvalRequired": True,
                "sideEffects": [],
                "executionSummary": {
                    "agentName": self.name,
                    "validated": True,
                    "sideEffects": [],
                    "completedAt": datetime.now(timezone.utc),
                },
            }

        except Exception as error:
            code = (
                error.args[0]
                if isinstance(error, ProcurementAnalysisError) and error.args
                else "procurement_analysis_failed"
            )

            return {
                "schemaVersion": "1.0",
                "task_id": task_id,
                "agent": self.name,
                "status": "Failed",
                "output": {},
                "error": str(code),
                "approvalRequired": True,
                "sideEffects": [],
                "executionSummary": {
                    "agentName": self.name,
                    "validated": True,
                    "sideEffects": [],
                    "completedAt": datetime.now(timezone.utc),
                },
            }


def recommend_procurement(
    task_input: ProcurementInput,
) -> dict[str, Any]:
    """Compare quotations and return a safe procurement recommendation."""

    material = task_input.materialName.strip().lower()
    required = task_input.requiredQuantity

    eligible = [
        quotation
        for quotation in task_input.quotations
        if quotation.materialName.strip().lower() == material
        and quotation.quantity >= required
    ]

    if not eligible:
        return {
            "materialName": task_input.materialName,
            "requiredQuantity": float(required),
            "quotationCount": 0,
            "status": "NoQuotationAvailable",
            "recommendedQuotation": None,
            "alternatives": [],
            "approvalRequired": True,
            "sideEffects": [],
        }

    # Deterministic ordering: lowest total cost first, then earliest delivery.
    eligible.sort(
        key=lambda quotation: (
            quotation.totalPrice,
            quotation.deliveryDate,
            quotation.quotationId,
        )
    )

    recommended = eligible[0]

    def serialize(quotation: Quotation) -> dict[str, Any]:
        return {
            "quotationId": quotation.quotationId,
            "supplierId": quotation.supplierId,
            "supplierName": quotation.supplierName,
            "materialName": quotation.materialName,
            "quantity": float(quotation.quantity),
            "unitPrice": float(quotation.unitPrice),
            "deliveryDate": quotation.deliveryDate.isoformat(),
            "totalPrice": float(quotation.totalPrice),
        }

    return {
        "materialName": task_input.materialName,
        "requiredQuantity": float(required),
        "quotationCount": len(eligible),
        "status": "QuotationAvailable",
        "recommendedQuotation": serialize(recommended),
        "alternatives": [
            serialize(quotation)
            for quotation in eligible[1:]
        ],
        "approvalRequired": True,
        "sideEffects": [],
    }
