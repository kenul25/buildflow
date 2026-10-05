"""Deterministic, read-only scheduling from verified backend snapshots."""
from datetime import datetime, timedelta, timezone
from decimal import Decimal
from pydantic import BaseModel, Field, ConfigDict


class ScheduleProposal(BaseModel):
    model_config = ConfigDict(extra="forbid")
    activityId: str
    startTime: datetime
    endTime: datetime
    workers: list[dict] = Field(default_factory=list)
    equipment: list[dict] = Field(default_factory=list)
    validation: dict
    status: str


def instant(value):
    result = datetime.fromisoformat(str(value).replace("Z", "+00:00"))
    return result.replace(tzinfo=timezone.utc) if result.tzinfo is None else result.astimezone(timezone.utc)


def resource_hours(item):
    if item['kind'] == 'Material' or item.get('resourceCount') is None:
        return Decimal(1)
    factors = {'hours': 1, 'shifts': 8, 'days': 8, 'mandays': 8, 'weeks': 40, 'workers': 0}
    factor = factors.get(item['unit'].lower())
    if factor is None:
        raise ValueError('Invalid resource usage unit')
    return Decimal(str(item['quantity'])) * factor / item['resourceCount'] if factor else Decimal(1)


def propose(data):
    snapshot = data.get("schedulingSnapshot")
    if not isinstance(snapshot, dict):
        raise ValueError("scheduling_snapshot_required")
    now = instant(snapshot["now"])
    candidates = {now + timedelta(minutes=5)}
    minimum_start = now + timedelta(minutes=5)
    for shift in snapshot.get("shifts", []):
        if instant(shift["startTime"]) > now:
            candidates.add(instant(shift["startTime"]))
    procurement = data.get("procurementResult", {})
    for item in procurement.get("recommendations", []):
        selected = item.get("recommendedSupplier", {})
        if selected.get("deliveryDate"):
            minimum_start = max(minimum_start, instant(selected["deliveryDate"]))
            candidates.add(minimum_start)
    issues = []
    stock = data.get("inventoryResult", {}).get("items", [])
    for requirement in stock:
        shortage = Decimal(str(requirement.get("shortageQuantity", requirement.get("shortage", 0))))
        if shortage > 0 and not any(r.get("materialName", "").casefold() == requirement.get("name", "").casefold() and r.get("unit", "").casefold() == requirement.get("unit", "").casefold() for r in procurement.get("recommendations", [])):
            issues.append("Uncovered material shortage")
    # Evaluate shift starts and booking ends too; do not assume the first worker is free.
    candidates.add(minimum_start)
    for booking in snapshot.get("assignments", []) + snapshot.get("reservations", []):
        candidates.add(max(minimum_start, instant(booking["endTime"])))
    deadline = data.get("requiredBy") or data.get("activityDueDate")
    duration = timedelta(hours=float(max((resource_hours(i) for i in data.get('items', [])), default=Decimal(1))))
    found = None
    for start in sorted(x for x in candidates if x >= minimum_start):
        end = start + duration
        if deadline and end.date() > datetime.fromisoformat(deadline).date():
            continue
        workers, equipment, local_issues = [], [], []
        for item in data.get("items", []):
            count = Decimal(str(item.get("resourceCount") or item["quantity"]))
            if item["kind"] == "Material":
                continue
            if count != count.to_integral_value():
                local_issues.append("Worker and equipment quantities must be whole numbers")
                continue
            if item["kind"] == "Workforce":
                skills = [s for s in snapshot.get("skills", []) if s["name"].casefold() == item["name"].casefold()]
                if not skills:
                    local_issues.append("Required worker skill is missing")
                    continue
                skill_id = skills[0]["id"]
                options = [w for w in snapshot.get("workers", []) if w.get("isActive", True) and not w.get("isArchived", False)
                           and any(s["workerId"] == w["id"] and s["skillId"] == skill_id and not s.get("isArchived", False) for s in snapshot.get("workerSkills", []))
                           and any(s["workerId"] == w["id"] and instant(s["startTime"]) <= start and instant(s["endTime"]) >= end for s in snapshot.get("shifts", []))
                           and not any(a["workerId"] == w["id"] and instant(a["startTime"]) < end and start < instant(a["endTime"]) for a in snapshot.get("assignments", []))
                           and w["id"] not in [a["workerId"] for a in workers]]
                if len(options) < count:
                    local_issues.append("Insufficient workers with skills and covering shifts")
                workers.extend({"workerId": w["id"], "requiredSkillId": skill_id} for w in options[:int(count)])
            elif item["kind"] == "Equipment":
                options = [e for e in snapshot.get("equipment", []) if e.get("status") == "Operational" and not e.get("isArchived", False)
                           and item["name"].casefold() in (e["name"].casefold(), e.get("category", "").casefold())
                           and not any(r["equipmentId"] == e["id"] and instant(r["startTime"]) < end and start < instant(r["endTime"]) for r in snapshot.get("reservations", []))
                           and e["id"] not in [r["equipmentId"] for r in equipment]]
                if len(options) < count:
                    local_issues.append("Insufficient operational equipment")
                equipment.extend({"equipmentId": e["id"]} for e in options[:int(count)])
        if not local_issues:
            found = (start, end, workers, equipment)
            break
    if found is None:
        issues.append("No feasible schedule within the deadline")
        found = (minimum_start, minimum_start + duration, [], [])
    total = Decimal(str(procurement.get("estimatedTotal", 0)))
    if data.get("budgetLimit") is not None and total > Decimal(str(data["budgetLimit"])):
        issues.append("Procurement exceeds budget")
    start, end, workers, equipment = found
    return ScheduleProposal(activityId=data["activityId"], startTime=start, endTime=end, workers=workers,
        equipment=equipment, validation={"isValid": not issues, "issues": issues},
        status="READY_FOR_APPROVAL" if not issues else "INVALID").model_dump(mode="json")
