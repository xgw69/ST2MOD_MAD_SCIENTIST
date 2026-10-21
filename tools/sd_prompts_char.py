import json
from pathlib import Path

CHAR = "a pale green-skinned mad scientist with wild white hair, round goggles, white lab coat and green tie"

# ids that feature the character
CHAR_IDS = {
    "strike", "defend", "wrench_strike", "assault", "mixed_reagent", "knock", "giant_wrench",
    "forbidden_frenzy", "universal_correction", "machine_control", "machine_production",
    "energy_shield", "chemical_fog", "obsession", "mutation", "beneficial_mutation",
    "self_review", "ethical_boundary", "loss_of_sanity", "scientific_method",
    "scientific_legacy", "great_fusion", "process", "pseudoscientific_method",
    "plan", "think", "mad_scientist_character_card", "strength_research",
    "dexterity_research", "reflex_research", "self_repair_research",
    "infinite_machine_research", "fully_automatic", "machine_reagent", "quick_repair",
    "full_repair", "self_repair_plan", "machine_defense", "reactive_boiler",
}

p = Path("art/sd_prompts/cards.json")
items = json.loads(p.read_text(encoding="utf-8"))
for item in items:
    if item["id"] in CHAR_IDS and not item["subject"].startswith(CHAR):
        item["subject"] = f"{CHAR}, {item['subject']}"
p.write_text(json.dumps(items, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print("updated", len(items))
