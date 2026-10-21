"""Free online generation via the public FLUX.1 Kontext HF Space.

Kontext edits an existing image, so the character keeps her exact design while
the prompt gives her a new pose / action.  This script:
  1. composes a 832x640 canvas (original cave background + character cutout)
  2. calls the free Space for every card, skipping ones already generated
"""
from __future__ import annotations

import argparse
import json
import shutil
import time
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parent.parent
IN = ROOT / "art" / "kontext_in"
OUT = ROOT / "art" / "kontext_out"
SPACE = "https://black-forest-labs-flux-1-kontext-dev.hf.space"

CHAR_DESC = "the same character: identical face, colorful spiky hair, brass goggles, orange scaly skin, white lab coat, backpack"
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

W, H = 832, 640


def build_inputs(items: list[dict]) -> None:
    IN.mkdir(parents=True, exist_ok=True)
    bg = Image.open(ROOT / "art" / "ref" / "event_bg.png").convert("RGB")
    scale = max(W / bg.width, H / bg.height)
    bg = bg.resize((round(bg.width * scale), round(bg.height * scale)), Image.LANCZOS)
    left = (bg.width - W) // 2
    top = (bg.height - H) // 2
    bg = bg.crop((left, top, left + W, top + H))

    char = Image.open(ROOT / "art" / "ref" / "event_cutout.png").convert("RGBA")
    h = 560
    w = round(char.width * h / char.height)
    char = char.resize((w, h), Image.LANCZOS)

    for item in items:
        canvas = bg.copy()
        if item["id"] in CHAR_IDS:
            canvas.paste(char, (W - w - 20, H - h - 10), char)
        canvas.save(IN / f"{item['id']}.png")


def prompt_for(item: dict) -> str:
    subject = item["subject"]
    if subject.startswith(CHAR_PREFIX):
        action = subject[len(CHAR_PREFIX):].lstrip(", ").strip()
        return (
            f"Keep {CHAR_DESC}. New pose and action: {action}. "
            "Dark cave laboratory, green energy, Slay the Spire card art."
        )
    return (
        f"A Slay the Spire card illustration of {subject}, dark cave background, "
        "green accents, flat cel shading, no text."
    )


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--only", default="")
    parser.add_argument("--limit", type=int, default=0)
    parser.add_argument("--sleep", type=float, default=2.0)
    args = parser.parse_args()

    items = json.loads((ROOT / "art" / "sd_prompts" / "cards.json").read_text(encoding="utf-8"))
    only = {x.strip() for x in args.only.split(",") if x.strip()}
    if only:
        items = [i for i in items if i["id"] in only]
    if args.limit:
        items = items[: args.limit]

    build_inputs(items)
    OUT.mkdir(parents=True, exist_ok=True)

    from gradio_client import Client, handle_file

    client = Client(SPACE, verbose=False)
    print("connected")

    for i, item in enumerate(items, 1):
        dst = OUT / f"{item['id']}.webp"
        if dst.exists():
            print(f"skip {item['id']}")
            continue

        prompt = prompt_for(item)
        for attempt in range(3):
            try:
                result = client.predict(
                    handle_file(str(IN / f"{item['id']}.png")),
                    prompt,
                    0,
                    True,
                    2.5,
                    28,
                    api_name="/infer",
                )
                path = result[0] if isinstance(result, (list, tuple)) else result
                if isinstance(path, dict):
                    path = path.get("path")
                shutil.copy(path, dst)
                print(f"[{i}/{len(items)}] {item['id']}")
                break
            except Exception as exc:  # noqa: BLE001
                print(f"retry {item['id']} ({attempt + 1}/3): {exc}")
                time.sleep(10)

        time.sleep(args.sleep)


if __name__ == "__main__":
    main()
