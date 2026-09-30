"""Deterministic procurement analysis tools.

These tools are read-only. They analyse verified inventory shortages and
supplier quotation snapshots supplied by the host application.
"""

from __future__ import annotations

from decimal import Decimal
from typing import Any


class ProcurementAnalysisError(ValueError):
    """Raised when procurement input cannot be safely analysed."""


def _number(value: Any, field: str) -> Decimal:
    try:
        number = Decimal(str(value))
    except Exception as exc:
        raise ProcurementAnalysisError(f"invalid_{field}") from exc

    if number < 0:
        raise ProcurementAnalysisError(f"invalid_{field}")

    return number


def _normalise(value: str) -> str:
    return value.strip().casefold()


def recommend_procurement(
    shortages: list[dict[str, Any]],
    quotations: list[dict[str, Any]],
) -> dict[str, Any]:
    """Compare verified shortages against supplier quotations.

    No database, HTTP, purchase-order creation, reservation, or mutation
    happens here. The host supplies the quotation snapshot.
    """

    if not isinstance(shortages, list):
        raise ProcurementAnalysisError("shortages_required")

    if not isinstance(quotations, list):
        raise ProcurementAnalysisError("quotations_required")

    recommendations: list[dict[str, Any]] = []
    unmatched: list[dict[str, Any]] = []

    for shortage in shortages:
        if not isinstance(shortage, dict):
            raise ProcurementAnalysisError("invalid_shortage")

        name = str(shortage.get("name", "")).strip()
        unit = str(shortage.get("unit", "")).strip()

        quantity = _number(
            shortage.get(
                "shortageQuantity",
                shortage.get("shortage", 0),
            ),
            "shortage_quantity",
        )

        if not name or not unit:
            raise ProcurementAnalysisError(
                "shortage_material_identity_required"
            )

        if quantity <= 0:
            continue

        matching: list[dict[str, Any]] = []

        for quotation in quotations:
            if not isinstance(quotation, dict):
                continue

            quotation_material = str(
                quotation.get("materialName", "")
            )

            quotation_quantity = _number(
                quotation.get("quantity", 0),
                "quotation_quantity",
            )

            is_active = bool(
                quotation.get("isActive", True)
            )

            if (
                _normalise(quotation_material) == _normalise(name)
                and quotation_quantity >= quantity
                and is_active
            ):
                matching.append(quotation)

        if not matching:
            unmatched.append(
                {
                    "materialName": name,
                    "unit": unit,
                    "shortageQuantity": float(quantity),
                    "status": "NoQuotation",
                }
            )
            continue

        candidates: list[dict[str, Any]] = []

        for quotation in matching:
            supplier_id = quotation.get("supplierId")
            supplier_name = quotation.get(
                "supplierName",
                "Unknown Supplier",
            )

            quotation_quantity = _number(
                quotation.get("quantity", 0),
                "quotation_quantity",
            )

            unit_price = _number(
                quotation.get("unitPrice", 0),
                "unit_price",
            )

            total_price = quotation.get("totalPrice")

            if total_price is None:
                total_price = quantity * unit_price
            else:
                total_price = _number(
                    total_price,
                    "total_price",
                )

            candidates.append(
                {
                    "quotationId": quotation.get("quotationId"),
                    "supplierId": supplier_id,
                    "supplierName": supplier_name,
                    "materialName": name,
                    "requiredQuantity": float(quantity),
                    "quotedQuantity": float(quotation_quantity),
                    "unitPrice": float(unit_price),
                    "totalPrice": float(total_price),
                    "deliveryDate": quotation.get("deliveryDate"),
                    "leadTimeDays": quotation.get("leadTimeDays"),
                }
            )

        candidates.sort(
            key=lambda item: (
                item["totalPrice"],
                item["unitPrice"],
                item["quotationId"] or 0,
            )
        )

        selected = candidates[0]

        recommendations.append(
            {
                "materialName": name,
                "unit": unit,
                "shortageQuantity": float(quantity),
                "status": "RecommendationAvailable",
                "recommendedSupplier": selected,
                "alternatives": candidates[1:],
            }
        )

    has_shortage = any(
        _number(
            item.get(
                "shortageQuantity",
                item.get("shortage", 0),
            ),
            "shortage_quantity",
        ) > 0
        for item in shortages
        if isinstance(item, dict)
    )

    if not shortages or not has_shortage:
        overall_status = "NoShortages"
    elif recommendations:
        overall_status = "RecommendationsAvailable"
    else:
        overall_status = "NoQuotation"

    return {
        "recommendations": recommendations,
        "unmatched": unmatched,
        "overallStatus": overall_status,
        "approvalRequired": True,
        "sideEffects": [],
    }
