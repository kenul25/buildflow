"""Deploy a tested Git SHA to a Git-backed Render service and wait for it."""
import argparse
import json
import os
import re
import time
from urllib.error import HTTPError, URLError
from urllib.request import Request, urlopen


def request_json(path, token, payload=None):
    request = Request(
        f"https://api.render.com/v1/{path}",
        data=json.dumps(payload).encode() if payload is not None else None,
        headers={"Authorization": f"Bearer {token}", "Content-Type": "application/json"},
        method="POST" if payload is not None else "GET",
    )
    try:
        with urlopen(request, timeout=60) as response:
            return json.load(response)
    except HTTPError as error:
        # Response bodies might contain configuration; don't print them in CI.
        raise RuntimeError(f"Render API returned HTTP {error.code}") from None
    except URLError:
        raise RuntimeError("Could not connect to the Render API") from None


def deploy(service, sha, token, timeout=1800):
    if not re.fullmatch(r"srv-[A-Za-z0-9]+", service):
        raise ValueError("Use a Render service ID beginning with srv-")
    if not re.fullmatch(r"[0-9a-f]{40}", sha):
        raise ValueError("A full Git commit SHA is required")
    result = request_json(f"services/{service}/deploys", token, {"commitId": sha})
    deploy_id = result["id"]
    deadline = time.monotonic() + timeout
    previous = None
    while time.monotonic() < deadline:
        result = request_json(f"services/{service}/deploys/{deploy_id}", token)
        status = result["status"]
        if status != previous:
            print(f"Render {service}: {status}", flush=True)
            previous = status
        if status == "live":
            if result.get("commit", {}).get("id") != sha:
                raise RuntimeError("Render deployed a different commit than the CI-tested commit")
            return deploy_id
        if status in {"build_failed", "update_failed", "pre_deploy_failed", "canceled", "deactivated"}:
            raise RuntimeError(f"Render deployment failed: {status}")
        time.sleep(20)
    raise RuntimeError("Timed out waiting for Render; inspect the service deployment logs")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--service-env", required=True)
    parser.add_argument("--sha", required=True)
    args = parser.parse_args()
    try:
        service = os.environ[args.service_env]
        token = os.environ["RENDER_API_KEY"]
        if not token:
            raise ValueError("RENDER_API_KEY is required")
        deploy(service, args.sha, token)
    except (KeyError, ValueError, RuntimeError) as error:
        raise SystemExit(str(error)) from None
