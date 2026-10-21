import json

zh = {
    "MUTATION": ("变异", "获得 6 点格挡。获得 1 点力量。下回合失去 2 点敏捷。"),
    "SCORCHED_GROUND": ("灼热地面", "接下来 3 个回合开始时，对随机敌人造成 8 点伤害。"),
    "THINK": ("思考", "下回合开始时获得 12 点格挡、2 点能量。"),
    "MECHANICAL_ASCENSION": ("机械飞升", "获得 1 层人工制品。消耗你抽牌堆、弃牌堆、手牌中所有能力牌。"),
    "REPTILIAN_EVOLUTION_PROJECT": ("爬行动物进化工程", "获得 5 层无实体。消耗你抽牌堆、弃牌堆、手牌中所有能力牌。"),
    "FORBIDDEN_SCIENCE": ("禁忌科学", "不可打出。与其他牌融合后，融合牌费用变为 0，打出时对所有敌人造成 15 点伤害。"),
    "SCIENTIFIC_RESEARCH": ("科学研究", "抽到时，复制一张到手牌。"),
    "PROCESS": ("处理", "消耗 1 张手牌，抽 1 张牌，并将其与此牌融合。"),
}
en = {
    "MUTATION": ("Mutation", "Gain 6 Block. Gain 1 Strength. Next turn, lose 2 Dexterity."),
    "SCORCHED_GROUND": ("Scorched Ground", "At the start of the next 3 turns, deal 8 damage to a random enemy."),
    "THINK": ("Think", "At the start of your next turn, gain 12 Block and 2 Energy."),
    "MECHANICAL_ASCENSION": ("Mechanical Ascension", "Gain 1 Artifact. Exhaust all Power cards in your draw pile, discard pile and hand."),
    "REPTILIAN_EVOLUTION_PROJECT": ("Reptilian Evolution Project", "Gain 5 Intangible. Exhaust all Power cards in your draw pile, discard pile and hand."),
    "FORBIDDEN_SCIENCE": ("Forbidden Science", "Unplayable. When fused, the fused card costs 0 and deals 15 damage to ALL enemies when played."),
    "SCIENTIFIC_RESEARCH": ("Scientific Research", "When drawn, add a copy of this card into your hand."),
    "PROCESS": ("Process", "Exhaust 1 card in your hand, draw 1 card, and fuse it with this card."),
}

powers_zh = {
    "MUTATION_POWER": ("变异", "下回合开始时失去敏捷。"),
    "SCORCHED_GROUND_POWER": ("灼热地面", "接下来 {Amount} 个回合开始时，对随机敌人造成 8 点伤害。"),
    "THINK_POWER": ("思考", "下回合开始时获得格挡和能量。"),
}
powers_en = {
    "MUTATION_POWER": ("Mutation", "Next turn, lose Dexterity."),
    "SCORCHED_GROUND_POWER": ("Scorched Ground", "At the start of the next {Amount} turns, deal 8 damage to a random enemy."),
    "THINK_POWER": ("Think", "At the start of your next turn, gain Block and Energy."),
}

for lang, adds in (("zhs", zh), ("eng", en)):
    p = f"MadScientist/localization/{lang}/cards.json"
    d = json.load(open(p, encoding="utf-8"))
    for k, (t, desc) in adds.items():
        d[f"MADSCIENTIST-{k}.title"] = t
        d[f"MADSCIENTIST-{k}.description"] = desc
    open(p, "w", encoding="utf-8", newline="\n").write(json.dumps(d, ensure_ascii=False, indent=2) + "\n")

for lang, adds in (("zhs", powers_zh), ("eng", powers_en)):
    p = f"MadScientist/localization/{lang}/powers.json"
    d = json.load(open(p, encoding="utf-8"))
    for k, (t, desc) in adds.items():
        d[f"MADSCIENTIST-{k}.title"] = t
        d[f"MADSCIENTIST-{k}.description"] = desc
    open(p, "w", encoding="utf-8", newline="\n").write(json.dumps(d, ensure_ascii=False, indent=2) + "\n")

# exhaust prompt used by Process
for lang, prompt in (("zhs", "选择要消耗的牌"), ("eng", "Choose a card to exhaust")):
    p = f"MadScientist/localization/{lang}/card_selection.json"
    d = json.load(open(p, encoding="utf-8"))
    d["MADSCIENTIST-EXHAUST_PROMPT"] = prompt
    open(p, "w", encoding="utf-8", newline="\n").write(json.dumps(d, ensure_ascii=False, indent=2) + "\n")

print("ok")
