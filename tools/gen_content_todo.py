"""Generate docs/content_todo.md — the text/design worksheet for the mod.

Pulls real Ironclad card/relic strings from the game's localization (as format
placeholders) and wraps them in editable markdown with the exact keys the mod
expects.

Run: python tools/gen_content_todo.py
"""
from __future__ import annotations

import json
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
LOC = Path(r"D:\DSH_work\ref\loc\localization")
OUT = ROOT / "docs" / "content_todo.md"

CHAR_KEY = "MADSCIENTIST-MAD_SCIENTIST"


def load(lang: str, table: str, base: Path = LOC) -> dict:
    p = base / lang / f"{table}.json"
    if not p.exists():
        return {}
    return json.loads(p.read_text(encoding="utf-8"))


def clean(text: str, limit: int = 46) -> str:
    text = " ".join(text.split())
    text = text.replace("|", "\\|")
    return text if len(text) <= limit else text[: limit - 1] + "…"


CARDS = [
    ("STRIKE_IRONCLAD", "1", "攻击", "基础", "敌人"),
    ("DEFEND_IRONCLAD", "1", "技能", "基础", "自身"),
    ("BASH", "2", "攻击", "基础", "敌人"),
    ("ANGER", "0", "攻击", "普通", "敌人"),
    ("POMMEL_STRIKE", "1", "攻击", "普通", "敌人"),
    ("TWIN_STRIKE", "1", "攻击", "普通", "敌人"),
    ("IRON_WAVE", "1", "攻击", "普通", "敌人"),
    ("SHRUG_IT_OFF", "1", "技能", "普通", "自身"),
    ("INFLAME", "1", "能力", "罕见", "自身"),
    ("UPPERCUT", "2", "攻击", "罕见", "敌人"),
    ("WHIRLWIND", "X", "攻击", "罕见", "所有敌人"),
    ("DEMON_FORM", "3", "能力", "稀有", "自身"),
]

RELICS = [
    ("BURNING_BLOOD", "初始", "战斗结束后回复生命"),
    ("ANCHOR", "普通", "战斗开始时获得格挡"),
    ("BAG_OF_PREPARATION", "普通", "战斗开始时额外抽牌"),
    ("VAJRA", "普通", "战斗开始时获得力量"),
]


def card_rows(zh: dict, en: dict) -> str:
    lines = ["| ID | 中文名 | 英文名 | 费用 | 类型 | 稀有度 | 目标 | 效果（占位） |",
             "|---|---|---|---|---|---|---|---|"]
    for cid, cost, ctype, rarity, target in CARDS:
        t_zh = zh.get(f"{cid}.title", cid)
        t_en = en.get(f"{cid}.title", "")
        d_zh = clean(zh.get(f"{cid}.description", ""))
        lines.append(f"| `{cid}` | {t_zh} | {t_en} | {cost} | {ctype} | {rarity} | {target} | {d_zh} |")
    return "\n".join(lines)


def relic_rows(zh: dict, en: dict) -> str:
    lines = ["| ID | 中文名 | 英文名 | 稀有度 | 效果（占位） |",
             "|---|---|---|---|---|"]
    for rid, rarity, _note in RELICS:
        t_zh = zh.get(f"{rid}.title", rid)
        t_en = en.get(f"{rid}.title", "")
        d_zh = clean(zh.get(f"{rid}.description", ""), 60)
        lines.append(f"| `{rid}` | {t_zh} | {t_en} | {rarity} | {d_zh} |")
    return "\n".join(lines)


BLANK_CARDS = "\n".join(
    f"| {i} |  |  |  |  |  |  |  |  |" for i in range(1, 13)
)
BLANK_RELICS = "\n".join(
    f"| {i} |  |  |  |  |  |  |" for i in range(1, 9)
)
BLANK_POWERS = "\n".join(f"| {i} |  |  |  |  |  |" for i in range(1, 5))
BLANK_POTIONS = "\n".join(f"| {i} |  |  |  |  |" for i in range(1, 4))


def main() -> None:
    zh_cards, en_cards = load("zhs", "cards"), load("eng", "cards")
    zh_relics, en_relics = load("zhs", "relics"), load("eng", "relics")
    mod_loc = ROOT / "MadScientist" / "localization"
    zh_char = load("zhs", "characters", mod_loc)
    en_char = load("eng", "characters", mod_loc)

    def c(key: str) -> str:
        return zh_char.get(f"{CHAR_KEY}.{key}", "")

    def ce(key: str) -> str:
        return en_char.get(f"{CHAR_KEY}.{key}", "")

    doc = f"""# 疯狂科学家 · 文本 / 设定填写清单

> 这是一份**给你改**的工作表：卡牌、遗物、先古之民对话、角色台词的占位与格式都在这里。
> 你直接在这个 `.md` 里改（或者只把“卡名 + 效果”发我），我负责转成游戏用的 JSON 和代码。
>
> 铁甲战士的卡/遗物只是**格式占位**，用来看字段怎么填，最终都要换成疯狂科学家的设计。

---

## 0. 命名规则速查

| 内容 | 类名示例 | 本地化 key | 图片路径 |
|---|---|---|---|
| 角色 | `MadScientist` | `{CHAR_KEY}.title` 等 | `images/charui/…` |
| 卡牌 | `ReagentFlask` | `MADSCIENTIST-REAGENT_FLASK.title` / `.description` | `images/card_portraits/reagent_flask.png`（250×190）<br>`images/card_portraits/big/reagent_flask.png`（1000×760） |
| 遗物 | `ReagentSatchel` | `MADSCIENTIST-REAGENT_SATCHEL.title` / `.description` / `.flavor` | `images/relics/reagent_satchel.png`（94×94）<br>`images/relics/big/reagent_satchel.png` |
| 药水 | `FizzingFlask` | `MADSCIENTIST-FIZZING_FLASK.title` / `.description` | `images/potions/fizzing_flask.png` |
| 能力 | `ExperimentPower` | `MADSCIENTIST-EXPERIMENT_POWER.title` / `.description` | `images/powers/experiment_power.png` |

要点：
- ID = 类名的全大写下划线形式；Mod 前缀固定 `MADSCIENTIST-`。
- 描述里的数值可以先用普通数字，我会换成 `{{Block:diff()}}`、`{{Damage:diff()}}` 这类动态变量。
- 颜色标记：`[gold]…[/gold]`、`[blue]…[/blue]`、`[red]…[/red]`；换行用 `\\n`。
- 卡图文件名 = 卡 ID 全小写。

---

## 1. 角色基础文本（当前版本，可直接改）

| 字段 | 当前中文 | 当前英文 | 说明 |
|---|---|---|---|
| title | {c("title")} | {ce("title")} | 角色名（选择界面） |
| titleObject | {c("titleObject")} | {ce("titleObject")} | 宾语形式的名字 |
| description | {clean(c("description"), 60)} | {clean(ce("description"), 60)} | 选择界面下方一句话 |
| pronounSubject | {c("pronounSubject")} | {ce("pronounSubject")} | 主格（她/she） |
| pronounObject | {c("pronounObject")} | {ce("pronounObject")} | 宾格 |
| pronounPossessive | {c("pronounPossessive")} | {ce("pronounPossessive")} | 物主代词 |
| possessiveAdjective | {c("possessiveAdjective")} | {ce("possessiveAdjective")} | 形容词性物主代词 |
| aromaPrinciple | {clean(c("aromaPrinciple"), 40)} | {clean(ce("aromaPrinciple"), 40)} | 闻香事件的台词 |
| goldMonologue | {clean(c("goldMonologue"), 40)} | {clean(ce("goldMonologue"), 40)} | 捡金币独白 |
| banter.alive.endTurnPing | {c("banter.alive.endTurnPing")} | {ce("banter.alive.endTurnPing")} | 多人模式还活着 |
| banter.dead.endTurnPing | {c("banter.dead.endTurnPing")} | {ce("banter.dead.endTurnPing")} | 多人模式阵亡 |
| eventDeathPrevention | {clean(c("eventDeathPrevention"), 40)} | {clean(ce("eventDeathPrevention"), 40)} | 事件里免死台词 |
| cardsModifierTitle | {c("cardsModifierTitle")} | {ce("cardsModifierTitle")} | 自定义模式“卡池”标题 |
| cardsModifierDescription | {clean(c("cardsModifierDescription"), 40)} | {clean(ce("cardsModifierDescription"), 40)} | 自定义模式说明 |
| bestiaryQuote | {clean(c("bestiaryQuote"), 30)} | {clean(ce("bestiaryQuote"), 30)} | 图鉴里没杀过怪时的台词 |

> 想改哪行直接改“当前中文/当前英文”两列。

---

## 2. 卡牌

### 2.1 单卡格式（示例，照着填）

**试剂瓶 Reagent Flask**（示例，不是最终设计）
- 费用 / 类型 / 稀有度：1 / 技能 / 普通
- 目标：自身
- 效果：获得 5 点格挡。消耗 1 瓶试剂：额外获得 5 点格挡。
- 升级：格挡改为 8 点。
- 卡图：`reagent_flask.png` / `big/reagent_flask.png`
- key：`MADSCIENTIST-REAGENT_FLASK.title` / `.description`

```json
"MADSCIENTIST-REAGENT_FLASK.title": "试剂瓶",
"MADSCIENTIST-REAGENT_FLASK.description": "获得 5 点[gold]格挡[/gold]。消耗 1 瓶试剂：额外获得 5 点[gold]格挡[/gold]。"
```

### 2.2 铁甲战士占位卡（只看格式，数值是原版的）

{card_rows(zh_cards, en_cards)}

### 2.3 疯狂科学家卡（待你填）

| # | 中文名 | 英文名 | 费用 | 类型 | 稀有度 | 目标 | 效果 | 升级 |
|---|---|---|---|---|---|---|---|---|
{BLANK_CARDS}

类型：攻击 / 技能 / 能力 / 状态 / 诅咒；稀有度：基础 / 普通 / 罕见 / 稀有 / 特殊。
建议数量：普通 12~15、罕见 10~12、稀有 6~8，能力牌 6~8 张。

---

## 3. 遗物

### 3.1 格式

**试剂背囊 Reagent Satchel**（示例）
- 稀有度：初始
- 触发：战斗开始时
- 效果：获得 2 瓶试剂。
- 风味：`"瓶瓶罐罐，叮当作响。"`
- key：`MADSCIENTIST-REAGENT_SATCHEL.title` / `.description` / `.flavor`

```json
"MADSCIENTIST-REAGENT_SATCHEL.title": "试剂背囊",
"MADSCIENTIST-REAGENT_SATCHEL.description": "战斗开始时，获得 2 瓶试剂。",
"MADSCIENTIST-REAGENT_SATCHEL.flavor": "瓶瓶罐罐，叮当作响。"
```

### 3.2 原版遗物占位（只看格式）

{relic_rows(zh_relics, en_relics)}

### 3.3 角色遗物 / 衍生遗物（待你填）

| # | 中文名 | 英文名 | 稀有度 | 触发时机 | 效果 | 风味文本 |
|---|---|---|---|---|---|---|
{BLANK_RELICS}

第 1 行建议填**初始遗物**（替换现在的“燃烧之血”占位），后面 7 行填角色专属遗物。
稀有度分布参考：初始 1、普通 3、罕见 2、稀有 1、商店 1。

---

## 4. 药水 / 能力（可选，先占位）

### 4.1 药水

| # | 中文名 | 英文名 | 稀有度 | 效果 |
|---|---|---|---|---|
{BLANK_POTIONS}

### 4.2 能力（卡牌产生的能力）

| # | 中文名 | 英文名 | 类型（增益/减益） | 叠加方式 | 效果 |
|---|---|---|---|---|---|
{BLANK_POWERS}

---

## 5. 先古之民对话

### 5.1 key 规则

格式：`<先古>.talk.<角色>.<段>-<行>.<说话人>`

- 先古：`NEOW` / `DARV` / `OROBAS` / `PAEL` / `TEZCATARA` / `NONUPEIPE` / `VAKUU` / `TANX` …
- 角色：`{CHAR_KEY}`
- 段-行：首访 `0-0`、`0-1`…；重复访问带 `r`（如 `1-0r`）；第三次 `2-0`
- 说话人：`.ancient`（先古说）、`.char`（角色说）、`.next`（继续按钮，可选）

放在 `MadScientist/localization/{{eng,zhs}}/ancients.json`。

### 5.2 NEOW（复活之母）

```json
"NEOW.talk.{CHAR_KEY}.0-0.ancient": "（先古台词：第一次见到她）",
"NEOW.talk.{CHAR_KEY}.0-1.char": "（角色回应）",
"NEOW.talk.{CHAR_KEY}.0-1.next": "继续",
"NEOW.talk.{CHAR_KEY}.1-0r.ancient": "（重复到访时的台词）",
"NEOW.talk.{CHAR_KEY}.2-0.ancient": "（第三次及以后的台词）"
```

### 5.3 DARV（囤积者）

```json
"DARV.talk.{CHAR_KEY}.0-0.ancient": "（先古台词）",
"DARV.talk.{CHAR_KEY}.0-1.char": "（角色回应）",
"DARV.talk.{CHAR_KEY}.1-0r.ancient": "（重复到访）"
```

### 5.4 其他先古（要不要写？）

| 先古 | 中文名（以游戏内为准） | 是否要写 | 备注 |
|---|---|---|---|
| NEOW | — | 建议写 | 每次开局都会遇到 |
| DARV | — | 可选 | 商店/事件向 |
| OROBAS | — | 可选 |  |
| PAEL | — | 可选 |  |
| TEZCATARA | — | 可选 |  |
| NONUPEIPE | — | 可选 |  |
| VAKUU | — | 可选 |  |
| TANX | — | 可选 |  |

---

## 6. 关键词 / 机制文本

如果角色有自定义关键词（比如“实验”“试剂”“过载”），每个需要一条：

```json
"EXPERIMENT.title": "实验",
"EXPERIMENT.description": "（关键词说明，出现在卡牌提示里）"
```

| 关键词 | 中文名 | 英文名 | 说明 | 状态 |
|---|---|---|---|---|
| EXPERIMENT |  |  |  | 待定 |
| REAGENT |  |  |  | 待定 |
| OVERLOAD |  |  |  | 待定 |

---

## 7. 其他可能要写的文本

| 内容 | 位置 | 说明 |
|---|---|---|
| 卡池名称/描述 | 角色 key `cardsModifierTitle` / `cardsModifierDescription` | 自定义模式里显示 |
| 图鉴击杀台词 | `{CHAR_KEY}.bestiaryKillQuote.<怪物ID>` | 每个怪物一条，量大，可选 |
| 专属事件 | `localization/*/events.json` | 需要我加事件代码，先把文本写好也行 |
| 多人模式表情/快捷语 | 待确认 | 需要的话我查 key |

---

## 8. 填完后

- 直接把改好的这个 md 发我，或者只发卡表/遗物表；
- 我负责：生成 `cards.json` / `relics.json` / `ancients.json`、写 C# 类、接卡图槽位、编译 + 无头测试；
- 数值平衡可以先粗填，我会按原版基准（打击 6 伤、防御 5 挡）给你一版初稿，再一起调。

---

### 附：卡图规格

| 用途 | 尺寸 | 说明 |
|---|---|---|
| 卡面小图 | 250×190 | 卡牌在手里的图 |
| 卡面大图 | 1000×760 | 查看卡牌时的大图（全画 606×852） |
| 遗物图标 | 94×94 | 遗物栏 |
| 遗物大图 | 256×256 左右 | 遗物详情 |
| 药水 | 256×256 | 药水栏 |
| 能力图标 | 64×64 左右 | 状态栏 |
"""

    OUT.write_text(doc, encoding="utf-8", newline="\n")
    print(f"wrote {OUT} ({len(doc)} chars)")


if __name__ == "__main__":
    main()
