import base64
import json
import re
from pathlib import Path
from urllib.parse import urlsplit, urlunsplit

import requests

KEY = open(Path.home() / ".dsh" / "secrets" / "dashscope_key", encoding="ascii").read().strip()
API = "https://dashscope.aliyuncs.com/api/v1/services/aigc/multimodal-generation/generation"
IMG = Path(r"D:\DSH_work\StS2_MadScientist\art\ref\wanx_ref.png")
OUT = Path(r"D:\DSH_work\StS2_MadScientist\art\dashscope_out")
OUT.mkdir(parents=True, exist_ok=True)

b64 = base64.b64encode(IMG.read_bytes()).decode()
body = {
    "model": "qwen-image-2.0-pro",
    "input": {
        "messages": [
            {
                "role": "user",
                "content": [
                    {"image": "data:image/png;base64," + b64},
                    {
                        "text": (
                            "Change the pose: the character swings a large wrench overhead in an attack pose. "
                            "Keep the exact same character design, face, colorful spiky hair, brass goggles, "
                            "white lab coat and backpack. Dark cave laboratory background, green sparks."
                        )
                    },
                ],
            }
        ]
    },
    "parameters": {"size": "1024*1024", "n": 1, "watermark": False},
}

r = requests.post(
    API,
    headers={"Authorization": f"Bearer {KEY}", "Content-Type": "application/json"},
    data=json.dumps(body),
    timeout=300,
)
print("api", r.status_code)
data = r.json()
url = data["output"]["choices"][0]["message"]["content"][0]["image"]
print("url", url[:120])

parts = urlsplit(url)
candidates = [url]
for host in (
    "dashscope-0484.oss-cn-beijing.aliyuncs.com",
    "dashscope-0484.oss-cn-hangzhou.aliyuncs.com",
    "dashscope-0484.oss-accelerate-overseas.aliyuncs.com",
):
    candidates.append(urlunsplit((parts.scheme, host, parts.path, parts.query, "")))

for cand in candidates:
    try:
        resp = requests.get(cand, timeout=45)
        if resp.ok and resp.headers.get("content-type", "").startswith("image"):
            (OUT / "strike_qwen.png").write_bytes(resp.content)
            print("downloaded", cand.split("?")[0][:100], len(resp.content))
            break
        print("fail", cand.split("?")[0][:100], resp.status_code)
    except Exception as exc:  # noqa: BLE001
        print("err", cand.split("?")[0][:100], type(exc).__name__)
