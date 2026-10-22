"""Cut room art with RMBG-1.4 (local, free)."""
from __future__ import annotations

import sys
from pathlib import Path

import numpy as np
import torch
import torch.nn.functional as F
from PIL import Image

sys.path.insert(0, r"D:\DSH_work\tools")
from rmbg.briarmbg import BriaRMBG  # noqa: E402

ROOT = Path(r"D:\DSH_work\StS2_MadScientist")
SRC = ROOT / "art" / "qwen_rooms2"
IMG = ROOT / "MadScientist" / "images"
MODEL = r"D:\DSH_work\tools\rmbg"


def mask_for(img: Image.Image, net) -> Image.Image:
    w, h = img.size
    arr = np.array(img.convert("RGB"))
    t = torch.tensor(arr, dtype=torch.float32).permute(2, 0, 1).unsqueeze(0)
    t = F.interpolate(t, size=(1024, 1024), mode="bilinear", align_corners=False)
    t = (t / 255.0 - 0.5).to("cuda")
    with torch.no_grad():
        out = net(t)
    while isinstance(out, (list, tuple)):
        out = out[0]
    if out.dim() == 3:
        out = out.unsqueeze(0)
    out = F.interpolate(out, size=(h, w), mode="bilinear", align_corners=False)[0, 0]
    out = (out - out.min()) / (out.max() - out.min())
    alpha = (out * 255).to(torch.uint8).cpu().numpy()
    return Image.fromarray(alpha, mode="L")


def trim(img: Image.Image) -> Image.Image:
    bbox = img.getbbox()
    return img.crop(bbox) if bbox else img


def main() -> None:
    net = BriaRMBG.from_pretrained(MODEL).to("cuda").eval()
    for name, target_h in (("rest_site", 310), ("merchant", 434)):
        src = SRC / f"{name}.png"
        if not src.exists():
            continue
        img = Image.open(src).convert("RGB")
        alpha = mask_for(img, net)
        out = img.convert("RGBA")
        out.putalpha(alpha)
        out = trim(out)
        out = out.resize((round(out.width * target_h / out.height), target_h), Image.LANCZOS)
        out.save(IMG / "rooms" / f"{name}.png")
        print(name, out.size)

    # Mod thumbnail: keep the square canvas, only clean the background.
    mod = IMG / "mod_image.png"
    if mod.exists():
        img = Image.open(mod).convert("RGB")
        alpha = mask_for(img, net)
        out = img.convert("RGBA")
        out.putalpha(alpha)
        out.save(mod)
        print("mod_image", out.size)


if __name__ == "__main__":
    main()
