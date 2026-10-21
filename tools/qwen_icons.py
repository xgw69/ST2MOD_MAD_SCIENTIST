"""Generate power / relic / potion icons with qwen-image-2.0-pro (text to image)."""
from __future__ import annotations

import argparse
import base64
import io
import json
import time
from pathlib import Path
from urllib.parse import urlsplit, urlunsplit

import requests
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parent.parent
KEY_FILE = Path.home() / ".dsh" / "secrets" / "dashscope_key"
API = "https://dashscope.aliyuncs.com/api/v1/services/aigc/multimodal-generation/generation"
OUT = ROOT / "art" / "qwen_icons"

ICONS = [
    ("machine_durability_power", "power", "a brass pressure gauge icon with a glowing green needle"),
    ("machine_broken_power", "power", "a cracked iron gear with smoke, game status icon"),
    ("infinite_machine_research_power", "power", "an infinity symbol made of interlocking gears"),
    ("self_repair_research_power", "power", "a wrench tightening a gear with green sparks"),
    ("spare_spikes_power", "power", "sharp metal spikes emerging from a plate"),
    ("spare_reagent_power", "power", "a small green potion vial with a cork"),
    ("plan_power", "power", "a clipboard with a glowing blueprint"),
    ("mutation_power", "power", "a glowing DNA helix in a flask"),
    ("scorched_ground_power", "power", "cracked ground with green flames"),
    ("think_power", "power", "a glowing lightbulb with gears inside"),
    ("inventory_power", "power", "an open metal toolbox with tools"),
    ("mad_research_power", "power", "a brain in a bubbling jar"),
    ("soul_research_power", "power", "a ghostly blue soul trapped in a glass flask"),

    ("tome_of_science", "relic", "an old leather book with a green chemical formula, item icon"),
    ("tome_of_awakening", "relic", "a floating ancient book with glowing green runes, item icon"),
    ("science_goggles", "relic", "brass goggles with glowing green lenses, item icon"),
    ("reagent_backpack", "relic", "a leather backpack with potion vials, item icon"),
    ("giant_wrench_relic", "relic", "a huge polished wrench with green runes, item icon"),
    ("gear_heart", "relic", "a mechanical heart made of gears and green energy, item icon"),

    ("potion_green", "potion", "a bubbling green potion bottle, game item icon"),
    ("potion_purple", "potion", "a swirling purple serum vial, game item icon"),
]


def download(url: str) -> bytes:
    parts = urlsplit(url)
    host = parts.hostname or ""
    alt = host.replace(".oss-accelerate.", ".oss-accelerate-overseas.")
    for cand in (url, urlunsplit((parts.scheme, alt, parts.path, parts.query, ""))):
        try:
            r = requests.get(cand, timeout=120)
            if r.ok and r.headers.get("content-type", "").startswith("image"):
                return r.content
        except Exception:  # noqa: BLE001
            pass
    raise RuntimeError("download failed")


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--only", default="")
    parser.add_argument("--sleep", type=float, default=2)
    args = parser.parse_args()

    key = KEY_FILE.read_text(encoding="ascii").strip()
    OUT.mkdir(parents=True, exist_ok=True)
    only = {x.strip() for x in args.only.split(",") if x.strip()}

    for idx, (name, kind, prompt) in enumerate(ICONS, 1):
        if only and name not in only:
            continue
        dst = OUT / f"{name}.png"
        if dst.exists():
            continue

        text = (
            f"Game icon of {prompt}, centered, simple bold shapes, flat cel shading, "
            "dark teal background, green accents, no text, no border"
        )
        body = {
            "model": "qwen-image-2.0-pro",
            "input": {"messages": [{"role": "user", "content": [{"text": text}]}]},
            "parameters": {"size": "1024*1024", "n": 1, "watermark": False},
        }

        for _ in range(3):
            try:
                r = requests.post(
                    API,
                    headers={"Authorization": f"Bearer {key}", "Content-Type": "application/json"},
                    data=json.dumps(body),
                    timeout=300,
                )
                if not r.ok:
                    print("retry", name, r.status_code)
                    time.sleep(15)
                    continue
                url = r.json()["output"]["choices"][0]["message"]["content"][0]["image"]
                Image.open(io.BytesIO(download(url))).convert("RGB").save(dst)
                print(f"[{idx}/{len(ICONS)}] {name} ({kind})")
                break
            except Exception as exc:  # noqa: BLE001
                print("retry", name, exc)
                time.sleep(15)

        time.sleep(args.sleep)


if __name__ == "__main__":
    main()
