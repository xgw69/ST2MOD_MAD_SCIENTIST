"""Generate rest-site / merchant / multiplayer-hand art with the character reference."""
from __future__ import annotations

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
OUT = ROOT / "art" / "qwen_rooms"
OUT.mkdir(parents=True, exist_ok=True)

KEEP = (
    "Keep the exact same character: identical face, colorful spiky hair, brass goggles, "
    "orange scaly skin, white lab coat, backpack. Hands must be anatomically correct with "
    "exactly five fingers, no extra or fused fingers."
)

PROMPTS = {
    "rest_site": (
        KEEP + " The character is sitting relaxed on a wooden chair beside a campfire, "
        "full body, three-quarter view, hands resting on the knees, warm firelight, dark cave."
    ),
    "merchant": (
        KEEP + " The character is sitting behind a merchant rug covered with potions, cards "
        "and trinkets, three-quarter view, one hand gesturing over the wares, dark cave shop."
    ),
    "multiplayer_hand": (
        "Close-up of a right hand reaching into a wooden treasure chest. The hand has EXACTLY "
        "five digits: one thumb on the left and four fingers, no sixth finger. Orange scaly "
        "skin, sharp claws, white coat sleeve, green glow from inside the chest, dark cave."
    ),
}


def data_uri() -> str:
    img = Image.open(ROOT / "MadScientist/images/character/combat.png").convert("RGB")
    img.thumbnail((768, 768), Image.LANCZOS)
    buf = io.BytesIO()
    img.save(buf, format="PNG")
    return "data:image/png;base64," + base64.b64encode(buf.getvalue()).decode()


def download(url: str) -> bytes:
    parts = urlsplit(url)
    alt = urlunsplit(
        (parts.scheme, parts.hostname.replace(".oss-accelerate.", ".oss-accelerate-overseas."), parts.path, parts.query, "")
    )
    for cand in (url, alt):
        try:
            r = requests.get(cand, timeout=120)
            if r.ok and r.headers.get("content-type", "").startswith("image"):
                return r.content
        except Exception:  # noqa: BLE001
            pass
    raise RuntimeError("download failed")


def main() -> None:
    import sys
    only = sys.argv[1:] if len(sys.argv) > 1 else list(PROMPTS)
    ref = data_uri()
    for name in only:
        prompt = PROMPTS[name]
        dst = OUT / f"{name}.png"
        if dst.exists():
            print("skip", name)
            continue

        body = {
            "model": "qwen-image-2.0-pro",
            "input": {"messages": [{"role": "user", "content": [{"image": ref}, {"text": prompt}]}]},
            "parameters": {"size": "1024*1024", "n": 1, "watermark": False, "prompt_extend": False},
        }
        r = requests.post(
            API,
            headers={"Authorization": f"Bearer {KEY}", "Content-Type": "application/json"},
            data=json.dumps(body),
            timeout=300,
        )
        if not r.ok:
            print(name, "FAILED", r.status_code, r.text[:200])
            break
        url = r.json()["output"]["choices"][0]["message"]["content"][0]["image"]
        Image.open(io.BytesIO(download(url))).convert("RGB").save(dst)
        print(name, "ok")


if __name__ == "__main__":
    main()
