"""Compose the Steam Workshop preview image with bilingual text."""
from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = Path(__file__).resolve().parent.parent
CHAR = ROOT / "dist" / "merchant_full.png"
OUT = ROOT / "dist" / "workshop_image.png"

W = H = 1024
GOLD = (232, 193, 90)
GREEN = (168, 224, 95)
WHITE = (245, 245, 240)

zh_font = "C:/Windows/Fonts/msyhbd.ttc"
en_font = "C:/Windows/Fonts/bahnschrift.ttf"


def background() -> Image.Image:
    img = Image.new("RGBA", (W, H), (10, 16, 14, 255))
    d = ImageDraw.Draw(img)
    for y in range(H):
        t = y / H
        c = (
            int(18 + 14 * (1 - t)),
            int(32 + 18 * (1 - t)),
            int(26 + 14 * (1 - t)),
            255,
        )
        d.line([(0, y), (W, y)], fill=c)

    # soft green glow behind the character
    glow = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    gd = ImageDraw.Draw(glow)
    for i in range(9):
        r = 380 - i * 34
        a = 22 + i * 6
        gd.ellipse((720 - r, 560 - r, 720 + r, 560 + r), fill=(168, 224, 95, a))
    glow = glow.filter(ImageFilter.GaussianBlur(60))
    img.alpha_composite(glow)

    return img


def spaced(draw, xy, text, font, fill, spacing=2):
    x, y = xy
    for ch in text:
        draw.text((x, y), ch, font=font, fill=fill)
        x += draw.textlength(ch, font=font) + spacing


def main() -> None:
    img = background()

    char = Image.open(CHAR).convert("RGBA")
    target_h = 900
    char = char.resize((round(char.width * target_h / char.height), target_h), Image.LANCZOS)
    img.alpha_composite(char, (W - char.width - 10, H - char.height - 8))

    draw = ImageDraw.Draw(img)
    zh_title = ImageFont.truetype(zh_font, 82)
    en_title = ImageFont.truetype(en_font, 38)
    zh_sub = ImageFont.truetype(zh_font, 29)
    en_sub = ImageFont.truetype(en_font, 22)
    small = ImageFont.truetype(en_font, 21)

    draw.text((58, 62), "疯狂科学家", font=zh_title, fill=WHITE, stroke_width=3, stroke_fill=(8, 12, 10))
    spaced(draw, (62, 160), "THE MAD SCIENTIST", en_title, GOLD, spacing=3)
    draw.rectangle((60, 216, 430, 220), fill=GREEN)
    draw.text((60, 244), "卡牌融合 · 机器耐久", font=zh_sub, fill=WHITE)
    draw.text((60, 284), "63 张原创卡牌", font=zh_sub, fill=WHITE)
    spaced(draw, (62, 330), "CARD FUSION · MACHINE DURABILITY", en_sub, (215, 215, 205), 1)

    draw.text((58, 906), "需要 BaseLib  ·  Requires BaseLib", font=zh_sub, fill=(225, 225, 215))
    spaced(draw, (60, 950), "PUBLIC BETA  ·  V0.1.0", small, GOLD, 2)

    draw.rectangle((16, 16, W - 16, H - 16), outline=GOLD, width=3)
    img.convert("RGB").save(OUT, optimize=True)
    print(OUT, OUT.stat().st_size // 1024, "KB")


if __name__ == "__main__":
    main()
