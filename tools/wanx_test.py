import base64
import json
import time

import requests

KEY = "***REMOVED***"
URL = "https://dashscope.aliyuncs.com/api/v1/services/aigc/image2image/image-synthesis"

img = open(r"D:\DSH_work\StS2_MadScientist\art\ref\ip_ref_combat.png", "rb").read()
b64 = base64.b64encode(img).decode()
data_uri = "data:image/png;base64," + b64

body = {
    "model": "wanx2.1-imageedit",
    "input": {
        "function": "description_edit",
        "prompt": (
            "Change the pose: the character swings a large wrench overhead, attacking. "
            "Keep the same character design, face, colorful hair, goggles, white coat."
        ),
        "base_image_url": data_uri,
    },
    "parameters": {"n": 1},
}

r = requests.post(
    URL,
    headers={
        "Authorization": f"Bearer {KEY}",
        "X-DashScope-Async": "enable",
        "Content-Type": "application/json",
    },
    data=json.dumps(body),
    timeout=120,
)
print(r.status_code, r.text[:400])
task = r.json().get("output", {}).get("task_id")
if not task:
    raise SystemExit(1)

for _ in range(60):
    time.sleep(5)
    res = requests.get(
        f"https://dashscope.aliyuncs.com/api/v1/tasks/{task}",
        headers={"Authorization": f"Bearer {KEY}"},
        timeout=60,
    ).json()
    st = res.get("output", {}).get("task_status")
    print(st)
    if st in ("SUCCEEDED", "FAILED"):
        print(json.dumps(res, ensure_ascii=False)[:800])
        break
