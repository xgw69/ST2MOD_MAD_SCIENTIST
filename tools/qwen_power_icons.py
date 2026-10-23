"""Generate the two missing power icons with qwen-image-3.0."""
from __future__ import annotations

import io
from pathlib import Path
from urllib.parse import urlsplit, urlunsplit

import requests
from PIL import Image

ROOT = Path(r"D:\DSH_work\StS2_MadScientist")
KEY = (Path.home() / ".dsh" / "secrets" / "dashscope_key").read_text(encoding="ascii").strip()
API = "https://dashscope.aliyuncs.com/api/v1/services/aigc/multimodal-generation/generation"
OUT = ROOT / "art" / "qwen_icons"

PROMPTS = {
    "block_lock_power": (
        "Game status icon of a steel shield with a bold red diagonal prohibition slash across it, "
        "flat vector style, thick black outline, dark teal background, green rim light, centered, no text."
    ),
    "blast_free_power": (
        "Game status icon of a round black bomb with a lit fuse and a glowing green energy ring, "
        "flat vector style, thick black outline, dark teal background, green glow, centered, no text."
    ),
}


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
    import json

    for name, prompt in PROMPTS.items():
        body = {
            "model": "qwen-image-3.0",
            "input": {"messages": [{"role": "user", "content": [{"text": prompt}]}]},
            "parameters": {"size": "1024*1024", "n": 1, "watermark": False, "prompt_extend": False},
        }
        r = requests.post(
            API,
            headers={"Authorization": f"Bearer {KEY}", "Content-Type": "application/json"},
            data=json.dumps(body),
            timeout=300,
        )
        if not r.ok:
            print(name, "FAILED", r.status_code, r.text[:160])
            continue
        url = r.json()["output"]["choices"][0]["message"]["content"][0]["image"]
        Image.open(io.BytesIO(download(url))).convert("RGB").save(OUT / f"{name}.png")
        print(name, "ok")


if __name__ == "__main__":
    main()
