import base64
import io
import json
from pathlib import Path
from urllib.parse import urlsplit, urlunsplit

import requests
from PIL import Image

ROOT = Path(r"D:\DSH_work\StS2_MadScientist")
KEY = (Path.home() / ".dsh" / "secrets" / "dashscope_key").read_text(encoding="ascii").strip()
API = "https://dashscope.aliyuncs.com/api/v1/services/aigc/multimodal-generation/generation"

ref = Image.open(ROOT / "MadScientist/images/character/combat.png").convert("RGB")
ref.thumbnail((768, 768), Image.LANCZOS)
buf = io.BytesIO()
ref.save(buf, format="PNG")
data_uri = "data:image/png;base64," + base64.b64encode(buf.getvalue()).decode()

prompt = (
    "Edit this reference image. Keep the character EXACTLY as she is: same face, same colorful "
    "spiky hair shape, same brass goggles, same orange scaly skin, same white lab coat, same "
    "backpack. Do not redesign her. Change only the pose: she holds a glass flask and a ghostly "
    "soul rises from it. Keep the dark cave laboratory background and the same art style."
)

body = {
    "model": "qwen-image-2.0-pro",
    "input": {"messages": [{"role": "user", "content": [{"image": data_uri}, {"text": prompt}]}]},
    "parameters": {"size": "1024*1024", "n": 1, "watermark": False, "prompt_extend": False},
}

r = requests.post(
    API,
    headers={"Authorization": f"Bearer {KEY}", "Content-Type": "application/json"},
    data=json.dumps(body),
    timeout=300,
)
print(r.status_code, r.text[:200])
if r.ok:
    url = r.json()["output"]["choices"][0]["message"]["content"][0]["image"]
    parts = urlsplit(url)
    alt = urlunsplit((parts.scheme, parts.hostname.replace(".oss-accelerate.", ".oss-accelerate-overseas."), parts.path, parts.query, ""))
    for cand in (url, alt):
        try:
            resp = requests.get(cand, timeout=120)
            if resp.ok:
                Image.open(io.BytesIO(resp.content)).convert("RGB").save(ROOT / "art" / "qwen_out" / "soul_research.png")
                print("saved")
                break
        except Exception as exc:  # noqa: BLE001
            print("dl fail", exc)
