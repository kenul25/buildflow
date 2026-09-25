import unittest

from agents.inventory_agent import InventoryAgent, analyze_availability
from tools.inventory_tools import InventoryAnalysisError


class InventoryAgentTests(unittest.TestCase):
    def test_reports_exact_shortage_without_side_effects(self):
        report = analyze_availability(
            [{"name": "Cement", "quantity": 150, "unit": "bags"}],
            [{"name": "Cement", "currentStock": 120, "reservedStock": 20, "unit": "bags"}],
        )
        item = report["items"][0]
        self.assertEqual(item["required"], 150.0)
        self.assertEqual(item["availableStock"], 100.0)
        self.assertEqual(item["shortage"], 50.0)
        self.assertFalse(report["isFullyAvailable"])
        self.assertEqual(report["sideEffects"], [])

    def test_aggregates_duplicate_requirements_and_warehouses(self):
        report = analyze_availability(
            [
                {"name": "Steel", "quantity": 40, "unit": "kg"},
                {"name": "steel", "quantity": 10, "unit": "KG"},
            ],
            [
                {"name": "Steel", "currentStock": 30, "reservedStock": 5, "unit": "kg"},
                {"name": "Steel", "currentStock": 30, "reservedStock": 5, "unit": "kg"},
            ],
        )
        item = report["items"][0]
        self.assertEqual(item["required"], 50.0)
        self.assertEqual(item["availableStock"], 50.0)
        self.assertTrue(item["isAvailable"])

    def test_missing_snapshot_fails_closed(self):
        result = InventoryAgent().execute({"agent": "InventoryAgent", "action": "AnalyzeAvailability", "input": {"items": []}})
        self.assertEqual(result["status"], "Failed")
        self.assertEqual(result["error"], "inventory_snapshot_required")
        self.assertEqual(result["sideEffects"], [])

    def test_task_wrapper_uses_snapshot_and_preserves_task_identity(self):
        result = InventoryAgent().execute({
            "task_id": "task-123", "agent": "InventoryAgent", "action": "AnalyzeAvailability",
            "input": {
                "workflowId": "workflow-123",
                "items": [{"name": "Cement", "quantity": 150, "unit": "bags"}],
                "inventorySnapshot": [{"name": "Cement", "currentStock": 100, "reservedStock": 0, "unit": "bags"}],
            },
        })
        self.assertEqual(result["task_id"], "task-123")
        self.assertEqual(result["workflowId"], "workflow-123")
        self.assertEqual(result["materials"][0]["shortageQuantity"], 50.0)
        self.assertTrue(result["approvalRequired"])

    def test_unsupported_task_fails_with_validated_safe_envelope(self):
        result = InventoryAgent().execute({"task_id": "task-123", "agent": "ProcurementAgent", "action": "RecommendProcurement", "input": {}})
        self.assertEqual(result["schemaVersion"], "1.0")
        self.assertEqual(result["status"], "Failed")
        self.assertEqual(result["overallStatus"], "FAILED")
        self.assertEqual(result["sideEffects"], [])

    def test_invalid_reserved_stock_is_rejected(self):
        with self.assertRaises(InventoryAnalysisError):
            analyze_availability(
                [{"name": "Cement", "quantity": 1, "unit": "bag"}],
                [{"name": "Cement", "currentStock": 10, "reservedStock": 11, "unit": "bag"}],
            )


if __name__ == "__main__":
    unittest.main()