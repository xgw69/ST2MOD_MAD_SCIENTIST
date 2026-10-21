"""Compose scenes + masks from the ORIGINAL game art.

For character cards the event cutout is pasted on the event background and the
mask protects her silhouette; inpainting then paints the whole scene around her.
"""
from __future__ import annotations

import json
from pathlib import Path

from PIL import Image, ImageFilter

ROOT = Path(__file__).resolve().parent.parent
BG = ROOT / "art" / "ref" / "event_bg.png"
CHAR = ROOT / "art" / "ref" / "event_cutout.png"
OUT = ROOT / "art" / "sd_in"
W, H = 832, 640

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


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)

    bg = Image.open(BG).convert("RGB")
    scale = max(W / bg.width, H / bg.height)
    bg = bg.resize((round(bg.width * scale), round(bg.height * scale)), Image.LANCZOS)
    left = (bg.width - W) // 2
    top = (bg.height - H) // 2
    bg = bg.crop((left, top, left + W, top + H))

    char = Image.open(CHAR).convert("RGBA")
    char_h = 520
    char_w = round(char.width * char_h / char.height)
    char = char.resize((char_w, char_h), Image.LANCZOS)
    pos = (W - char_w - 30, H - char_h - 5)

    scene = bg.copy()
    scene.paste(char, pos, char)
    scene.save(OUT / "_base.png")

    # mask: black = keep (character), white = repaint
    mask = Image.new("L", (W, H), 255)
    alpha = Image.new("L", (W, H), 0)
    alpha.paste(char.getchannel("A"), pos)
    alpha = alpha.point(lambda v: 255 if v > 12 else 0)
    alpha = alpha.filter(ImageFilter.MaxFilter(9))
    mask.paste(0, (0, 0), alpha)
    mask.save(OUT / "_mask.png")

    items = json.loads((ROOT / "art" / "sd_prompts" / "cards.json").read_text(encoding="utf-8"))
    for item in items:
        if item["id"] in CHAR_IDS:
            scene.save(OUT / f"{item['id']}.png")
            mask.save(OUT / f"{item['id']}_mask.png")
        else:
            bg.save(OUT / f"{item['id']}.png")
            Image.new("L", (W, H), 255).save(OUT / f"{item['id']}_mask.png")
    print(f"composed {len(items)} scenes -> {OUT}")


if __name__ == "__main__":
    main()
