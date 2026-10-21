"""Batch art generation via DashScope qwen-image-2.0-pro (native API).

Character cards send the combat sprite as a reference image (base64) so the
model keeps her exact design; object cards are plain text-to-image.
"""
from __future__ import annotations

import argparse
import base64
import io
import json
import time
from pathlib import Path
from urllib.parse import urlsplit, urlunsplit

import requests
from PIL import Image

ROOT = Path(__file__).resolve().parent.parent
KEY_FILE = Path.home() / ".dsh" / "secrets" / "dashscope_key"
API = "https://dashscope.aliyuncs.com/api/v1/services/aigc/multimodal-generation/generation"
OUT = ROOT / "art" / "qwen_out"
REF = ROOT / "MadScientist" / "images" / "character" / "combat.png"

CHAR_PREFIX = "a goblin scientist with orange scaly skin, colorful spiky hair, brass goggles, white lab coat and a green potion backpack"
CHAR_IDS = {
    "strike", "defend", "wrench_strike", "assault", "mixed_reagent", "knock", "giant_wrench",
    "forbidden_frenzy", "universal_correction", "machine_control", "machine_production",
    "energy_shield", "chemical_fog", "obsession", "mutation", "beneficial_mutation",
    "self_review", "ethical_boundary", "loss_of_sanity", "scientific_method",
    "scientific_legacy", "great_fusion", "process", "pseudoscientific_method",
    "plan", "think", "mad_scientist_character_card", "strength_research",
    "dexterity_research", "reflex_research", "self_repair_research",
    "infinite_machine_research", "fully_automatic", "machine_reagent", "quick_repair",
    "full_repair", "self_repair_plan", "machine_defense", "reactive_boiler",
}

KEEP = (
    "Keep the exact same character: identical face, colorful spiky hair, brass goggles, "
    "orange scaly skin, white lab coat, backpack."
)


def ref_data_uri() -> str:
    img = Image.open(REF).convert("RGB")
    img.thumbnail((768, 768), Image.LANCZOS)
    buf = io.BytesIO()
    img.save(buf, format="PNG")
    return "data:image/png;base64," + base64.b64encode(buf.getvalue()).decode()


def download(url: str) -> bytes:
    parts = urlsplit(url)
    host = parts.hostname or ""
    alt_host = host.replace(".oss-accelerate.", ".oss-accelerate-overseas.")
    candidates = [url, urlunsplit((parts.scheme, alt_host, parts.path, parts.query, ""))]
    last = None
    for cand in candidates:
        try:
            r = requests.get(cand, timeout=120)
            if r.ok and r.headers.get("content-type", "").startswith("image"):
                return r.content
            last = f"{r.status_code}"
        except Exception as exc:  # noqa: BLE001
            last = type(exc).__name__
    raise RuntimeError(f"download failed: {last}")


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--only", default="")
    parser.add_argument("--limit", type=int, default=0)
    parser.add_argument("--sleep", type=float, default=3)
    args = parser.parse_args()

    key = KEY_FILE.read_text(encoding="ascii").strip()
    items = json.loads((ROOT / "art" / "sd_prompts" / "cards.json").read_text(encoding="utf-8"))
    only = {x.strip() for x in args.only.split(",") if x.strip()}
    if only:
        items = [i for i in items if i["id"] in only]
    if args.limit:
        items = items[: args.limit]

    OUT.mkdir(parents=True, exist_ok=True)
    data_uri = ref_data_uri()

    for idx, item in enumerate(items, 1):
        dst = OUT / f"{item['id']}.png"
        if dst.exists():
            print(f"skip {item['id']}")
            continue

        subject = item["subject"]
        if item["id"] in CHAR_IDS and subject.startswith(CHAR_PREFIX):
            action = subject[len(CHAR_PREFIX):].lstrip(", ")
            text = f"{KEEP} New pose and action: {action}. Dark cave laboratory, green energy, card art."
            content = [{"image": data_uri}, {"text": text}]
        else:
            text = f"A Slay the Spire card illustration of {subject}, dark cave background, green accents, no text."
            content = [{"text": text}]

        body = {
            "model": "qwen-image-2.0-pro",
            "input": {"messages": [{"role": "user", "content": content}]},
            "parameters": {"size": "1024*1024", "n": 1, "watermark": False},
        }

        for attempt in range(3):
            try:
                r = requests.post(
                    API,
                    headers={"Authorization": f"Bearer {key}", "Content-Type": "application/json"},
                    data=json.dumps(body),
                    timeout=300,
                )
                if not r.ok:
                    print(f"retry {item['id']} {r.status_code} {r.text[:120]}")
                    time.sleep(15)
                    continue
                url = r.json()["output"]["choices"][0]["message"]["content"][0]["image"]
                data = download(url)
                Image.open(io.BytesIO(data)).convert("RGB").save(dst)
                print(f"[{idx}/{len(items)}] {item['id']}")
                break
            except Exception as exc:  # noqa: BLE001
                print(f"retry {item['id']}: {exc}")
                time.sleep(15)

        time.sleep(args.sleep)


if __name__ == "__main__":
    main()
