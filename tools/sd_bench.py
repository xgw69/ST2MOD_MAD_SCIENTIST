import os
import time

import torch

os.environ["HF_HUB_OFFLINE"] = "1"
os.environ["TRANSFORMERS_OFFLINE"] = "1"

from diffusers import StableDiffusionXLPipeline

pipe = StableDiffusionXLPipeline.from_single_file(
    r"D:\DSH_work\tools\sd\sd_xl_base_1.0.safetensors",
    torch_dtype=torch.float16,
    use_safetensors=True,
    config=r"D:\DSH_work\tools\sd\sdxl_cfg",
)
pipe.to("cuda")
print("device", pipe.unet.device, "vram", round(torch.cuda.memory_allocated() / 1e9, 2), "GB")

for steps in (3, 10):
    t = time.time()
    pipe("a cat", num_inference_steps=steps, guidance_scale=6.0, width=832, height=640)
    print(steps, "steps", round(time.time() - t, 1), "s")
