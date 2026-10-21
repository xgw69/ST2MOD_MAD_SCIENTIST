"""Regenerate drifted cards: strict reference edit, no prompt rewriting."""
from __future__ import annotations

import argparse
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

ACTIONS = {
    "mechanical_ascension": "she is rising into the air with mechanical gear wings on her back",
    "reptilian_evolution_project": "she is injecting a glowing green serum into her arm",
    "scientific_research": "she is examining floating cards through a magnifying glass",
    "refine": "she is holding two identical glowing potion vials",
    "inventory": "she is putting cards into an open metal toolbox",
    "mad_research": "glowing formula papers are floating around her",
    "soul_research": "a ghostly soul is rising from the glass flask in her hand",
}

STRICT = (
    "Edit this reference image. The character must stay EXACTLY identical: same face, "
    "same colorful spiky hair shape, same brass goggles, same orange scaly skin, same white "
    "lab coat, same backpack. Do not redesign her, do not change her species. "
    "Keep the dark cave laboratory and the same card art style. Change only the pose: {action}."
)


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
    parser = argparse.ArgumentParser()
    parser.add_argument("--ids", default=",".join(ACTIONS))
    args = parser.parse_args()

    ref = data_uri()
    for name in [x.strip() for x in args.ids.split(",") if x.strip()]:
        action = ACTIONS.get(name)
        if not action:
            continue

        body = {
            "model": "qwen-image-2.0-pro",
            "input": {
                "messages": [
                    {
                        "role": "user",
                        "content": [
                            {"image": ref},
                            {"text": STRICT.format(action=action)},
                        ],
                    }
                ]
            },
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
        data = download(url)
        Image.open(io.BytesIO(data)).convert("RGB").save(ROOT / "art" / "qwen_out" / f"{name}.png")
        print(name, "ok")


if __name__ == "__main__":
    main()
