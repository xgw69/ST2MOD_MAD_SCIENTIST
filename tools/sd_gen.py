"""Local Stable Diffusion XL batch generator for card / relic art.

Usage:
    python tools/sd_gen.py --prompts art/sd_prompts/cards.json --out art/sd_out
    python tools/sd_gen.py --only strike,defend --limit 2

This uses a single-file SDXL checkpoint (downloaded from ModelScope) with the
local GPU.  It is intentionally free / offline.
"""
from __future__ import annotations

import argparse
import json
import os
from pathlib import Path

from PIL import Image

STYLE = (
    "{subject}, trading card illustration, dark teal laboratory background, "
    "acid green accent lighting, flat cel shading, bold clean outlines, "
    "painterly highlights, slay the spire art style, centered composition, "
    "no text, no border, no watermark"
)

NEGATIVE = (
    "text, letters, watermark, signature, frame, border, ui, blurry, low quality, "
    "extra limbs, deformed hands, bad anatomy, photo, photorealistic, human man, old man, white beard, human face"
)


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--model", default=r"D:\DSH_work\tools\sd\sd_xl_base_1.0.safetensors")
    parser.add_argument("--config", default="D:/DSH_work/tools/sd/sdxl_cfg")
    parser.add_argument("--prompts", default="art/sd_prompts/cards.json")
    parser.add_argument("--out", default="art/sd_out")
    parser.add_argument("--width", type=int, default=832)
    parser.add_argument("--height", type=int, default=640)
    parser.add_argument("--steps", type=int, default=28)
    parser.add_argument("--cfg", type=float, default=6.0)
    parser.add_argument("--seed", type=int, default=20261021)
    parser.add_argument("--only", default="")
    parser.add_argument("--limit", type=int, default=0)
    parser.add_argument("--cpu-offload", action="store_true")
    parser.add_argument("--ref", default="MadScientist/images/character/combat.png")
    parser.add_argument("--ip-scale", type=float, default=0.55)
    parser.add_argument("--ip-adapter-dir", default="D:/DSH_work/tools/sd/ip-adapter")
    parser.add_argument("--no-ip-adapter", action="store_true")
    parser.add_argument("--dry-run", action="store_true")
    args = parser.parse_args()

    items = json.loads(Path(args.prompts).read_text(encoding="utf-8"))
    only = {x.strip() for x in args.only.split(",") if x.strip()}
    if only:
        items = [i for i in items if i["id"] in only]
    if args.limit:
        items = items[: args.limit]

    out_dir = Path(args.out)
    out_dir.mkdir(parents=True, exist_ok=True)

    ref_embeds = None

    if args.dry_run:
        for item in items:
            print(f"{item['id']}: {STYLE.format(subject=item['subject'])}")
        print(f"total {len(items)}")
        return

    import torch
    from diffusers import StableDiffusionXLPipeline

    print(f"loading {args.model} ...")
    os.environ.setdefault("HF_HUB_OFFLINE", "1")
    os.environ.setdefault("TRANSFORMERS_OFFLINE", "1")
    pipe = StableDiffusionXLPipeline.from_single_file(
        args.model,
        torch_dtype=torch.float16,
        use_safetensors=True,
        config=args.config,
    )
    # 8 GB VRAM: keep only the active module on GPU, everything else in RAM.
    # This avoids Windows shared-memory thrashing (14 s/step -> ~1.5 s/step).
    pipe.enable_model_cpu_offload()
    pipe.enable_vae_slicing()

    ref_embeds = None
    if not args.no_ip_adapter and Path(args.ip_adapter_dir).exists() and Path(args.ref).exists():
        print("loading IP-Adapter ...")
        pipe.load_ip_adapter(
            args.ip_adapter_dir,
            subfolder="sdxl_models",
            weight_name="ip-adapter_sdxl.safetensors",
            image_encoder_folder=None,
        )
        pipe.set_ip_adapter_scale(args.ip_scale)

        from transformers import CLIPImageProcessor, CLIPVisionModelWithProjection

        enc_dir = str(Path(args.ip_adapter_dir) / "sdxl_models" / "image_encoder")
        encoder = CLIPVisionModelWithProjection.from_pretrained(enc_dir, torch_dtype=torch.float16).to("cuda")
        processor = CLIPImageProcessor()
        pixels = processor(images=Image.open(args.ref).convert("RGB"), return_tensors="pt").pixel_values
        with torch.no_grad():
            pooled = encoder(pixels.to("cuda", dtype=torch.float16)).image_embeds.unsqueeze(1)
            ref_embeds = torch.cat([torch.zeros_like(pooled), pooled], dim=0)
        del encoder
        torch.cuda.empty_cache()
        print(f"IP-Adapter scale={args.ip_scale} ref={args.ref}")

    done = 0
    for item in items:
        target = out_dir / f"{item['id']}.png"
        if target.exists() and target.stat().st_size > 0:
            print(f"skip {item['id']}")
            continue

        prompt = STYLE.format(subject=item["subject"])
        generator = torch.Generator("cuda").manual_seed(args.seed + done)
        kwargs = {}
        if ref_embeds is not None:
            kwargs["ip_adapter_image_embeds"] = [ref_embeds]
        image = pipe(
            prompt=prompt,
            **kwargs,
            negative_prompt=NEGATIVE,
            width=args.width,
            height=args.height,
            num_inference_steps=args.steps,
            guidance_scale=args.cfg,
            generator=generator,
        ).images[0]
        image.save(target)
        done += 1
        print(f"[{done}/{len(items)}] {item['id']} -> {target}")


if __name__ == "__main__":
    main()
