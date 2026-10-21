import json
import time
from pathlib import Path

import requests

KEY = open(Path.home() / ".dsh" / "secrets" / "dashscope_key", encoding="ascii").read().strip()
BASE = "https://ws-q7s1mlhcjf7yjjnm.cn-beijing.maas.aliyuncs.com/compatible-mode/v1"
API = "https://dashscope.aliyuncs.com/api/v1"
IMG = Path(r"D:\DSH_work\StS2_MadScientist\art\ref\wanx_ref.png")
PROMPT = "the character swings a large wrench overhead, attack pose, keep the same character design, face and outfit"


def upload(path: Path) -> str:
    policy = requests.get(
        f"{API}/uploads?action=getPolicy&model=wanx2.1-imageedit",
        headers={"Authorization": f"Bearer {KEY}"},
        timeout=60,
    ).json()["data"]
    key = f"{policy['upload_dir']}/{path.name}"
    with path.open("rb") as fh:
        r = requests.post(
            policy["upload_host"],
            data={
                "OSSAccessKeyId": policy["oss_access_key_id"],
                "policy": policy["policy"],
                "Signature": policy["signature"],
                "key": key,
                "success_action_status": "200",
                "x-oss-object-acl": "private",
                "x-oss-forbid-overwrite": "true",
            },
            files={"file": (path.name, fh, "image/png")},
            timeout=120,
        )
    print("upload", r.status_code)
    return "oss://" + key


url = upload(IMG)
print(url)

time.sleep(60)
body = {
    "model": "qwen-image-2.0-pro",
    "prompt": PROMPT,
    "size": "1024x1024",
    "image_url": url,
}
r = requests.post(
    f"{BASE}/images/generations",
    headers={"Authorization": f"Bearer {KEY}", "Content-Type": "application/json"},
    data=json.dumps(body),
    timeout=240,
)
print(r.status_code, r.text[:400])
