import copy
import unittest
from datetime import datetime, timedelta, timezone
from agents.resource_procurement import recommend
from agents.resource_scheduler import propose
import os
from unittest.mock import patch
from uuid import uuid4
from fastapi.testclient import TestClient
from main import app


class ResourcePlanningTests(unittest.TestCase):
    def data(self):
        now = datetime.now(timezone.utc)
        return {
            'activityId': 'activity', 'requiredBy': (now + timedelta(days=4)).date().isoformat(),
            'budgetLimit': 100,
            'items': [{'kind': 'Workforce', 'name': 'Masonry', 'quantity': 1}, {'kind': 'Equipment', 'name': 'Crane', 'quantity': 1}],
            'inventoryResult': {'items': [{'name': 'Cement', 'unit': 'kg', 'shortageQuantity': 2.5}]},
            'quotations': [{'quotationId': 1, 'supplierId': 1, 'materialName': 'Cement', 'unit': 'kg', 'quantity': 100, 'unitPrice': 3, 'isActive': True, 'availableQuantity': 100, 'deliveryDate': (now + timedelta(days=1)).isoformat(), 'validUntil': (now + timedelta(days=3)).isoformat()},
                {'quotationId': 2, 'supplierId': 2, 'materialName': 'Cement', 'unit': 'kg', 'quantity': 3, 'unitPrice': 4, 'isActive': True, 'availableQuantity': 3, 'deliveryDate': (now + timedelta(days=1)).isoformat(), 'validUntil': (now + timedelta(days=3)).isoformat()}],
            'schedulingSnapshot': {'now': now.isoformat(), 'workers': [{'id': 'worker', 'isActive': True}], 'skills': [{'id': 'skill', 'name': 'Masonry'}], 'workerSkills': [{'workerId': 'worker', 'skillId': 'skill'}],
                'shifts': [{'workerId': 'worker', 'startTime': (now + timedelta(days=1)).isoformat(), 'endTime': (now + timedelta(days=2)).isoformat()}],
                'equipment': [{'id': 'crane', 'name': 'Crane', 'status': 'Operational'}], 'assignments': [], 'reservations': []}}

    def test_requested_quantity_price_and_read_only(self):
        data = self.data(); before = copy.deepcopy(data)
        result = recommend(data)
        self.assertEqual(result['recommendations'][0]['recommendedSupplier']['quotationId'], 1)
        self.assertEqual(result['estimatedTotal'], 7.5)
        self.assertEqual(result['sideEffects'], [])
        self.assertEqual(data, before)

    def test_expired_quote_and_unit_mismatch_rejected(self):
        data = self.data()
        data['quotations'][0]['validUntil'] = (datetime.now(timezone.utc) - timedelta(days=1)).isoformat()
        data['quotations'][1]['unit'] = 'bags'
        self.assertEqual(recommend(data)['overallStatus'], 'INVALID')

    def test_supplier_capacity_and_budget_enforced(self):
        data = self.data(); data['budgetLimit'] = 1
        self.assertEqual(recommend(data)['overallStatus'], 'INVALID')
        for quote in data['quotations']: quote['availableQuantity'] = 1
        self.assertEqual(len(recommend(data)['unmatched']), 1)

    def test_schedules_after_delivery_with_qualified_resources(self):
        data = self.data(); data['procurementResult'] = recommend(data)
        result = propose(data)
        self.assertEqual(result['status'], 'READY_FOR_APPROVAL')
        self.assertEqual(result['workers'], [{'workerId': 'worker', 'requiredSkillId': 'skill'}])
        self.assertGreaterEqual(result['startTime'], data['quotations'][0]['deliveryDate'])

    def test_overlap_waits_until_booking_finishes(self):
        data = self.data(); data['procurementResult'] = recommend(data)
        shift = data['schedulingSnapshot']['shifts'][0]
        finish = (datetime.fromisoformat(shift['startTime']) + timedelta(hours=3)).isoformat()
        data['schedulingSnapshot']['assignments'] = [{'workerId': 'worker', 'startTime': shift['startTime'], 'endTime': finish}]
        result = propose(data)
        self.assertEqual(result['status'], 'READY_FOR_APPROVAL')
        self.assertGreaterEqual(result['startTime'], finish)

    def test_missing_shift_or_skill_fails_closed(self):
        for key in ['shifts', 'workerSkills']:
            data = self.data(); data['procurementResult'] = recommend(data); data['schedulingSnapshot'][key] = []
            self.assertEqual(propose(data)['status'], 'INVALID')

    def test_deadline_fractional_worker_and_uncovered_material_rejected(self):
        data = self.data(); data['requiredBy'] = datetime.now(timezone.utc).date().isoformat(); data['procurementResult'] = recommend(data)
        self.assertEqual(propose(data)['status'], 'INVALID')

    def test_private_agent_http_contract_runs_all_three_agents(self):
        data = self.data()
        data['workflowId'] = str(uuid4())
        data['items'].append({'kind': 'Material', 'name': 'Cement', 'quantity': 2.5, 'unit': 'kg'})
        data['inventorySnapshot'] = [{'materialId': str(uuid4()), 'name': 'Cement', 'unit': 'kg', 'currentStock': 1, 'reservedStock': 0}]
        with patch.dict(os.environ, {'BUILDFLOW_INTERNAL_KEY': 'test-private-key'}), TestClient(app) as client:
            self.assertEqual(client.post('/internal/agents', json={'agent': 'InventoryAgent'}).status_code, 401)
            for agent, action in [('InventoryAgent', 'AnalyzeAvailability'), ('ProcurementAgent', 'RecommendProcurement'), ('SchedulingValidationAgent', 'ProposeAndValidateSchedule')]:
                task_id = str(uuid4())
                response = client.post('/internal/agents', headers={'X-BuildFlow-Key': 'test-private-key'}, json={'task_id': task_id, 'agent': agent, 'action': action, 'input': data})
                self.assertEqual(response.status_code, 200)
                result = response.json()
                self.assertEqual(result['task_id'], task_id)
                self.assertEqual(result['status'], 'Completed', result)
                if agent == 'InventoryAgent': data['inventoryResult'] = result['output']
                elif agent == 'ProcurementAgent': data['procurementResult'] = result['output']
                else: self.assertEqual(result['output']['status'], 'READY_FOR_APPROVAL')
        data = self.data(); data['items'][0]['quantity'] = 0.5; data['procurementResult'] = recommend(data)
        self.assertEqual(propose(data)['status'], 'INVALID')
        data = self.data(); data['procurementResult'] = {'recommendations': []}
        self.assertEqual(propose(data)['status'], 'INVALID')
