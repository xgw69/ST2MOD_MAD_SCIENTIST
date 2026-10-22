"""Cut the white background off room/hand art and install into the mod."""
from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parent.parent
SRC = ROOT / "art" / "qwen_rooms2"
IMG = ROOT / "MadScientist" / "images"


def cut_white(img: Image.Image, tol: int = 18) -> Image.Image:
    img = img.convert("RGB")
    sentinel = (1, 2, 3)
    work = img.copy()
    w, h = work.size
    for seed in ((0, 0), (w - 1, 0), (0, h - 1), (w - 1, h - 1), (w // 2, 0), (w // 2, h - 1)):
        try:
            ImageDraw.floodfill(work, seed, sentinel, thresh=tol)
        except Exception:  # noqa: BLE001
            pass
    rgba = img.convert("RGBA")
    px = rgba.load()
    wp = work.load()
    for y in range(h):
        for x in range(w):
            if wp[x, y] == sentinel:
                px[x, y] = (0, 0, 0, 0)
    return rgba


def trim(img: Image.Image) -> Image.Image:
    bbox = img.getbbox()
    return img.crop(bbox) if bbox else img


def main() -> None:
    (IMG / "rooms").mkdir(parents=True, exist_ok=True)
    (IMG / "ui" / "hands").mkdir(parents=True, exist_ok=True)

    for name in ("rest_site", "merchant"):
        src = SRC / f"{name}.png"
        if not src.exists():
            continue
        out = trim(cut_white(Image.open(src)))
        out.save(IMG / "rooms" / f"{name}.png")
        print(name, out.size)

    sheet = SRC / "hand_sheet.png"
    if sheet.exists():
        img = Image.open(sheet)
        w, h = img.size
        cells = {
            "rock": (0, 0, w // 2, h // 2),
            "paper": (w // 2, 0, w, h // 2),
            "scissors": (0, h // 2, w // 2, h),
            "point": (w // 2, h // 2, w, h),
        }
        for name, box in cells.items():
            cell = trim(cut_white(img.crop(box)))
            cell.save(IMG / "ui" / "hands" / f"multiplayer_hand_madscientist_{name}.png")
            print(name, cell.size)


if __name__ == "__main__":
    main()
