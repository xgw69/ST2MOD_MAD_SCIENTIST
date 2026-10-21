"""Convert raw SD outputs into the mod's card / power art layout.

Raw images live in art/sd_out/{id}.png (832x640).  This script cover-crops them
to the sizes the game expects:

    MadScientist/images/card_portraits/{id}.png        250x190
    MadScientist/images/card_portraits/big/{id}.png    1000x760
    MadScientist/images/powers/{id}.png                128x128 (center crop)
    MadScientist/images/relics/{id}.png                94x94 (center crop)
    MadScientist/images/relics/big/{id}.png            256x256

Relic ids are prefixed with tome_ in the prompt list.
"""
from __future__ import annotations

import argparse
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parent.parent
SRC = ROOT / "art" / "sd_out"
PORTRAITS = ROOT / "MadScientist" / "images" / "card_portraits"
POWERS = ROOT / "MadScientist" / "images" / "powers"
RELICS = ROOT / "MadScientist" / "images" / "relics"

CARD_IDS = [
    "strike", "defend", "wrench_strike", "mixed_reagent", "knock", "jolt", "blast",
    "rumble", "spare_spikes", "assault", "x_ray", "scorched_ground", "spatial_tear",
    "machine_rumble", "machine_blast", "machine_reagent", "fully_automatic",
    "spare_reagent", "universal_correction", "forbidden_frenzy", "blasting_device",
    "forbidden_science", "giant_wrench", "scientific_method", "scientific_legacy",
    "self_repair_plan", "quick_repair", "full_repair", "machine_defense",
    "machine_production", "machine_control", "energy_shield", "reactive_boiler",
    "chemical_fog", "obsession", "mutation", "beneficial_mutation", "self_review",
    "ethical_boundary", "loss_of_sanity", "assemble", "defense_module",
    "speed_module", "energy_module", "attack_module", "plan", "think",
    "great_fusion", "process", "pseudoscientific_method", "mad_scientist_character_card",
    "strength_research", "dexterity_research", "reflex_research",
    "self_repair_research", "infinite_machine_research",
]

POWER_IDS = [
    "machine_durability_power", "machine_broken_power", "infinite_machine_research_power",
    "self_repair_research_power", "spare_spikes_power", "spare_reagent_power",
    "plan_power", "mutation_power", "scorched_ground_power", "think_power",
]

RELIC_IDS = ["tome_of_science", "tome_of_awakening"]


def cover(img: Image.Image, size: tuple[int, int]) -> Image.Image:
    w, h = size
    scale = max(w / img.width, h / img.height)
    resized = img.resize((round(img.width * scale), round(img.height * scale)), Image.LANCZOS)
    left = (resized.width - w) // 2
    top = (resized.height - h) // 2
    return resized.crop((left, top, left + w, top + h))


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--only", default="")
    args = parser.parse_args()
    only = {x.strip() for x in args.only.split(",") if x.strip()}

    PORTRAITS.mkdir(parents=True, exist_ok=True)
    (PORTRAITS / "big").mkdir(exist_ok=True)
    POWERS.mkdir(parents=True, exist_ok=True)
    (POWERS / "big").mkdir(exist_ok=True)
    RELICS.mkdir(parents=True, exist_ok=True)
    (RELICS / "big").mkdir(exist_ok=True)

    for cid in CARD_IDS:
        if only and cid not in only:
            continue
        raw = SRC / f"{cid}.png"
        if not raw.exists():
            print(f"missing {raw}")
            continue
        img = Image.open(raw).convert("RGB")
        cover(img, (250, 190)).save(PORTRAITS / f"{cid}.png")
        cover(img, (1000, 760)).save(PORTRAITS / "big" / f"{cid}.png")
        print(f"card {cid}")

    for pid in POWER_IDS:
        if only and pid not in only:
            continue
        raw = SRC / f"{pid}.png"
        if not raw.exists() or pid.startswith("machine_"):
            continue
        img = Image.open(raw).convert("RGB")
        icon = cover(img, (128, 128))
        icon.save(POWERS / f"{pid}.png")
        icon.save(POWERS / "big" / f"{pid}.png")
        print(f"power {pid}")

    for rid in RELIC_IDS:
        if only and rid not in only:
            continue
        raw = SRC / f"{rid}.png"
        if not raw.exists():
            print(f"missing {raw}")
            continue
        img = Image.open(raw).convert("RGB")
        cover(img, (94, 94)).save(RELICS / f"{rid}.png")
        cover(img, (256, 256)).save(RELICS / "big" / f"{rid}.png")
        print(f"relic {rid}")


if __name__ == "__main__":
    main()
