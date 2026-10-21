import shutil
import sys
from pathlib import Path

from gradio_client import Client, handle_file

ROOT = Path(__file__).resolve().parent.parent
SPACE = "https://black-forest-labs-flux-1-kontext-dev.hf.space"

ref = str(ROOT / "art" / "ref" / "ip_ref_combat.png")
prompt = (
    "Change the pose: the character swings a large wrench overhead in an attack pose. "
    "Keep the exact same character design, face, colorful hair, goggles, white coat and backpack. "
    "Dark cave laboratory background, green sparks, Slay the Spire card art style."
)

client = Client(SPACE, verbose=False)
print("connected")
result = client.predict(
    handle_file(ref),
    prompt,
    0,
    True,
    2.5,
    28,
    api_name="/infer",
)
print(result)
path = result[0] if isinstance(result, (list, tuple)) else result
if isinstance(path, dict):
    path = path.get("path")
out = ROOT / "art" / "kontext_out"
out.mkdir(parents=True, exist_ok=True)
if path:
    shutil.copy(path, out / "strike_kontext.png")
    print("saved", out / "strike_kontext.png")
