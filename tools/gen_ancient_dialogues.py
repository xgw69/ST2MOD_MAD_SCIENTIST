import json
from pathlib import Path

CH = "MADSCIENTIST-MAD_SCIENTIST"

EN = {
 "NEOW": {
  "0-0.ancient": "[sine]...another... tinkerer... ...show me... what you... would... build....[/sine]",
  "0-0.next": "Talk",
  "0-1.char": "A colossal eye. Unknown biology. May I take a sample?",
  "0-1.next": "Continue",
  "0-2.ancient": "[sine]...take... what... you... need.....[/sine]",
  "1-0r.ancient": "[sine]...still... experimenting... good....[/sine]",
  "2-0.ancient": "[sine]...the... [red]Architect[/red]... awaits...[/sine]",
  "2-0.next": "Continue",
  "2-1.char": "Good. I have questions only he can answer.",
 },
 "DARV": {
  "0-0.ancient": "Buy something, or get out. Unless you have something worth trading.",
  "0-1.char": "This wrench? Prototype. It trades for a relic.",
  "0-1.next": "Continue",
  "0-2.ancient": "Hmph. Deal.",
  "1-0r.ancient": "Back for more junk?",
  "2-0.ancient": "You still owe me a potion.",
 },
 "OROBAS": {
  "0-0.ancient": "A new shape. Show me what you would become.",
  "0-1.char": "Upgrade me? I have a list.",
  "0-1.next": "Continue",
  "0-2.ancient": "Then choose what to refine.",
  "1-0r.ancient": "Your machine still hums.",
  "2-0.ancient": "The rainbow bends around your gear.",
 },
 "PAEL": {
  "0-0.ancient": "Heat. Everything melts. Even machines.",
  "0-1.char": "Melt this sample and I will learn.",
  "1-0r.ancient": "Your flask survived?",
  "2-0.ancient": "Burn brighter, little tinkerer.",
 },
 "TEZCATARA": {
  "0-0.ancient": "Feed the fire.",
  "0-1.char": "With what? Cards? Reagents? ...A colleague?",
  "1-0r.ancient": "The flame remembers you.",
  "2-0.ancient": "It is never enough.",
 },
 "NONUPEIPE": {
  "0-0.ancient": "Randomness is the only true science.",
  "0-1.char": "Finally, someone who understands my lab.",
  "1-0r.ancient": "Roll again.",
  "2-0.ancient": "The dice remember you.",
 },
 "VAKUU": {
  "0-0.ancient": "A deal. Your ethics for power.",
  "0-1.char": "My ethics are already a variable.",
  "1-0r.ancient": "The contract holds.",
  "2-0.ancient": "Sign again.",
 },
 "TANX": {
  "0-0.ancient": "You stitch things together too. Show me.",
  "0-1.char": "I fuse cards. You fuse beasts. We are colleagues.",
  "1-0r.ancient": "Show me your stitching.",
  "2-0.ancient": "The chimera approves.",
 },
}

ZH = {
 "NEOW": {
  "0-0.ancient": "[sine]……又一个……修补匠……让我看看……你会……造出什么……[/sine]",
  "0-0.next": "交谈",
  "0-1.char": "一只巨大的眼睛。未知生物结构。可以取个样本吗？",
  "0-1.next": "继续",
  "0-2.ancient": "[sine]……取走……你需要的……吧……[/sine]",
  "1-0r.ancient": "[sine]……还在……实验……很好……[/sine]",
  "2-0.ancient": "[sine]……[red]建筑师[/red]……在等你……[/sine]",
  "2-0.next": "继续",
  "2-1.char": "很好。我有一堆问题只有他能回答。",
 },
 "DARV": {
  "0-0.ancient": "买东西，或者滚出去。除非你有值得交换的东西。",
  "0-1.char": "这把扳手？原型机。换一件遗物。",
  "0-1.next": "继续",
  "0-2.ancient": "哼。成交。",
  "1-0r.ancient": "又回来淘破烂了？",
  "2-0.ancient": "你还欠我一瓶药水。",
 },
 "OROBAS": {
  "0-0.ancient": "新的形态。让我看看你想变成什么。",
  "0-1.char": "升级我？我有一张清单。",
  "0-1.next": "继续",
  "0-2.ancient": "那就选一样来精炼吧。",
  "1-0r.ancient": "你的机器还在嗡鸣。",
  "2-0.ancient": "虹光绕着你的齿轮弯折。",
 },
 "PAEL": {
  "0-0.ancient": "热。一切都会融化。连机器也是。",
  "0-1.char": "把这个样本熔了，我就能学到东西。",
  "1-0r.ancient": "你的烧瓶居然没化？",
  "2-0.ancient": "烧得更亮些吧，小小的修补匠。",
 },
 "TEZCATARA": {
  "0-0.ancient": "喂火。",
  "0-1.char": "用什么喂？卡牌？试剂？……还是一位同事？",
  "1-0r.ancient": "火焰记得你。",
  "2-0.ancient": "永远都不够。",
 },
 "NONUPEIPE": {
  "0-0.ancient": "随机，才是唯一真实的科学。",
  "0-1.char": "终于有人懂我的实验室了。",
  "1-0r.ancient": "再掷一次。",
  "2-0.ancient": "骰子记得你。",
 },
 "VAKUU": {
  "0-0.ancient": "交易。用你的伦理换力量。",
  "0-1.char": "我的伦理本来就是一个变量。",
  "1-0r.ancient": "契约仍然有效。",
  "2-0.ancient": "再签一次。",
 },
 "TANX": {
  "0-0.ancient": "你也喜欢把东西缝在一起。给我看看。",
  "0-1.char": "我融合卡牌，你融合野兽。我们算同行。",
  "1-0r.ancient": "让我看看你的缝合。",
  "2-0.ancient": "嵌合兽认可你。",
 },
}


def build(table, next_label):
    out = {}
    for ancient, lines in table.items():
        pages = {}
        for suffix, text in lines.items():
            page, speaker = suffix.rsplit(".", 1)
            pages.setdefault(page, {})[speaker] = text
        groups = {}
        for page in pages:
            groups.setdefault(page.split("-")[0], []).append(page)
        for prefix, plist in groups.items():
            order = sorted(plist, key=lambda p: int(p.split("-")[1].rstrip("r")))
            for page in plist:
                for speaker, text in pages[page].items():
                    out[f"{ancient}.talk.{CH}.{page}.{speaker}"] = text
            for page in order[:-1]:
                key = f"{ancient}.talk.{CH}.{page}.next"
                if key not in out:
                    out[key] = next_label
    return out

for lang, table in (("eng", EN), ("zhs", ZH)):
    p = Path(f"MadScientist/localization/{lang}/ancients.json")
    existing = json.loads(p.read_text(encoding="utf-8")) if p.exists() else {}
    existing = {k: v for k, v in existing.items() if CH not in k}
    existing.update(build(table, "继续" if lang == "zhs" else "Continue"))
    p.write_text(json.dumps(existing, ensure_ascii=False, indent=2) + "\n", encoding="utf-8", newline="\n")

print("ok")
