# 疯狂科学家 · 文本 / 设定填写清单

> 这是一份**给你改**的工作表：卡牌、遗物、先古之民对话、角色台词的占位与格式都在这里。
> 你直接在这个 `.md` 里改（或者只把“卡名 + 效果”发我），我负责转成游戏用的 JSON 和代码。
>
> 铁甲战士的卡/遗物只是**格式占位**，用来看字段怎么填，最终都要换成疯狂科学家的设计。

---

## 0. 命名规则速查

| 内容 | 类名示例 | 本地化 key | 图片路径 |
|---|---|---|---|
| 角色 | `MadScientist` | `MADSCIENTIST-MAD_SCIENTIST.title` 等 | `images/charui/…` |
| 卡牌 | `ReagentFlask` | `MADSCIENTIST-REAGENT_FLASK.title` / `.description` | `images/card_portraits/reagent_flask.png`（250×190）<br>`images/card_portraits/big/reagent_flask.png`（1000×760） |
| 遗物 | `ReagentSatchel` | `MADSCIENTIST-REAGENT_SATCHEL.title` / `.description` / `.flavor` | `images/relics/reagent_satchel.png`（94×94）<br>`images/relics/big/reagent_satchel.png` |
| 药水 | `FizzingFlask` | `MADSCIENTIST-FIZZING_FLASK.title` / `.description` | `images/potions/fizzing_flask.png` |
| 能力 | `ExperimentPower` | `MADSCIENTIST-EXPERIMENT_POWER.title` / `.description` | `images/powers/experiment_power.png` |

要点：
- ID = 类名的全大写下划线形式；Mod 前缀固定 `MADSCIENTIST-`。
- 描述里的数值可以先用普通数字，我会换成 `{Block:diff()}`、`{Damage:diff()}` 这类动态变量。
- 颜色标记：`[gold]…[/gold]`、`[blue]…[/blue]`、`[red]…[/red]`；换行用 `\n`。
- 卡图文件名 = 卡 ID 全小写。

---

## 1. 角色基础文本（当前版本，可直接改）

| 字段 | 当前中文 | 当前英文 | 说明 |
|---|---|---|---|
| title | 疯狂科学家 | The Mad Scientist | 角色名（选择界面） |
| titleObject | 疯狂科学家 | The Mad Scientist | 宾语形式的名字 |
| description | 在尖塔的废料堆里翻找材料的修补匠。 把破烂、浓烟和来路不明的化学变成力量。 | A tinkerer who scavenges the Spire's scrap heaps. Turns jun… | 选择界面下方一句话 |
| pronounSubject | 她 | she | 主格（她/she） |
| pronounObject | 她 | her | 宾格 |
| pronounPossessive | 她的 | hers | 物主代词 |
| possessiveAdjective | 她的 | her | 形容词性物主代词 |
| aromaPrinciple | [sine][green]嗅……嗯，这是很有潜力的试剂。[/green][/s… | [sine][green]Sniff... yes, that is a pr… | 闻香事件的台词 |
| goldMonologue | [sine]哦，亮闪闪的。能换不少试剂。[/sine] | [sine]Ooh, shiny. That will fetch a few… | 捡金币独白 |
| banter.alive.endTurnPing | 马上……就好…… | Almost... done... | 多人模式还活着 |
| banter.dead.endTurnPing | …… | ... | 多人模式阵亡 |
| eventDeathPrevention | 还不行——我的实验还没做完。 | Not yet - I have experiments to finish. | 事件里免死台词 |
| cardsModifierTitle | 疯狂科学家卡牌 | Mad Scientist Cards | 自定义模式“卡池”标题 |
| cardsModifierDescription | 疯狂科学家的卡牌现在会出现在奖励和商店中。 | Mad Scientist cards will now appear in … | 自定义模式说明 |
| bestiaryQuote | 真有意思。我得取个样本。 | Fascinating. I must take a sa… | 图鉴里没杀过怪时的台词 |

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

| ID | 中文名 | 英文名 | 费用 | 类型 | 稀有度 | 目标 | 效果（占位） |
|---|---|---|---|---|---|---|---|
| `STRIKE_IRONCLAD` | 打击 | Strike | 1 | 攻击 | 基础 | 敌人 | 造成{Damage:diff()}点伤害。 |
| `DEFEND_IRONCLAD` | 防御 | Defend | 1 | 技能 | 基础 | 自身 | 获得{Block:diff()}点[gold]格挡[/gold]。 |
| `BASH` | 痛击 | Bash | 2 | 攻击 | 基础 | 敌人 | 造成{Damage:diff()}点伤害。 给予{VulnerablePower:diff… |
| `ANGER` | 愤怒 | Anger | 0 | 攻击 | 普通 | 敌人 | 造成{Damage:diff()}点伤害。 将一张此牌的复制品加入你的[gold]弃牌堆[… |
| `POMMEL_STRIKE` | 剑柄打击 | Pommel Strike | 1 | 攻击 | 普通 | 敌人 | 造成{Damage:diff()}点伤害。 抽{Cards:diff()}张牌。 |
| `TWIN_STRIKE` | 双重打击 | Twin Strike | 1 | 攻击 | 普通 | 敌人 | 造成{Damage:diff()}点伤害两次。 |
| `IRON_WAVE` | 铁斩波 | Iron Wave | 1 | 攻击 | 普通 | 敌人 | 获得{Block:diff()}点[gold]格挡[/gold]。 造成{Damage:d… |
| `SHRUG_IT_OFF` | 耸肩无视 | Shrug It Off | 1 | 技能 | 普通 | 自身 | 获得{Block:diff()}点[gold]格挡[/gold]。 抽{Cards:dif… |
| `INFLAME` | 燃烧 | Inflame | 1 | 能力 | 罕见 | 自身 | 获得{StrengthPower:diff()}点[gold]力量[/gold]。 |
| `UPPERCUT` | 上勾拳 | Uppercut | 2 | 攻击 | 罕见 | 敌人 | 造成{Damage:diff()}点伤害。 给予{Power:diff()}层[gold]… |
| `WHIRLWIND` | 旋风斩 | Whirlwind | X | 攻击 | 罕见 | 所有敌人 | 对所有敌人造成{Damage:diff()}点伤害X次。 |
| `DEMON_FORM` | 恶魔形态 | Demon Form | 3 | 能力 | 稀有 | 自身 | 在你的回合开始时，获得{StrengthPower:diff()}点[gold]力量[/g… |

### 2.3 疯狂科学家卡（待你填）

| # | 中文名 | 英文名 | 费用 | 类型 | 稀有度 | 目标 | 效果 | 升级 |
|---|---|---|---|---|---|---|---|---|
| 1 |  |  |  |  |  |  |  |  |
| 2 |  |  |  |  |  |  |  |  |
| 3 |  |  |  |  |  |  |  |  |
| 4 |  |  |  |  |  |  |  |  |
| 5 |  |  |  |  |  |  |  |  |
| 6 |  |  |  |  |  |  |  |  |
| 7 |  |  |  |  |  |  |  |  |
| 8 |  |  |  |  |  |  |  |  |
| 9 |  |  |  |  |  |  |  |  |
| 10 |  |  |  |  |  |  |  |  |
| 11 |  |  |  |  |  |  |  |  |
| 12 |  |  |  |  |  |  |  |  |

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

| ID | 中文名 | 英文名 | 稀有度 | 效果（占位） |
|---|---|---|---|---|
| `BURNING_BLOOD` | 燃烧之血 | Burning Blood | 初始 | 在战斗结束时，回复[green]{Heal}[/green]点生命。 |
| `ANCHOR` | 锚 | Anchor | 普通 | 每场战斗开始时获得[blue]{Block}[/blue]点[gold]格挡[/gold]。 |
| `BAG_OF_PREPARATION` | 准备背包 | Bag of Preparation | 普通 | 在每场战斗开始时，额外抽[blue]{Cards}[/blue]张牌。 |
| `VAJRA` | 金刚杵 | Vajra | 普通 | 在每场战斗开始时，获得[blue]{StrengthPower}[/blue]点[gold]力量[/gold]。 |

### 3.3 角色遗物 / 衍生遗物（待你填）

| # | 中文名 | 英文名 | 稀有度 | 触发时机 | 效果 | 风味文本 |
|---|---|---|---|---|---|---|
| 1 |  |  |  |  |  |  |
| 2 |  |  |  |  |  |  |
| 3 |  |  |  |  |  |  |
| 4 |  |  |  |  |  |  |
| 5 |  |  |  |  |  |  |
| 6 |  |  |  |  |  |  |
| 7 |  |  |  |  |  |  |
| 8 |  |  |  |  |  |  |

第 1 行建议填**初始遗物**（替换现在的“燃烧之血”占位），后面 7 行填角色专属遗物。
稀有度分布参考：初始 1、普通 3、罕见 2、稀有 1、商店 1。

---

## 4. 药水 / 能力（可选，先占位）

### 4.1 药水

| # | 中文名 | 英文名 | 稀有度 | 效果 |
|---|---|---|---|---|
| 1 |  |  |  |  |
| 2 |  |  |  |  |
| 3 |  |  |  |  |

### 4.2 能力（卡牌产生的能力）

| # | 中文名 | 英文名 | 类型（增益/减益） | 叠加方式 | 效果 |
|---|---|---|---|---|---|
| 1 |  |  |  |  |  |
| 2 |  |  |  |  |  |
| 3 |  |  |  |  |  |
| 4 |  |  |  |  |  |

---

## 5. 先古之民对话

### 5.1 key 规则

格式：`<先古>.talk.<角色>.<段>-<行>.<说话人>`

- 先古：`NEOW` / `DARV` / `OROBAS` / `PAEL` / `TEZCATARA` / `NONUPEIPE` / `VAKUU` / `TANX` …
- 角色：`MADSCIENTIST-MAD_SCIENTIST`
- 段-行：首访 `0-0`、`0-1`…；重复访问带 `r`（如 `1-0r`）；第三次 `2-0`
- 说话人：`.ancient`（先古说）、`.char`（角色说）、`.next`（继续按钮，可选）

放在 `MadScientist/localization/{eng,zhs}/ancients.json`。

### 5.2 NEOW（复活之母）

```json
"NEOW.talk.MADSCIENTIST-MAD_SCIENTIST.0-0.ancient": "（先古台词：第一次见到她）",
"NEOW.talk.MADSCIENTIST-MAD_SCIENTIST.0-1.char": "（角色回应）",
"NEOW.talk.MADSCIENTIST-MAD_SCIENTIST.0-1.next": "继续",
"NEOW.talk.MADSCIENTIST-MAD_SCIENTIST.1-0r.ancient": "（重复到访时的台词）",
"NEOW.talk.MADSCIENTIST-MAD_SCIENTIST.2-0.ancient": "（第三次及以后的台词）"
```

### 5.3 DARV（囤积者）

```json
"DARV.talk.MADSCIENTIST-MAD_SCIENTIST.0-0.ancient": "（先古台词）",
"DARV.talk.MADSCIENTIST-MAD_SCIENTIST.0-1.char": "（角色回应）",
"DARV.talk.MADSCIENTIST-MAD_SCIENTIST.1-0r.ancient": "（重复到访）"
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
| 图鉴击杀台词 | `MADSCIENTIST-MAD_SCIENTIST.bestiaryKillQuote.<怪物ID>` | 每个怪物一条，量大，可选 |
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
