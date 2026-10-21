"""DashScope Wanx image edit: upload reference to OSS, edit, poll, download."""
from __future__ import annotations

import base64
import json
import os
import time
from pathlib import Path

import requests

ROOT = Path(__file__).resolve().parent.parent
SECRET = Path(os.path.expanduser("~")) / ".dsh" / "secrets" / "dashscope_key"
BASE = "https://dashscope.aliyuncs.com/api/v1"


def get_key() -> str:
    if SECRET.exists():
        return SECRET.read_text(encoding="ascii").strip()
    return os.environ["DASHSCOPE_API_KEY"]


def upload(path: Path, key: str) -> str:
    policy = requests.get(
        f"{BASE}/uploads?action=getPolicy&model=wanx2.1-imageedit",
        headers={"Authorization": f"Bearer {key}"},
        timeout=60,
    ).json()["data"]

    object_key = f"{policy['upload_dir']}/{path.name}"
    with path.open("rb") as fh:
        r = requests.post(
            policy["upload_host"],
            data={
                "OSSAccessKeyId": policy["oss_access_key_id"],
                "policy": policy["policy"],
                "Signature": policy["signature"],
                "key": object_key,
                "success_action_status": "200",
                "x-oss-object-acl": "private",
                "x-oss-forbid-overwrite": "true",
            },
            files={"file": (path.name, fh, "image/png")},
            timeout=120,
        )
    r.raise_for_status()
    return "oss://" + object_key


def main() -> None:
    key = get_key()
    ref = ROOT / "art" / "ref" / "wanx_ref.png"
    url = upload(ref, key)
    print("uploaded", url)

    body = {
        "model": "wanx2.1-imageedit",
        "input": {
            "function": "description_edit",
            "prompt": (
                "The character swings a large wrench overhead in an attack pose. "
                "Keep the same character design, face, colorful hair, goggles, white coat."
            ),
            "base_image_url": url,
        },
        "parameters": {"n": 1},
    }
    r = requests.post(
        f"{BASE}/services/aigc/image2image/image-synthesis",
        headers={
            "Authorization": f"Bearer {key}",
            "X-DashScope-Async": "enable",
            "Content-Type": "application/json",
        },
        data=json.dumps(body),
        timeout=120,
    )
    print(r.status_code, r.text[:300])
    task = r.json()["output"]["task_id"]

    for _ in range(60):
        time.sleep(5)
        res = requests.get(
            f"{BASE}/tasks/{task}", headers={"Authorization": f"Bearer {key}"}, timeout=60
        ).json()
        st = res["output"]["task_status"]
        print(st)
        if st in ("SUCCEEDED", "FAILED"):
            print(json.dumps(res, ensure_ascii=False)[:600])
            if st == "SUCCEEDED":
                img_url = res["output"]["results"][0]["url"]
                data = requests.get(img_url, timeout=120).content
                out = ROOT / "art" / "wanx_out"
                out.mkdir(exist_ok=True)
                (out / "test.png").write_bytes(data)
                print("saved", out / "test.png")
            break


if __name__ == "__main__":
    main()
