"""Compare shortage quantities against authoritative, dated supplier snapshots."""
from datetime import datetime, timezone
from decimal import Decimal


def recommend(data):
    shortages = data.get("inventoryResult", {}).get("items")
    if not isinstance(shortages, list) or not isinstance(data.get("quotations"), list):
        raise ValueError("verified_shortages_and_quotations_required")
    now = datetime.now(timezone.utc)
    recommendations, unmatched, total = [], [], Decimal(0)
    for shortage in shortages:
        quantity = Decimal(str(shortage.get("shortageQuantity", shortage.get("shortage", 0))))
        if quantity <= 0:
            continue
        choices = []
        for quote in data["quotations"]:
            delivery = datetime.fromisoformat(quote["deliveryDate"].replace("Z", "+00:00"))
            expiry = datetime.fromisoformat(quote["validUntil"].replace("Z", "+00:00")) if quote.get("validUntil") else None
            if not expiry or expiry < now or delivery < now or not quote.get("isActive", False):
                continue
            if quote["materialName"].casefold() != shortage["name"].casefold() or quote.get("unit", "").casefold() != shortage["unit"].casefold():
                continue
            if Decimal(str(quote["quantity"])) < quantity or Decimal(str(quote.get("availableQuantity", 0))) < quantity:
                continue
            if data.get("requiredBy") and delivery.date() > datetime.fromisoformat(data["requiredBy"]).date():
                continue
            price = Decimal(str(quote["unitPrice"]))
            if price < 0:
                continue
            choices.append({**quote, "totalPrice": float(price * quantity)})
        choices.sort(key=lambda x: (x["totalPrice"], x["deliveryDate"], x["quotationId"]))
        if not choices:
            unmatched.append({"materialName": shortage["name"], "unit": shortage["unit"], "shortageQuantity": float(quantity)})
            continue
        total += Decimal(str(choices[0]["totalPrice"]))
        recommendations.append({"materialName": shortage["name"], "unit": shortage["unit"], "shortageQuantity": float(quantity), "recommendedSupplier": choices[0], "alternatives": choices[1:]})
    within_budget = data.get("budgetLimit") is None or total <= Decimal(str(data["budgetLimit"]))
    return {"recommendations": recommendations, "unmatched": unmatched, "estimatedTotal": float(total),
            "overallStatus": "READY" if not unmatched and within_budget else "INVALID", "approvalRequired": True, "sideEffects": []}
