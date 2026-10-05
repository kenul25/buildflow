import importlib.util
from pathlib import Path
import unittest
from unittest.mock import patch

spec = importlib.util.spec_from_file_location("deploy_render", Path(__file__).parents[1] / "deploy_render.py")
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)
SHA = "a" * 40


class RenderDeployTests(unittest.TestCase):
    def test_waits_for_the_requested_commit_to_be_live(self):
        with patch.object(module, "request_json", side_effect=[
            {"id": "dep-test"}, {"status": "build_in_progress"},
            {"status": "live", "commit": {"id": SHA}},
        ]) as request, patch.object(module.time, "sleep"):
            self.assertEqual(module.deploy("srv-test", SHA, "test-token"), "dep-test")
            self.assertEqual(request.call_args_list[0].args[2], {"commitId": SHA})
            self.assertEqual(request.call_count, 3)

    def test_failed_deploy_stops_the_pipeline(self):
        for status in ["build_failed", "update_failed", "pre_deploy_failed", "canceled"]:
            with self.subTest(status=status), patch.object(module, "request_json", side_effect=[
                {"id": "dep-test"}, {"status": status},
            ]):
                with self.assertRaisesRegex(RuntimeError, "failed"):
                    module.deploy("srv-test", SHA, "test-token")

    def test_live_wrong_commit_is_rejected(self):
        with patch.object(module, "request_json", side_effect=[
            {"id": "dep-test"}, {"status": "live", "commit": {"id": "b" * 40}},
        ]):
            with self.assertRaisesRegex(RuntimeError, "different commit"):
                module.deploy("srv-test", SHA, "test-token")

    def test_wait_is_bounded(self):
        with patch.object(module, "request_json", return_value={"id": "dep-test"}), patch.object(module.time, "monotonic", side_effect=[0, 1801]):
            with self.assertRaisesRegex(RuntimeError, "Timed out"):
                module.deploy("srv-test", SHA, "test-token")

    def test_invalid_identifiers_do_not_make_requests(self):
        with patch.object(module, "request_json") as request:
            for service, sha in [("wrong-id", SHA), ("srv-test", "main")]:
                with self.assertRaises(ValueError):
                    module.deploy(service, sha, "test-token")
            request.assert_not_called()
