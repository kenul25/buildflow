import unittest
from datetime import datetime, timezone
from uuid import uuid4

from agents.deterministic_control import AgentTask
from agents.scheduling_validation_agent import SchedulingValidationAgent


class SchedulingValidationAgentTests(unittest.TestCase):
    def setUp(self):
        self.agent = SchedulingValidationAgent()

        self.workflow_id = uuid4()
        self.task_id = uuid4()
        self.activity_id = uuid4()
        self.worker_id = uuid4()
        self.equipment_id = uuid4()

    def make_task(self, **overrides):
        data = {
            "workflowId": self.workflow_id,
            "taskId": self.task_id,
            "activityId": self.activity_id,
            "activityName": "Concrete Work",
            "requiredSkills": ["Masonry"],
            "requiredWorkerCount": 1,
            "workers": [
                {
                    "workerId": self.worker_id,
                    "name": "Worker 01",
                    "skills": ["Masonry", "Concrete"],
                    "available": True,
                    "shiftStart": "2026-09-28T08:00:00Z",
                    "shiftEnd": "2026-09-28T17:00:00Z",
                }
            ],
            "equipment": [
                {
                    "equipmentId": self.equipment_id,
                    "code": "EQ001",
                    "name": "Concrete Mixer",
                    "status": "Available",
                    "available": True,
                }
            ],
            "proposedStart": "2026-09-28T09:00:00Z",
            "proposedEnd": "2026-09-28T12:00:00Z",
            "dependencyReady": True,
            "supplierDeliveryDate": "2026-09-28",
            "constructionDeadline": "2026-09-30",
            "procurementCost": 100000,
            "budgetLimit": 200000,
        }
        data.update(overrides)

        return AgentTask(
            task_id=str(self.task_id),
            agent="SchedulingValidationAgent",
            action="ProposeAndValidateSchedule",
            depends_on=[],
            input=data,
        )

    def test_valid_schedule_is_accepted(self):
        result = self.agent.execute(self.make_task())

        self.assertEqual(result["status"], "Completed")
        self.assertTrue(result["accepted"])
        self.assertTrue(result["approvalRequired"])
        self.assertEqual(result["agent"], "SchedulingValidationAgent")
        self.assertEqual(result["proposal"]["activityId"], self.activity_id)

    def test_missing_workers_fails_validation(self):
        result = self.agent.execute(
            self.make_task(workers=[])
        )

        self.assertEqual(result["status"], "Completed")
        self.assertFalse(result["accepted"])
        self.assertIn(
            "insufficient_available_workers",
            result["validationErrors"],
        )

    def test_missing_required_skill_fails_validation(self):
        result = self.agent.execute(
            self.make_task(
                workers=[{
                    "workerId": self.worker_id,
                    "name": "Worker 01",
                    "skills": ["Painting"],
                    "available": True,
                    "shiftStart": "2026-09-28T08:00:00Z",
                    "shiftEnd": "2026-09-28T17:00:00Z",
                }]
            )
        )

        self.assertEqual(result["status"], "Completed")
        self.assertFalse(result["accepted"])
        self.assertIn(
            "required_worker_skills_not_available",
            result["validationErrors"],
        )

    def test_dependency_failure_is_detected(self):
        result = self.agent.execute(
            self.make_task(dependencyReady=False)
        )

        self.assertEqual(result["status"], "Completed")
        self.assertFalse(result["accepted"])
        self.assertIn(
            "dependencies_not_ready",
            result["validationErrors"],
        )

    def test_budget_exceeded_is_detected(self):
        result = self.agent.execute(
            self.make_task(
                procurementCost=300000,
                budgetLimit=200000,
            )
        )

        self.assertEqual(result["status"], "Completed")
        self.assertFalse(result["accepted"])
        self.assertIn(
            "procurement_cost_exceeds_budget",
            result["validationErrors"],
        )

    def test_unsupported_task_fails_safely(self):
        task = {
            **self.make_task().__dict__,
            "action": "ReserveEquipment"
        }

        result = self.agent.execute(task)

        self.assertEqual(result["status"], "Failed")
        self.assertFalse(result["accepted"])
        self.assertEqual(result["agent"], "SchedulingValidationAgent")
        self.assertEqual(result["sideEffects"], [])


if __name__ == "__main__":
    unittest.main()

