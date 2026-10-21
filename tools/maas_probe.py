import base64
import json

import requests

KEY = "***REMOVED***"
BASE = "https://ws-q7s1mlhcjf7yjjnm.cn-beijing.maas.aliyuncs.com/compatible-mode/v1"
IMG = r"D:\DSH_work\StS2_MadScientist\art\ref\wanx_ref.png"
PROMPT = "the character swings a large wrench overhead, attack pose, keep the same character design, face and outfit"

b64 = base64.b64encode(open(IMG, "rb").read()).decode()
data_uri = "data:image/png;base64," + b64

variants = {
    "image_data_uri": {"image": data_uri},
    "image_url": {"image_url": data_uri},
    "images_list": {"images": [data_uri]},
    "input_image": {"input_image": data_uri},
    "ref_image": {"ref_image": data_uri},
}

for name, extra in variants.items():
    body = {"model": "qwen-image-2.0-pro", "prompt": PROMPT, "size": "1024x1024", **extra}
    try:
        r = requests.post(
            f"{BASE}/images/generations",
            headers={"Authorization": f"Bearer {KEY}", "Content-Type": "application/json"},
            data=json.dumps(body),
            timeout=180,
        )
        print(name, r.status_code, r.text[:200])
    except Exception as exc:  # noqa: BLE001
        print(name, "ERR", exc)
