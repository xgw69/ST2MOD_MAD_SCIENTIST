# 实现计划（疯狂科学家）

> 状态：设计已定稿（`docs/cards_design.md`），开始按阶段实现。
> 目标：先跑通「机器耐久 → 普通卡 → 融合 → 特殊卡 → 遗物/先古」，每阶段可独立进游戏验证。

---

## 1. 系统总览

| 系统 | 实现方式 | 依赖 |
|---|---|---|
| 机器耐久 / 修复 | 自定义 Power（Buff，层数 0~20）+ 静态助手 `Durability` | 无 |
| 爆炸 | 耐久 < 0 时：伤害 = 2×透支、耐久归零、挂 `MachineBrokenPower`（锁定） | 耐久系统 |
| 融合 | 自定义 `FusedCard : CardModel`（战斗内临时卡，不进牌组） | 反射调用母卡 `OnPlay` |
| 组件牌 | 不可打出的卡，融合时把效果并入 | 融合 |
| 疯狂科学 | 引用原版 `MadScience`（攻/技/能 + 9 词条组合） | 原版卡 |
| 执迷不误 | 覆写 `ShouldPlay`（参考 `Enthralled`） | 无 |
| 全自动运行 | `AfterAutoPrePlayPhaseEnteredEarly`（参考 `Bombardment`） | 无 |
| 丧失理智 | 卡牌奖励删除 1 张（参考 `ForbiddenGrimoire` 的奖励流程） | 奖励系统 |
| 进化 | 欧洛巴斯（OROBAS）选项池 3 的自定义选项 | 先古选项 |

---

## 2. 阶段 1：耐久 + 基础卡

### 文件

```
MadScientistCode/
├─ Powers/MachineDurabilityPower.cs     机器耐久（Buff，0~20）
├─ Powers/MachineBrokenPower.cs         机器损毁（锁定本场）
├─ Utils/Durability.cs                  Repair / Consume / Explode 助手
├─ Cards/Strike.cs  Defend.cs  WrenchStrike.cs
├─ Cards/Jolt.cs  Assault.cs  FullRepair.cs
├─ Cards/MachineDefense.cs  SpareReagent.cs  ...
└─ localization/{eng,zhs}/powers.json + cards.json
```

### 关键点

- `Durability.Repair(ctx, card, n)`：`PowerCmd.Apply<MachineDurabilityPower>`，封顶 20
- `Durability.Consume(ctx, card, n)`：
  - 有 `MachineBrokenPower` → 直接返回 false（不可打出）
  - `amount < n` → 透支 `n - amount`，造成 2×透支 伤害，耐久归零，挂损毁，返回 false
  - 否则扣 n
- 卡的 `CanPlay` 里检查损毁状态（消耗耐久的牌）
- 显示：Power 的 description 写明“0~20，负数爆炸”，层数即当前耐久

### 验收

- 无头日志能加载全部新卡
- 进游戏打一张「扳手打击」：造成伤害 + 耐久 +1
- 打「机器防御」：格挡 +5 耐久消耗；透支时爆炸受伤

---

## 3. 阶段 2：融合（核心）

参考 `CardFusion.FusedCard`（workshop 3747615796，已反编译到 `ref/fusedcard.cs`），但规则不同：

### 3.1 数据模型

```csharp
public sealed class FusedCard : CardModel
{
    private readonly List<CardModel> _sources;   // 母卡（战斗内临时，不序列化）
    public override string Title => string.Join("+", _sources.Select(c => c.Title));
    public override string PortraitPath => _sources[0].PortraitPath; // 或其他
    protected override int CanonicalEnergyCost => 费用和（含 X 规则）;
    protected override async Task OnPlay(...)
    {
        foreach (var src in _sources)
            await PlaySource(src, ctx, cardPlay);   // 反射调用 src 的 OnPlay
    }
}
```

### 3.2 反射调用母卡效果

- `OnPlay` 是 `protected`，用 `AccessTools.Method(typeof(CardModel), "OnPlay")` 或缓存的 `MethodInfo` 在母卡实例上调用
- 母卡需要 `Owner`/`Pile` 等上下文；CardFusion 的做法是 `ClonePreservingMutability` 后调用
- 单体效果：统一使用融合牌选中的目标；目标死亡后跳过（空打）

### 3.3 费用

- 普通：母卡费用之和
- 含 X：融合牌 `CostsX`，打出时能量清零；只有 X 母卡读到 X，其余母卡只结算一次（通过设置母卡 `EnergyCost` 或传参控制）

### 3.4 名称 / 卡图

- 名称：`A+B`（超过 3 张显示 `A+…`）
- 卡图：优先第一张母卡，其次融合底图（`images/card_portraits/fusion.png`）
- 描述：拼贴母卡描述（先用纯文本，后续做富文本）

### 3.5 融合入口

| 卡 | 行为 |
|---|---|
| 科学方法 | 选 2 张手牌 → 融合 |
| 大融合 | 全部手牌 → 融合 → 再结算一次 |
| 组装 | 抽牌堆 + 手牌的不可打出牌 → 融合 |
| 处理 | 消耗 1 张手牌 → 抽 1 → 与自身融合 |

---

## 4. 阶段 3：特殊卡

- 疯狂科学 9 张（引用原版 `MadScience`，按 6.1 组合配置数值）
- 执迷不误（`ShouldPlay` 拦截其他技能）
- 全自动运行（消耗堆自动打出）
- 丧失理智（奖励删 1，跨局永久：写 profile 级存档字段）
- 爆破装置（名字含"爆破"→ 修复 5，中英文双匹配）
- 灵魂研究（致死替代）
- 空间撕裂 / 全能修正 / 疯狂禁忌 等条件卡

## 5. 阶段 4：遗物 / 进化 / 先古

- 科学宝典 / 觉醒宝典（回合开始修复、回合结束格挡/伤害）
- 欧洛巴斯选项池 3：新增"升级初始遗物"或"升级初始卡"选项
  - 需要研究 `AncientOption` / `OROBAS` 的选项注册方式（BaseLib 的 `CustomAncientModel` 只覆盖新先古）
- 进化后的卡：伪科学方法（融合至多 4 张）

---

## 6. 风险

| 风险 | 影响 | 处理 |
|---|---|---|
| 融合反射调用母卡 `OnPlay` | 高（核心） | 先做 2 张牌的原型（打击 + 防御），验证效果/目标/动画 |
| 融合牌费用与 X | 中 | 参考 CardFusion 的 `CardEnergyCost` 字段注入，但只做战斗内 |
| 先古选项注入 | 中 | 先反编译 OROBAS 的选项定义，找可扩展点 |
| 跨局删除卡牌 | 中 | 需要 profile 存档字段，先做本局版本 |
| 63 张卡数值平衡 | 低 | 先按文档数值，跑通后统一调 |
