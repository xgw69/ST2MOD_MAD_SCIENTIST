# 测试待办：卡牌与机制 bug 全面测试

当前版本：public-beta（v0.111.0），依赖 BaseLib 3.4.7，工坊 id `3824548296`。

## 每张卡的测试维度

对下面每一张卡至少检查：

- [ ] 打出：费用、目标、数值、关键词是否正确
- [ ] 升级：升级后数值/费用/关键词/描述是否变化，描述是否与实际一致
- [ ] 复制/融合：被「融合」「科学研究」等复制或作为材料时是否正常
- [ ] 多人：主机/客户端结果是否一致（重点：随机、生成卡、选牌）
- [ ] 存档：战斗结束、读档后是否残留错误状态（如临时费用、临时能力）

## 机制专项

- [ ] **融合**：费用相加、X 只由 X 材料提供、关键词并集、材料从手牌消失无残影、融合卡描述按顺序列出效果
- [ ] **机器耐久**：0~20 显示、透支扣血为 2 倍、爆炸后锁定、每场战斗重置
- [ ] **修复**：修复量、上限、满耐久时是否浪费
- [ ] **疯狂科学**：9 张 3 类 3 种变体，复制/融合后 `Owner` 是否正确
- [ ] **疯狂科学家（9 选 3）**：能否取消、少选、重复选，获得 3(4) 能量
- [ ] **X 费牌**（X射线、爆破装置）：X=0/1/3、升级后 X+1、描述是否显示 `X` 而不是 `X(X+1)`
- [ ] **格挡封锁**：X射线后卡牌来源的格挡是否归零，遗物/能力格挡是否仍生效，回合数是否正确
- [ ] **库存**：玩家回合开始触发一次、弃牌堆为空、选择 0 张、多人下选牌
- [ ] **丧失理智**：升级后只给脆弱+易伤、消耗是否正确
- [ ] **灵魂研究**：致死触发、最大生命 -5/-3、回复到一半、每场只触发一次
- [ ] **伦理边界**：抽牌堆 +1 愧疚、牌组 +1 愧疚、多人
- [ ] **科学研究**：抽到复制、打出格挡 6/7、修复 1/2
- [ ] **爆破装置**：X(X+1) 张爆破、本回合爆破 0 费、跨回合是否恢复
- [ ] **先古对话**：八位先古首次/重复/后续对话，右下角是否出现原始 key（如出现请截图）
- [ ] **休息处/商店**：角色贴图大小、位置、透明边缘
- [ ] **多人猜拳手**：石头/布/剪刀/指人贴图

## 全卡清单（63 张）

- [ ] **X射线** / X-Ray
- [ ] **丧失理智** / Loss of Sanity
- [ ] **伦理边界** / Ethical Boundary
- [ ] **伪科学方法** / Pseudoscientific Method
- [ ] **全能修正** / Universal Correction
- [ ] **全自动运行** / Fully Automatic
- [ ] **力量研究** / Strength Research
- [ ] **化学雾气** / Chemical Fog
- [ ] **反射研究** / Reflex Research
- [ ] **反应锅炉** / Reactor Boiler
- [ ] **变异** / Mutation
- [ ] **处理** / Process
- [ ] **备用尖刺** / Spare Spikes
- [ ] **备用药剂** / Spare Reagent
- [ ] **大融合** / Great Fusion
- [ ] **完全修复** / Full Repair
- [ ] **巨型扳手** / Giant Wrench
- [ ] **库存** / Inventory
- [ ] **快速修复** / Quick Repair
- [ ] **思考** / Think
- [ ] **打击** / Strike
- [ ] **执迷不误** / Obsession
- [ ] **扳手打击** / Wrench Strike
- [ ] **攻击组件** / Attack Module
- [ ] **敏捷研究** / Dexterity Research
- [ ] **敲击** / Knock
- [ ] **机器控制** / Machine Control
- [ ] **机器无限研究** / Infinite Machine Research
- [ ] **机器爆破** / Machine Blast
- [ ] **机器生产** / Machine Production
- [ ] **机器药剂** / Machine Reagent
- [ ] **机器轰鸣** / Machine Rumble
- [ ] **机器防御** / Machine Defense
- [ ] **机械飞升** / Mechanical Ascension
- [ ] **混合药剂** / Mixed Reagent
- [ ] **灵魂研究** / Soul Research
- [ ] **灼热地面** / Scorched Ground
- [ ] **炼制** / Refine
- [ ] **爆破** / Blast
- [ ] **爆破装置** / Blasting Device
- [ ] **爬行动物进化工程** / Reptilian Evolution Project
- [ ] **疯狂禁忌** / Forbidden Frenzy
- [ ] **疯狂科学家** / Mad Scientist
- [ ] **疯狂科研** / Mad Research
- [ ] **禁忌科学** / Forbidden Science
- [ ] **科学传承** / Scientific Legacy
- [ ] **科学方法** / Scientific Method
- [ ] **科学研究** / Scientific Research
- [ ] **空间撕裂** / Spatial Tear
- [ ] **突击** / Assault
- [ ] **组装** / Assemble
- [ ] **能量护盾** / Energy Shield
- [ ] **能量组件** / Energy Module
- [ ] **自修复研究** / Self-Repair Research
- [ ] **自修复计划** / Self-Repair Plan
- [ ] **自我审视** / Self-Review
- [ ] **良化变异** / Beneficial Mutation
- [ ] **计划** / Plan
- [ ] **轰鸣** / Rumble
- [ ] **速度组件** / Speed Module
- [ ] **防御** / Defend
- [ ] **防御组件** / Defense Module
- [ ] **震荡** / Jolt

## 已知注意点

- 工坊版与本地 `mods` 版不要同时启用，来源不一致会导致多人分歧
- 测试多人时两端必须是同一版本
- 日志位置：`%APPDATA%\SlayTheSpire2\logs\godot.log`
