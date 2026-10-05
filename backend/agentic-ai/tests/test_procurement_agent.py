import unittest
from datetime import datetime, timezone
from decimal import Decimal

from agents.procurement_agent import (
    ProcurementAgent,
    ProcurementInput,
    Quotation,
    recommend_procurement,
)


class ProcurementAgentTests(unittest.TestCase):

    def quotation(
        self,
        quotation_id,
        supplier_id,
        supplier_name,
        material,
        quantity,
        unit_price,
        delivery_date,
    ):
        return Quotation(
            quotationId=quotation_id,
            supplierId=supplier_id,
            supplierName=supplier_name,
            materialName=material,
            quantity=Decimal(str(quantity)),
            unitPrice=Decimal(str(unit_price)),
            deliveryDate=datetime.fromisoformat(
                delivery_date
            ).replace(tzinfo=timezone.utc),
            totalPrice=Decimal(str(quantity)) * Decimal(str(unit_price)),
        )

    def test_selects_lowest_cost_for_requested_quantity(self):
        task_input = ProcurementInput(
            workflowId="workflow-123",
            materialName="Cement",
            requiredQuantity=Decimal("50"),
            quotations=[
                self.quotation(
                    1,
                    1,
                    "ABC Construction Suppliers",
                    "Cement",
                    50,
                    2500,
                    "2026-09-30T00:00:00",
                ),
                self.quotation(
                    2,
                    3,
                    "Lanka Building Materials",
                    "Cement",
                    60,
                    2200,
                    "2026-09-28T00:00:00",
                ),
            ],
        )

        result = recommend_procurement(task_input)

        self.assertEqual(result["status"], "QuotationAvailable")
        self.assertEqual(result["quotationCount"], 2)
        self.assertEqual(
            result["recommendedQuotation"]["quotationId"],
            2,
        )
        self.assertEqual(
            result["recommendedQuotation"]["supplierName"],
            "Lanka Building Materials",
        )
        self.assertEqual(result["recommendedQuotation"]["totalPrice"], 110000)
        self.assertEqual(result["sideEffects"], [])
        self.assertTrue(result["approvalRequired"])

    def test_ignores_insufficient_quantity(self):
        task_input = ProcurementInput(
            workflowId="workflow-123",
            materialName="Cement",
            requiredQuantity=Decimal("55"),
            quotations=[
                self.quotation(
                    1,
                    1,
                    "ABC Construction Suppliers",
                    "Cement",
                    50,
                    2500,
                    "2026-09-30T00:00:00",
                ),
                self.quotation(
                    2,
                    3,
                    "Lanka Building Materials",
                    "Cement",
                    60,
                    2200,
                    "2026-09-28T00:00:00",
                ),
            ],
        )

        result = recommend_procurement(task_input)

        self.assertEqual(result["quotationCount"], 1)
        self.assertEqual(
            result["recommendedQuotation"]["quotationId"],
            2,
        )
        self.assertEqual(
            result["recommendedQuotation"]["supplierName"],
            "Lanka Building Materials",
        )
        self.assertEqual(result["sideEffects"], [])

    def test_material_name_case_insensitive(self):
        task_input = ProcurementInput(
            workflowId="workflow-123",
            materialName="cement",
            requiredQuantity=Decimal("50"),
            quotations=[
                self.quotation(
                    1,
                    1,
                    "ABC Construction Suppliers",
                    "CEMENT",
                    50,
                    2500,
                    "2026-09-30T00:00:00",
                ),
            ],
        )

        result = recommend_procurement(task_input)

        self.assertEqual(result["status"], "QuotationAvailable")
        self.assertEqual(result["quotationCount"], 1)
        self.assertEqual(
            result["recommendedQuotation"]["quotationId"],
            1,
        )
        self.assertEqual(result["sideEffects"], [])

    def test_no_quotation_available(self):
        task_input = ProcurementInput(
            workflowId="workflow-123",
            materialName="Steel",
            requiredQuantity=Decimal("100"),
            quotations=[],
        )

        result = recommend_procurement(task_input)

        self.assertEqual(
            result["status"],
            "NoQuotationAvailable",
        )
        self.assertEqual(result["quotationCount"], 0)
        self.assertIsNone(result["recommendedQuotation"])
        self.assertEqual(result["alternatives"], [])
        self.assertTrue(result["approvalRequired"])
        self.assertEqual(result["sideEffects"], [])

    def test_no_eligible_quotation_when_quantity_is_insufficient(self):
        task_input = ProcurementInput(
            workflowId="workflow-123",
            materialName="Cement",
            requiredQuantity=Decimal("100"),
            quotations=[
                self.quotation(
                    1,
                    1,
                    "ABC Construction Suppliers",
                    "Cement",
                    50,
                    2500,
                    "2026-09-30T00:00:00",
                ),
                self.quotation(
                    2,
                    3,
                    "Lanka Building Materials",
                    "Cement",
                    60,
                    2200,
                    "2026-09-28T00:00:00",
                ),
            ],
        )

        result = recommend_procurement(task_input)

        self.assertEqual(
            result["status"],
            "NoQuotationAvailable",
        )
        self.assertEqual(result["quotationCount"], 0)
        self.assertIsNone(result["recommendedQuotation"])
        self.assertEqual(result["alternatives"], [])
        self.assertTrue(result["approvalRequired"])
        self.assertEqual(result["sideEffects"], [])

    def test_task_wrapper(self):
        result = ProcurementAgent().execute(
            {
                "task_id": "task-procurement-123",
                "agent": "ProcurementAgent",
                "action": "RecommendProcurement",
                "input": {
                    "workflowId": "workflow-123",
                    "materialName": "Cement",
                    "requiredQuantity": 50,
                    "quotations": [
                        {
                            "quotationId": 1,
                            "supplierId": 1,
                            "supplierName": "ABC Construction Suppliers",
                            "materialName": "Cement",
                            "quantity": 50,
                            "unitPrice": 2500,
                            "deliveryDate": "2026-09-30T00:00:00Z",
                            "totalPrice": 125000,
                        }
                    ],
                },
            }
        )

        self.assertEqual(result["status"], "Completed")
        self.assertEqual(
            result["task_id"],
            "task-procurement-123",
        )
        self.assertEqual(
            result["workflowId"],
            "workflow-123",
        )
        self.assertEqual(
            result["agent"],
            "ProcurementAgent",
        )
        self.assertEqual(
            result["output"]["quotationCount"],
            1,
        )
        self.assertEqual(
            result["output"]["status"],
            "QuotationAvailable",
        )
        self.assertEqual(
            result["output"]["recommendedQuotation"]["quotationId"],
            1,
        )
        self.assertEqual(result["sideEffects"], [])
        self.assertTrue(result["approvalRequired"])

    def test_unsupported_action(self):
        result = ProcurementAgent().execute(
            {
                "task_id": "task-123",
                "agent": "ProcurementAgent",
                "action": "CreatePurchaseOrder",
                "input": {},
            }
        )

        self.assertEqual(result["status"], "Failed")
        self.assertEqual(
            result["error"],
            "unsupported_procurement_action",
        )
        self.assertEqual(result["sideEffects"], [])
        self.assertTrue(result["approvalRequired"])

    def test_unsupported_agent(self):
        result = ProcurementAgent().execute(
            {
                "task_id": "task-123",
                "agent": "InventoryAgent",
                "action": "RecommendProcurement",
                "input": {},
            }
        )

        self.assertEqual(result["status"], "Failed")
        self.assertEqual(
            result["error"],
            "unsupported_procurement_agent",
        )
        self.assertEqual(result["sideEffects"], [])
        self.assertTrue(result["approvalRequired"])

    def test_invalid_quantity_is_rejected(self):
        with self.assertRaises(Exception):
            ProcurementInput(
                workflowId="workflow-123",
                materialName="Cement",
                requiredQuantity=0,
                quotations=[],
            )


if __name__ == "__main__":
    unittest.main()
