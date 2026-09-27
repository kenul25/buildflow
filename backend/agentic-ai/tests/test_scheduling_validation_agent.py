import unittest
from datetime import datetime, timedelta, timezone

from agents.scheduling_validation_agent import (
    SchedulingValidationAgent,
    propose_and_validate_schedule,
)


class SchedulingValidationAgentTests(unittest.TestCase):

    def future_date(self, days=2):
        return (
            datetime.now(timezone.utc) + timedelta(days=days)
        ).isoformat()

    def valid_delivery(self):
        return {
            "deliveryId": 1,
            "purchaseOrderId": 100,
            "status": "Pending",
            "deliveryDate": self.future_date(),
            "quantity": 50,
        }

    def valid_purchase_order(self):
        return {
            "purchaseOrderId": 100,
            "quantity": 100,
        }

    def test_valid_delivery(self):
        result = propose_and_validate_schedule(
            self.valid_delivery(),
            self.valid_purchase_order(),
        )

        self.assertEqual(
            result["validationStatus"],
            "Valid",
        )
        self.assertEqual(
            result["message"],
            "Delivery is valid",
        )

    def test_completed_delivery_is_rejected(self):
        delivery = self.valid_delivery()
        delivery["status"] = "Completed"

        result = propose_and_validate_schedule(
            delivery,
            self.valid_purchase_order(),
        )

        self.assertEqual(
            result["validationStatus"],
            "Invalid",
        )
        self.assertEqual(
            result["message"],
            "Delivery is already completed",
        )

    def test_past_delivery_date_is_rejected(self):
        delivery = self.valid_delivery()
        delivery["deliveryDate"] = (
            datetime.now(timezone.utc) - timedelta(days=1)
        ).isoformat()

        result = propose_and_validate_schedule(
            delivery,
            self.valid_purchase_order(),
        )

        self.assertEqual(
            result["validationStatus"],
            "Invalid",
        )
        self.assertEqual(
            result["message"],
            "Delivery date has passed",
        )

    def test_invalid_quantity_is_rejected(self):
        delivery = self.valid_delivery()
        delivery["quantity"] = 0

        result = propose_and_validate_schedule(
            delivery,
            self.valid_purchase_order(),
        )

        self.assertEqual(
            result["validationStatus"],
            "Invalid",
        )
        self.assertEqual(
            result["message"],
            "Invalid delivery quantity",
        )

    def test_missing_purchase_order_is_rejected(self):
        result = propose_and_validate_schedule(
            self.valid_delivery(),
            None,
        )

        self.assertEqual(
            result["validationStatus"],
            "Invalid",
        )
        self.assertEqual(
            result["message"],
            "Purchase order not found",
        )

    def test_quantity_exceeds_purchase_order(self):
        delivery = self.valid_delivery()
        delivery["quantity"] = 150

        result = propose_and_validate_schedule(
            delivery,
            self.valid_purchase_order(),
        )

        self.assertEqual(
            result["validationStatus"],
            "Invalid",
        )
        self.assertEqual(
            result["message"],
            "Delivery quantity exceeds purchase order quantity",
        )

    def test_purchase_order_id_mismatch(self):
        purchase_order = self.valid_purchase_order()
        purchase_order["purchaseOrderId"] = 999

        result = propose_and_validate_schedule(
            self.valid_delivery(),
            purchase_order,
        )

        self.assertEqual(
            result["validationStatus"],
            "Invalid",
        )
        self.assertEqual(
            result["message"],
            "Purchase order not found",
        )

    def test_task_wrapper(self):
        result = SchedulingValidationAgent().execute({
            "task_id": "schedule-task-123",
            "agent": "SchedulingValidationAgent",
            "action": "ProposeAndValidateSchedule",
            "input": {
                "workflowId": "workflow-123",
                "delivery": self.valid_delivery(),
                "purchaseOrder": self.valid_purchase_order(),
            },
        })

        self.assertEqual(
            result["status"],
            "Completed",
        )
        self.assertEqual(
            result["task_id"],
            "schedule-task-123",
        )
        self.assertEqual(
            result["workflowId"],
            "workflow-123",
        )
        self.assertEqual(
            result["agent"],
            "SchedulingValidationAgent",
        )
        self.assertEqual(
            result["validationStatus"],
            "Valid",
        )
        self.assertTrue(
            result["approvalRequired"]
        )
        self.assertEqual(
            result["sideEffects"],
            [],
        )

    def test_unsupported_action(self):
        result = SchedulingValidationAgent().execute({
            "task_id": "schedule-task-123",
            "agent": "SchedulingValidationAgent",
            "action": "CreateSchedule",
            "input": {},
        })

        self.assertEqual(
            result["status"],
            "Failed",
        )
        self.assertEqual(
            result["error"],
            "unsupported_scheduling_action",
        )
        self.assertEqual(
            result["sideEffects"],
            [],
        )

    def test_unsupported_agent(self):
        result = SchedulingValidationAgent().execute({
            "task_id": "schedule-task-123",
            "agent": "InventoryAgent",
            "action": "ProposeAndValidateSchedule",
            "input": {},
        })

        self.assertEqual(
            result["status"],
            "Failed",
        )
        self.assertEqual(
            result["error"],
            "unsupported_scheduling_agent",
        )
        self.assertEqual(
            result["sideEffects"],
            [],
        )


if __name__ == "__main__":
    unittest.main()
