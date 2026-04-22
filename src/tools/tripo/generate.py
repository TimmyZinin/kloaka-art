#!/usr/bin/env python3
"""
Batch-generate 3D models for the Space Shooter lead-magnet game via the
Tripo3D API, then drop the resulting GLB files into the Unity project's
Resources/ folders so Unity picks them up on the next build.

Docs: https://platform.tripo3d.ai/docs

Env:
    TRIPO_API_KEY   required, API key from tripo3d.ai

Usage:
    python tools/tripo/generate.py                # generate all missing models
    python tools/tripo/generate.py --only hh_drone
    python tools/tripo/generate.py --force        # regenerate even if file exists
    python tools/tripo/generate.py --list         # list slugs and where they go

If the API quota runs out (or no key is provided), see
`tools/tripo/prompts.md` for the same prompts you can paste into the Tripo
web UI manually — the output GLB files just need to land in the paths
listed in prompts.json (`target_path` field).
"""

from __future__ import annotations

import argparse
import json
import os
import sys
import time
import urllib.request
import urllib.error
from pathlib import Path

TRIPO_API_BASE = "https://api.tripo3d.ai/v2/openapi"
POLL_INTERVAL_S = 4
POLL_TIMEOUT_S = 600

ROOT = Path(__file__).resolve().parents[2]
PROMPTS = ROOT / "tools" / "tripo" / "prompts.json"


def log(msg: str) -> None:
    print(f"[tripo] {msg}", flush=True)


def load_prompts() -> dict:
    with PROMPTS.open("r", encoding="utf-8") as fp:
        return json.load(fp)


def http_post_json(url: str, body: dict, headers: dict) -> dict:
    data = json.dumps(body).encode("utf-8")
    req = urllib.request.Request(url, data=data, headers=headers, method="POST")
    with urllib.request.urlopen(req, timeout=60) as resp:
        return json.loads(resp.read().decode("utf-8"))


def http_get_json(url: str, headers: dict) -> dict:
    req = urllib.request.Request(url, headers=headers, method="GET")
    with urllib.request.urlopen(req, timeout=60) as resp:
        return json.loads(resp.read().decode("utf-8"))


def http_download(url: str, dest: Path) -> None:
    dest.parent.mkdir(parents=True, exist_ok=True)
    with urllib.request.urlopen(url, timeout=120) as resp, dest.open("wb") as fp:
        fp.write(resp.read())


def create_task(api_key: str, prompt: str, style_suffix: str, output_format: str) -> str:
    """Kick off a text-to-3D task and return the task_id."""
    full_prompt = f"{prompt}. {style_suffix}".strip()
    body = {
        "type": "text_to_model",
        "prompt": full_prompt,
        "file_format": output_format,
    }
    headers = {
        "Authorization": f"Bearer {api_key}",
        "Content-Type": "application/json",
    }
    log(f"  submitting task: {full_prompt[:90]}…")
    resp = http_post_json(f"{TRIPO_API_BASE}/task", body, headers)
    data = resp.get("data") or {}
    task_id = data.get("task_id") or data.get("id")
    if not task_id:
        raise RuntimeError(f"Tripo API response missing task_id: {resp}")
    log(f"  task_id = {task_id}")
    return task_id


def poll_task(api_key: str, task_id: str) -> str:
    """Poll until the task is done; return the downloadable model URL."""
    headers = {"Authorization": f"Bearer {api_key}"}
    started = time.time()
    last_status = None
    while True:
        if time.time() - started > POLL_TIMEOUT_S:
            raise TimeoutError(f"Task {task_id} did not finish within {POLL_TIMEOUT_S}s")
        resp = http_get_json(f"{TRIPO_API_BASE}/task/{task_id}", headers)
        data = resp.get("data") or {}
        status = data.get("status")
        if status != last_status:
            log(f"  status: {status}")
            last_status = status
        if status in ("success", "completed"):
            # Tripo's schema nests the model url under output / result
            output = data.get("output") or data.get("result") or {}
            url = (
                output.get("model")
                or output.get("pbr_model")
                or output.get("base_model")
                or output.get("glb")
            )
            if not url:
                raise RuntimeError(f"Task {task_id} finished without a model URL: {data}")
            return url
        if status in ("failed", "cancelled", "error"):
            raise RuntimeError(f"Task {task_id} failed: {data}")
        time.sleep(POLL_INTERVAL_S)


def generate_one(api_key: str, model: dict, style_suffix: str, output_format: str, force: bool) -> bool:
    slug = model["slug"]
    target = ROOT / model["target_path"]
    if target.exists() and not force:
        log(f"· {slug} already exists at {target.relative_to(ROOT)} — skipping (use --force to regenerate)")
        return False

    log(f"▶ {slug} → {target.relative_to(ROOT)}")
    task_id = create_task(api_key, model["prompt_en"], style_suffix, output_format)
    model_url = poll_task(api_key, task_id)
    log(f"  downloading {model_url}")
    http_download(model_url, target)
    size_kb = target.stat().st_size // 1024
    log(f"✔ {slug} saved ({size_kb} KB)")
    return True


def main(argv: list[str]) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--only", action="append", default=[],
                        help="Restrict generation to these slugs (repeatable)")
    parser.add_argument("--force", action="store_true",
                        help="Regenerate even if the target file already exists")
    parser.add_argument("--list", action="store_true",
                        help="List all slugs and their target paths, then exit")
    args = parser.parse_args(argv)

    spec = load_prompts()
    models = spec["models"]

    if args.list:
        for m in models:
            exists = (ROOT / m["target_path"]).exists()
            flag = "✓" if exists else "·"
            print(f"{flag} {m['slug']:<20} {m['target_path']}")
        return 0

    wanted = set(args.only)
    if wanted:
        models = [m for m in models if m["slug"] in wanted]
        if not models:
            log(f"no models match --only {args.only}")
            return 1

    api_key = os.environ.get("TRIPO_API_KEY")
    if not api_key:
        log("TRIPO_API_KEY is not set — can't call the API.")
        log("Open tools/tripo/prompts.md and generate the missing models manually in the Tripo web UI.")
        return 2

    style = spec.get("style_suffix", "")
    fmt = spec.get("output_format", "glb")

    produced = 0
    for model in models:
        try:
            if generate_one(api_key, model, style, fmt, args.force):
                produced += 1
        except urllib.error.HTTPError as e:
            log(f"✗ {model['slug']}: HTTP {e.code}: {e.reason}")
            body = e.read().decode("utf-8", errors="replace")
            log(f"  response body: {body[:400]}")
        except Exception as e:  # noqa: BLE001
            log(f"✗ {model['slug']}: {type(e).__name__}: {e}")

    log(f"done, produced {produced} file(s)")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
