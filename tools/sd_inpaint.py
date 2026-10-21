"""Inpaint composed scenes: character pixels stay untouched, the rest is painted."""
from __future__ import annotations

import argparse
import json
import os
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parent.parent
STYLE = "{subject}, dark cave, acid green accents, flat cel shading, bold outlines, slay the spire style, no text"
NEGATIVE = "text, watermark, signature, frame, border, blurry, low quality, extra limbs, deformed hands, human man"


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--model", default="D:/DSH_work/tools/sd/sd_xl_base_1.0.safetensors")
    parser.add_argument("--config", default="D:/DSH_work/tools/sd/sdxl_cfg")
    parser.add_argument("--prompts", default="art/sd_prompts/cards.json")
    parser.add_argument("--src", default="art/sd_in")
    parser.add_argument("--out", default="art/sd_out")
    parser.add_argument("--steps", type=int, default=25)
    parser.add_argument("--cfg", type=float, default=6.0)
    parser.add_argument("--width", type=int, default=832)
    parser.add_argument("--height", type=int, default=640)
    parser.add_argument("--only", default="")
    parser.add_argument("--limit", type=int, default=0)
    args = parser.parse_args()

    items = json.loads(Path(args.prompts).read_text(encoding="utf-8"))
    only = {x.strip() for x in args.only.split(",") if x.strip()}
    if only:
        items = [i for i in items if i["id"] in only]
    if args.limit:
        items = items[: args.limit]

    out = Path(args.out)
    out.mkdir(parents=True, exist_ok=True)

    import torch
    from diffusers import StableDiffusionXLInpaintPipeline

    os.environ.setdefault("HF_HUB_OFFLINE", "1")
    os.environ.setdefault("TRANSFORMERS_OFFLINE", "1")

    print("loading inpaint pipeline ...")
    pipe = StableDiffusionXLInpaintPipeline.from_single_file(
        args.model,
        torch_dtype=torch.float16,
        use_safetensors=True,
        config=args.config,
    )
    pipe.enable_model_cpu_offload()
    pipe.enable_vae_slicing()

    done = 0
    for item in items:
        img_path = Path(args.src) / f"{item['id']}.png"
        mask_path = Path(args.src) / f"{item['id']}_mask.png"
        if not img_path.exists() or not mask_path.exists():
            print(f"missing {img_path}")
            continue

        prompt = STYLE.format(subject=item["subject"])
        image = pipe(
            prompt=prompt,
            negative_prompt=NEGATIVE,
            image=Image.open(img_path).convert("RGB"),
            mask_image=Image.open(mask_path).convert("L"),
            width=args.width,
            height=args.height,
            num_inference_steps=args.steps,
            guidance_scale=args.cfg,
            generator=torch.Generator("cuda").manual_seed(20261021 + done),
        ).images[0]
        image.save(out / f"{item['id']}.png")
        done += 1
        print(f"[{done}/{len(items)}] {item['id']}")


if __name__ == "__main__":
    main()
