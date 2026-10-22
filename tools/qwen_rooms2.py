"""Room art v2: white background, black outline, qwen-image-3.0."""
from __future__ import annotations

import base64
import io
import sys
from pathlib import Path
from urllib.parse import urlsplit, urlunsplit

import requests
from PIL import Image

ROOT = Path(r"D:\DSH_work\StS2_MadScientist")
KEY = (Path.home() / ".dsh" / "secrets" / "dashscope_key").read_text(encoding="ascii").strip()
API = "https://dashscope.aliyuncs.com/api/v1/services/aigc/multimodal-generation/generation"
OUT = ROOT / "art" / "qwen_rooms2"
OUT.mkdir(parents=True, exist_ok=True)

CHAR = (
    "the same goblin scientist as the reference: identical face, colorful spiky hair, "
    "brass goggles, orange scaly skin, white lab coat, backpack. Hands anatomically correct "
    "with exactly five fingers."
)
STYLE = (
    "Full body, thick black outline, flat cel shading, bold colors, centered, "
    "pure white background, no shadow, no scenery, no text, game character asset."
)

PROMPTS = {
    "rest_site": f"{CHAR} She is sitting on the ground with her legs stretched to one side, "
                 f"hands resting on her knees, no chair, no furniture, no scenery, "
                 f"legs and feet fully visible, three-quarter front view. {STYLE}",
    "merchant": f"{CHAR} She is standing upright and gesturing with one hand, the other hand relaxed at her side, "
                f"no counter, no furniture, no props, full body, legs and feet visible, front view. {STYLE}",
    "hand_sheet": (
        "A 2x2 grid of four hand-and-forearm assets on a pure white background. Each cell shows a LONG "
        "forearm entering from the bottom with a white lab coat sleeve, and the hand at the top; the forearm "
        "must be about twice as long as the hand. Right hands with orange scaly skin, sharp claws and thick "
        "black outline: top-left a closed fist (rock), top-right an open palm (paper), bottom-left a two-finger "
        "V sign (scissors), bottom-right an index finger pointing (point). No text, no labels, no shadows."
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
    names = sys.argv[1:] if len(sys.argv) > 1 else list(PROMPTS)
    ref = data_uri()
    for name in names:
        dst = OUT / f"{name}.png"
        if dst.exists():
            print("skip", name)
            continue
        content = [{"image": ref}, {"text": PROMPTS[name]}] if name != "hand_sheet" else [
            {"image": ref},
            {"text": PROMPTS[name]},
        ]
        body = {
            "model": "qwen-image-3.0",
            "input": {"messages": [{"role": "user", "content": content}]},
            "parameters": {"size": "1024*1024", "n": 1, "watermark": False, "prompt_extend": False},
        }
        r = requests.post(
            API,
            headers={"Authorization": f"Bearer {KEY}", "Content-Type": "application/json"},
            data=body and __import__("json").dumps(body),
            timeout=300,
        )
        if not r.ok:
            print(name, "FAILED", r.status_code, r.text[:160])
            continue
        url = r.json()["output"]["choices"][0]["message"]["content"][0]["image"]
        Image.open(io.BytesIO(download(url))).convert("RGB").save(dst)
        print(name, "ok")


if __name__ == "__main__":
    main()
