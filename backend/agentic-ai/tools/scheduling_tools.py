from __future__ import annotations

from datetime import datetime
from decimal import Decimal
from typing import Any


class SchedulingValidationError(ValueError):
    pass


def validate_schedule_input(data: dict[str, Any]) -> list[str]:
    errors: list[str] = []

    start = data.get("proposedStart")
    end = data.get("proposedEnd")

    if not isinstance(start, datetime) or not isinstance(end, datetime):
        errors.append("invalid_schedule_time")
    elif start >= end:
        errors.append("start_time_must_be_before_end_time")

    required_count = data.get("requiredWorkerCount", 1)
    workers = data.get("workers") or []
    available_workers = [w for w in workers if w.get("available")]

    if len(available_workers) < required_count:
        errors.append("insufficient_available_workers")

    required_skills = {
        str(skill).strip().lower()
        for skill in (data.get("requiredSkills") or [])
        if str(skill).strip()
    }

    if required_skills:
        qualified = []
        for worker in available_workers:
            worker_skills = {
                str(skill).strip().lower()
                for skill in (worker.get("skills") or [])
            }
            if required_skills.issubset(worker_skills):
                qualified.append(worker)

        if len(qualified) < required_count:
            errors.append("required_worker_skills_not_available")

    equipment = data.get("equipment") or []
    operational_equipment = [
        item for item in equipment
        if item.get("available") and item.get("status") == "Available"
    ]

    requested_equipment_count = int(data.get("requiredEquipmentCount", 0) or 0)

    if requested_equipment_count > 0 and len(operational_equipment) < requested_equipment_count:
        errors.append("insufficient_available_equipment")

    dependency_ready = data.get("dependencyReady", True)
    if dependency_ready is False:
        errors.append("dependencies_not_ready")

    supplier_delivery = data.get("supplierDeliveryDate")
    deadline = data.get("constructionDeadline")

    if supplier_delivery is not None and deadline is not None:
        if supplier_delivery > deadline:
            errors.append("supplier_delivery_after_construction_deadline")

    procurement_cost = data.get("procurementCost")
    budget_limit = data.get("budgetLimit")

    if procurement_cost is not None and budget_limit is not None:
        try:
            if Decimal(str(procurement_cost)) > Decimal(str(budget_limit)):
                errors.append("procurement_cost_exceeds_budget")
        except (ArithmeticError, ValueError):
            errors.append("invalid_procurement_cost")

    return errors


def build_schedule_report(data: dict[str, Any]) -> dict[str, Any]:
    errors = validate_schedule_input(data)

    start = data.get("proposedStart")
    end = data.get("proposedEnd")

    workers = [
        worker for worker in (data.get("workers") or [])
        if worker.get("available")
    ]

    required_skills = {
        str(skill).strip().lower()
        for skill in (data.get("requiredSkills") or [])
        if str(skill).strip()
    }

    qualified_workers = []
    for worker in workers:
        skills = {
            str(skill).strip().lower()
            for skill in (worker.get("skills") or [])
        }
        if required_skills.issubset(skills):
            qualified_workers.append(worker)

    required_count = int(data.get("requiredWorkerCount", 1) or 1)
    selected_workers = qualified_workers[:required_count]

    equipment = [
        item for item in (data.get("equipment") or [])
        if item.get("available") and item.get("status") == "Available"
    ]

    selected_equipment = equipment[
        : int(data.get("requiredEquipmentCount", 0) or 0)
    ]

    accepted = not errors

    risks: list[str] = []
    if data.get("supplierDeliveryDate") is None:
        risks.append("supplier_delivery_date_not_confirmed")
    if data.get("requiredSkills"):
        risks.append("worker_selection_depends_on_required_skills")
    if not data.get("equipment"):
        risks.append("no_equipment_candidate_data_provided")

    return {
        "accepted": accepted,
        "workerIds": [worker.get("workerId") for worker in selected_workers],
        "equipmentIds": [item.get("equipmentId") for item in selected_equipment],
        "startTime": start.isoformat() if isinstance(start, datetime) else None,
        "endTime": end.isoformat() if isinstance(end, datetime) else None,
        "procurementCost": data.get("procurementCost"),
        "validationErrors": errors,
        "risks": risks,
    }
