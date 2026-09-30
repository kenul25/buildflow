"""Pure inventory analysis helpers.

The agent receives a read-only inventory snapshot from its host. These helpers
never reserve, issue, or mutate inventory.
"""
from __future__ import annotations

from collections import defaultdict
from decimal import Decimal, InvalidOperation
from typing import Any, Iterable


class InventoryAnalysisError(ValueError):
	"""Raised when an inventory task or snapshot violates its contract."""


def _decimal(value: Any, field: str) -> Decimal:
	try:
		result = Decimal(str(value))
	except (InvalidOperation, TypeError, ValueError) as exc:
		raise InventoryAnalysisError(f"{field} must be a finite number") from exc
	if not result.is_finite() or result < 0:
		raise InventoryAnalysisError(f"{field} must be a non-negative finite number")
	return result


def normalize_requirements(requirements: Iterable[dict[str, Any]]) -> list[dict[str, Any]]:
	totals: defaultdict[tuple[str, str], Decimal] = defaultdict(Decimal)
	for item in requirements:
		if not isinstance(item, dict):
			raise InventoryAnalysisError("each material requirement must be an object")
		name = item.get("name")
		unit = item.get("unit")
		if not isinstance(name, str) or not name.strip() or not isinstance(unit, str) or not unit.strip():
			raise InventoryAnalysisError("each material requirement needs a name and unit")
		quantity = _decimal(item.get("quantity"), "requirement quantity")
		if quantity <= 0:
			raise InventoryAnalysisError("requirement quantity must be greater than zero")
		key = (name.strip().casefold(), unit.strip().casefold())
		totals[key] += quantity
	return [
		{"name": name, "unit": unit, "required": quantity}
		for (name, unit), quantity in sorted(totals.items())
	]


def normalize_inventory(snapshot: Iterable[dict[str, Any]]) -> dict[tuple[str, str], dict[str, Decimal]]:
	totals: defaultdict[tuple[str, str], dict[str, Decimal]] = defaultdict(
		lambda: {"current": Decimal("0"), "reserved": Decimal("0")}
	)
	for item in snapshot:
		if not isinstance(item, dict):
			raise InventoryAnalysisError("each inventory record must be an object")
		name = item.get("name")
		unit = item.get("unit")
		if not isinstance(name, str) or not name.strip() or not isinstance(unit, str) or not unit.strip():
			raise InventoryAnalysisError("each inventory record needs a name and unit")
		current = _decimal(item.get("currentStock", item.get("current")), "current stock")
		reserved = _decimal(item.get("reservedStock", item.get("reserved", 0)), "reserved stock")
		if reserved > current:
			raise InventoryAnalysisError("reserved stock cannot exceed current stock")
		key = (name.strip().casefold(), unit.strip().casefold())
		totals[key]["current"] += current
		totals[key]["reserved"] += reserved
		material_id = item.get("materialId")
		if material_id and not totals[key].get("material_id"):
			totals[key]["material_id"] = material_id
	for amounts in totals.values():
		if amounts["reserved"] > amounts["current"]:
			raise InventoryAnalysisError("aggregated reserved stock cannot exceed current stock")
	return dict(totals)


def build_report(requirements: Iterable[dict[str, Any]], snapshot: Iterable[dict[str, Any]]) -> dict[str, Any]:
	normalized_requirements = normalize_requirements(requirements)
	inventory = normalize_inventory(snapshot)
	items: list[dict[str, Any]] = []
	for requirement in normalized_requirements:
		key = (requirement["name"].casefold(), requirement["unit"].casefold())
		amounts = inventory.get(key, {"current": Decimal("0"), "reserved": Decimal("0")})
		current = amounts["current"]
		reserved = amounts["reserved"]
		available = current - reserved
		shortage = max(Decimal("0"), requirement["required"] - available)
		item = {
			"materialId": amounts.get("material_id"), "name": requirement["name"], "unit": requirement["unit"],
			"required": float(requirement["required"]), "currentStock": float(current),
			"reservedStock": float(reserved), "availableStock": float(available),
			"shortage": float(shortage), "isAvailable": shortage == 0,
			"requiredQuantity": float(requirement["required"]), "availableQuantity": float(available),
			"shortageQuantity": float(shortage), "status": "AVAILABLE" if shortage == 0 else "SHORTAGE",
		}
		items.append(item)
	shortages = [item for item in items if not item["isAvailable"]]
	return {
		"schemaVersion": "1.0", "agent": "InventoryAgent", "status": "Completed",
		"items": items, "shortages": shortages, "isFullyAvailable": not shortages,
		"materials": items, "overallStatus": "AVAILABLE" if not shortages else "SHORTAGE_FOUND",
		"approvalRequired": True, "sideEffects": [],
	}
