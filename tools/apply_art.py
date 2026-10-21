"""Install generated art into the mod's image folders.

Cards  -> images/card_portraits/{id}.png (250x190) + big/{id}.png (1000x760)
Powers -> images/powers/{id}.png (128) + big/{id}.png (256), circular alpha
Relics -> images/relics/{id}.png (94) + big/{id}.png (256) + _outline
Potions-> images/potions/{id}.png (256)
"""
from __future__ import annotations

import json
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parent.parent
CARDS_SRC = ROOT / "art" / "qwen_out"
ICONS_SRC = ROOT / "art" / "qwen_icons"
IMG = ROOT / "MadScientist" / "images"


def cover(img: Image.Image, size: tuple[int, int]) -> Image.Image:
    w, h = size
    scale = max(w / img.width, h / img.height)
    im = img.resize((round(img.width * scale), round(img.height * scale)), Image.LANCZOS)
    left = (im.width - w) // 2
    top = (im.height - h) // 2
    return im.crop((left, top, left + w, top + h))


def circular(img: Image.Image, size: int) -> Image.Image:
    im = cover(img.convert("RGB"), (size, size)).convert("RGBA")
    mask = Image.new("L", (size * 4, size * 4), 0)
    ImageDraw.Draw(mask).ellipse((6, 6, size * 4 - 6, size * 4 - 6), fill=255)
    mask = mask.resize((size, size), Image.LANCZOS)
    im.putalpha(mask)
    return im


def outline(im: Image.Image) -> Image.Image:
    alpha = im.getchannel("A")
    alpha = alpha.filter(ImageFilter.MaxFilter(5))
    white = Image.new("RGBA", im.size, (255, 255, 255, 255))
    white.putalpha(alpha)
    return white


def main() -> None:
    items = json.loads((ROOT / "art" / "sd_prompts" / "cards.json").read_text(encoding="utf-8"))
    (IMG / "card_portraits" / "big").mkdir(parents=True, exist_ok=True)

    # Ancient cards use a portrait art area (600x847) instead of the normal 4:3 (1000x760).
    ancient = {"infinite_machine_research", "pseudoscientific_method"}

    for item in items:
        src = CARDS_SRC / f"{item['id']}.png"
        if not src.exists():
            continue
        img = Image.open(src).convert("RGB")
        if item["id"] in ancient:
            cover(img, (250, 353)).save(IMG / "card_portraits" / f"{item['id']}.png")
            cover(img, (600, 847)).save(IMG / "card_portraits" / "big" / f"{item['id']}.png")
        else:
            cover(img, (250, 190)).save(IMG / "card_portraits" / f"{item['id']}.png")
            cover(img, (1000, 760)).save(IMG / "card_portraits" / "big" / f"{item['id']}.png")

    (IMG / "powers" / "big").mkdir(parents=True, exist_ok=True)
    (IMG / "relics" / "big").mkdir(parents=True, exist_ok=True)
    (IMG / "potions").mkdir(parents=True, exist_ok=True)

    if ICONS_SRC.exists():
        for src in ICONS_SRC.glob("*.png"):
            name = src.stem
            img = Image.open(src)
            if name.endswith("_power"):
                circular(img, 128).save(IMG / "powers" / f"{name}.png")
                circular(img, 256).save(IMG / "powers" / "big" / f"{name}.png")
            elif name.startswith("potion_"):
                cover(img.convert("RGB"), (256, 256)).save(IMG / "potions" / f"{name}.png")
            else:
                icon = circular(img, 94)
                icon.save(IMG / "relics" / f"{name}.png")
                outline(icon).save(IMG / "relics" / f"{name}_outline.png")
                big = circular(img, 256)
                big.save(IMG / "relics" / "big" / f"{name}.png")

    print("art installed")


if __name__ == "__main__":
    main()
