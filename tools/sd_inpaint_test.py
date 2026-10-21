import os

import torch
from PIL import Image

os.environ["HF_HUB_OFFLINE"] = "1"
os.environ["TRANSFORMERS_OFFLINE"] = "1"

from diffusers import StableDiffusionXLInpaintPipeline

pipe = StableDiffusionXLInpaintPipeline.from_single_file(
    r"D:\DSH_work\tools\sd\sd_xl_base_1.0.safetensors",
    torch_dtype=torch.float16,
    use_safetensors=True,
    config=r"D:\DSH_work\tools\sd\sdxl_cfg",
)
pipe.enable_model_cpu_offload()
pipe.enable_vae_slicing()

img = Image.open(r"D:\DSH_work\StS2_MadScientist\art\sd_in\strike.png").convert("RGB")
# mask: repaint everything except the character box on the right
mask = Image.new("L", img.size, 255)
mask.paste(0, (470, 60, 832, 640))

out = pipe(
    prompt="green chemical explosion, sparks, dark cave laboratory, slay the spire style",
    negative_prompt="text, watermark, low quality",
    image=img,
    mask_image=mask,
    num_inference_steps=25,
    guidance_scale=6.0,
    strength=1.0,
    generator=torch.Generator("cuda").manual_seed(1),
).images[0]
out.save(r"D:\DSH_work\StS2_MadScientist\art\sd_out\inpaint_test.png")
print("ok")
