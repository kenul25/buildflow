"""Deterministic, read-only scheduling and delivery validation tools."""

from __future__ import annotations

from datetime import datetime, timezone
from typing import Any


class SchedulingValidationError(ValueError):
    """Raised when scheduling input cannot be safely validated."""


def validate_delivery(
    delivery: dict[str, Any],
    purchase_order: dict[str, Any] | None,
) -> dict[str, Any]:
    """
    Validate a delivery using the same business rules as the ASP.NET
    SchedulingValidationAgent.

    This function is read-only and has no database or mutation capability.
    """

    if not isinstance(delivery, dict):
        raise SchedulingValidationError("invalid_delivery")

    delivery_id = delivery.get("deliveryId")
    purchase_order_id = delivery.get("purchaseOrderId")
    status = str(delivery.get("status", "")).strip()
    delivery_date = delivery.get("deliveryDate")
    quantity = delivery.get("quantity")

    if delivery_id is None:
        raise SchedulingValidationError("delivery_id_required")

    if purchase_order_id is None:
        raise SchedulingValidationError("purchase_order_id_required")

    if not status:
        raise SchedulingValidationError("delivery_status_required")

    if delivery_date is None:
        raise SchedulingValidationError("delivery_date_required")

    if quantity is None:
        raise SchedulingValidationError("delivery_quantity_required")

    try:
        quantity_value = float(quantity)
    except (TypeError, ValueError) as exc:
        raise SchedulingValidationError("invalid_delivery_quantity") from exc

    try:
        if isinstance(delivery_date, datetime):
            parsed_delivery_date = delivery_date
        else:
            parsed_delivery_date = datetime.fromisoformat(
                str(delivery_date).replace("Z", "+00:00")
            )
    except (TypeError, ValueError) as exc:
        raise SchedulingValidationError("invalid_delivery_date") from exc

    if parsed_delivery_date.tzinfo is None:
        parsed_delivery_date = parsed_delivery_date.replace(
            tzinfo=timezone.utc
        )

    if status.casefold() == "completed":
        return {
            "validationStatus": "Invalid",
            "message": "Delivery is already completed",
            "reasons": ["Delivery is already completed"],
        }

    if parsed_delivery_date < datetime.now(timezone.utc):
        return {
            "validationStatus": "Invalid",
            "message": "Delivery date has passed",
            "reasons": ["Delivery date has passed"],
        }

    if quantity_value <= 0:
        return {
            "validationStatus": "Invalid",
            "message": "Invalid delivery quantity",
            "reasons": ["Invalid delivery quantity"],
        }

    if purchase_order is None:
        return {
            "validationStatus": "Invalid",
            "message": "Purchase order not found",
            "reasons": ["Purchase order not found"],
        }

    if not isinstance(purchase_order, dict):
        raise SchedulingValidationError("invalid_purchase_order")

    po_id = purchase_order.get("purchaseOrderId")
    po_quantity = purchase_order.get("quantity")

    if po_id is None or po_quantity is None:
        raise SchedulingValidationError("invalid_purchase_order")

    try:
        po_quantity_value = float(po_quantity)
    except (TypeError, ValueError) as exc:
        raise SchedulingValidationError("invalid_purchase_order_quantity") from exc

    if po_id != purchase_order_id:
        return {
            "validationStatus": "Invalid",
            "message": "Purchase order not found",
            "reasons": ["Purchase order not found"],
        }

    if quantity_value > po_quantity_value:
        return {
            "validationStatus": "Invalid",
            "message": "Delivery quantity exceeds purchase order quantity",
            "reasons": ["Delivery quantity exceeds purchase order quantity"],
        }

    return {
        "validationStatus": "Valid",
        "message": "Delivery is valid",
        "reasons": [],
    }
