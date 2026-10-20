# 疯狂科学家 / The Mad Scientist

《杀戮尖塔 2》角色 Mod 的工作区。角色基于原版第三层事件 **Tinker Time（修补时间）** 里那位疯狂科学家的形象，
角色立绘、头像、地图标记和战斗形象都直接从该事件的官方插画裁切、抠像、重绘而来。

当前版本是 **v0.1.0 框架版**：角色已经能注册进游戏、能在角色选择界面出现、能开局；
卡牌 / 遗物 / 初始牌组等内容等你把设计给我之后再逐个加进去。

---

## 1. 目录结构

```
StS2_MadScientist/
├─ MadScientist.csproj          项目文件（net9.0 + Godot.NET.Sdk 4.5.1 + BaseLib）
├─ MadScientist.json            Mod 清单（id / 名称 / 版本 / 依赖）
├─ Directory.Build.props.example  Godot 路径示例（复制成 Directory.Build.props 用，只有 publish 才需要）
├─ Sts2PathDiscovery.props      自动定位游戏安装目录
├─ project.godot                Godot 工程配置
├─ MadScientist/                ← 会被打包进 .pck 的资源目录
│  ├─ images/
│  │  ├─ character/combat.png              战斗形象（静态图，426×560）
│  │  ├─ charui/char_select_*.png          角色选择立绘 132×195（含 locked 剪影）
│  │  ├─ charui/character_icon_*.png       顶栏/统计头像 88×88（含白色描边版）
│  │  ├─ charui/map_marker_*.png           地图标记 49×64
│  │  ├─ charui/big_energy.png             能量球 74×74
│  │  ├─ card_portraits/…                  卡图占位（普通 250×190 / 全画 1000×760）
│  │  ├─ powers/ relics/ potions/          能力、遗物、药水图标占位
│  │  └─ mod_image.png                     Mod 列表缩略图 420×420
│  └─ localization/{eng,zhs}/characters.json   角色文本（名称、描述、台词、代词）
├─ MadScientistCode/
│  ├─ Character/MadScientist.cs            角色本体（血量、初始牌组、卡池、美术路径）
│  ├─ Character/MadScientistCardPool.cs    卡池（卡背 HSV 酸绿色）
│  ├─ Character/MadScientistRelicPool.cs
│  ├─ Character/MadScientistPotionPool.cs
│  ├─ Cards/ MadScientistCard.cs           卡的基类（卡图路径约定）
│  ├─ Relics/ Potions/ Powers/             遗物/药水/能力的基类
│  ├─ Extensions/StringExtensions.cs       资源路径工具
│  └─ MainFile.cs                          入口（Harmony PatchAll）
├─ art/build_art.py             美术生成脚本（从原版事件图重新生成上面所有图片）
└─ docs/design.md               角色设计 / 后续规划笔记
```

## 2. 构建环境

这台机器上已经装好（都在 `D:\DSH_work` 下，不影响系统环境变量）：

| 工具 | 位置 | 说明 |
|---|---|---|
| .NET SDK 9.0.100 | `D:\DSH_work\.dotnet\dotnet.exe` | 编译 C# |
| ilspycmd | `D:\DSH_work\tools\ilspy\ilspycmd.exe` | 反编译游戏程序集查 API |
| PckPacker | NuGet 包 `BSchneppe.StS2.PckPacker` | **不需要 Godot 编辑器**就能生成 `.pck` |

> 原版模板要求装 MegaDot（577 MB 的 Godot 分支）才能打包资源。这里改用 PckPacker，
> 每次 `dotnet build` 直接把 `MadScientist/` 打包成 `MadScientist.pck`，省掉整个 Godot 依赖。
> 代价：**不能打包 `.tscn` 场景**（休息处/商人立绘、自定义能量计数器、角色选择背景这类需要场景的资源暂时沿用原版占位）。

## 3. 编译 / 安装 / 测试

```powershell
cd D:\DSH_work\StS2_MadScientist
D:\DSH_work\.dotnet\dotnet.exe build -c Release
```

构建会自动把 `MadScientist.dll` / `.pdb` / `.json` / `.pck` 复制到
`D:\steam\steamapps\common\Slay the Spire 2\mods\MadScientist\`。

然后启动游戏（从 Steam 启动，或直接运行 exe；直接运行需要游戏目录里有 `steam_appid.txt`，已创建），
在 主菜单 → 设置 → Mod 设置 里确认 “疯狂科学家 / Mad Scientist” 已加载，再从角色选择里选她。

**改了文字/图片一定要重新 build**（会重新打 pck），只改 `.cs` 的话 build 也够了。

## 4. 美术是怎么来的

`art/build_art.py` 的流程：

1. 从游戏 `SlayTheSpire2.pck` 里解出原版事件插画 `images/events/tinker_time.png`（3440×1616）；
2. 裁出疯狂科学家所在区域（580×745）；
3. 用手工描的躯干/手臂多边形 + 与中值滤波背景的色差做抠像，边缘按亮度裁剪，
   指尖、发丝这些细节保留，底部衣摆渐隐成阴影；
4. 输出成 4 种尺寸（立绘 / 头像 / 地图标记 / 战斗形象）+ 白色描边 + locked 剪影 + Mod 缩略图。

重新生成：

```powershell
python art\build_art.py
D:\DSH_work\.dotnet\dotnet.exe build -c Release
```

改立绘构图就改脚本里的 `CROP` / `BODY_POLY` / `ARM_POLY` / 各个 `char.crop(...)` 参数。

## 5. 命名与本地化约定

- Mod id：`MadScientist`；角色类名 `MadScientist`。
- **ModelId 是 BaseLib 自动加前缀的**：`CHARACTER.MADSCIENTIST-MAD_SCIENTIST`
  （`<ModId 大写>-<类名 Slug>`）。编写代码时可以用 `StringExtensions.RemovePrefix()` 去掉前缀，
  例如卡图路径就是 `MAD_SCIENTIST` → `mad_scientist.png`。
- 本地化 key 前缀 = ModelId 的 Entry：`MADSCIENTIST-MAD_SCIENTIST.`，必需字段：
  `title`、`titleObject`、`description`、`pronounObject`、`pronounSubject`、`pronounPossessive`、
  `possessiveAdjective`、`aromaPrinciple`、`banter.alive.endTurnPing`、`banter.dead.endTurnPing`、
  `eventDeathPrevention`、`goldMonologue`、`cardsModifierTitle`、`cardsModifierDescription`、`bestiaryQuote`。
- 角色主题色：`#A8E05F`（酸绿色，取自背包上的试剂瓶）；卡背 HSV = (0.24, 0.72, 1.0)。

## 6. 无头测试（不开窗口）

```powershell
powershell -ExecutionPolicy Bypass -File tools	est_headless.ps1
```

游戏以 `--headless` 启动，脚本从 `godot.log` 里等 `[MadScientist] [Diag]` 一行，拿到结果后只关闭自己启动的那个进程。
检测到游戏已经在运行时会直接退出（避免抢 `settings.save` / 日志）。

## 7. 预览图（离线渲染，非游戏截图）

- `docs/select_mockup.png` —— 角色选择背景 + 立绘的合成预览
- `docs/anim_preview.png` —— 战斗动画关键帧（和代码里的关键帧数值一致）
- `docs/preview.png` —— 各尺寸素材总览

生成：`python art/preview_sheet.py`

## 8. 不想打扰正在运行的游戏时

编译到项目内的 `.deploy/` 而不是游戏目录：

```powershell
dotnet build -c Release -p:ModsPath=D:\DSH_work\StS2_MadScientist\.deploy```

（游戏启动时才会读取 mod，运行中覆盖 mods 目录一般不会影响当前这局，但 DLL 被占用时复制会失败，所以稳妥起见用上面的参数。）

无头测试脚本 `tools/test_headless.ps1` 在检测到游戏已运行时**会直接退出**，只停掉它自己启动的进程。

## 8.1 动画与角色选择背景

- 战斗形象：`MadScientistCode/Visuals/MadScientistVisuals.cs`
  - 用代码生成 `AnimationPlayer`（不依赖 Spine）：`Idle`（呼吸/轻微摆动，循环）、`Attack`（前冲+白闪）、`Cast`（起跳+绿光）、`Hurt`（抖动+红闪）、`Die`（倒地淡出），同时注册小写别名。
  - 贴图挂在脚部枢轴上，所以前倾/倒地是绕脚旋转，不会以腰部为轴。
- 角色选择背景：`scenes/char_select_bg_mad_scientist.tscn`（Control + TextureRect），底图是事件插画做的高斯模糊 + 压暗 + 暗角版本 `images/events/tinker_time_bg.png`。
  - `.tscn` 由 `art/pack_scenes.py` 在 `dotnet build` 时追加进 `.pck`（PckPacker 本身不支持场景）。

## 9. 当前状态

已完成：

- [x] 工程骨架、可编译、可打包、游戏内 Mod 列表可见、无报错加载
- [x] 角色注册（`CHARACTER.MADSCIENTIST-MAD_SCIENTIST`），日志自检通过：名称解析为「疯狂科学家」，血量 70
- [x] 角色选择立绘、locked 剪影、顶栏头像、白色描边、地图标记、Mod 缩略图（4 张 PNG 全部能被游戏加载）
- [x] 战斗形象 + 代码生成的待机/攻击/施法/受击/死亡动画（无头端日志确认 5 组动画都已注册，Autoplay=Idle）
- [x] 角色选择背景（事件插画模糊压暗版，`.tscn` 已能从 pck 加载）
- [x] 中英双语名称/描述/台词
- [x] 卡池/遗物池/药水池 + 酸绿色卡背

说明：`MadScientistCode/Diagnostics.cs` 会在每次启动时往日志里写一行自检结果
（`[MadScientist] [Diag] OK ...`），不需要的话删掉这个文件即可。

待办（等卡牌/遗物设计）：

- [ ] 初始牌组、初始遗物（现在临时借用铁甲战士的打击/防御/燃烧之血）
- [ ] 角色专属卡牌、遗物、能力、药水
- [ ] 卡图（每张卡 `images/card_portraits/<card_id>.png` + `big/<card_id>.png`）
- [ ] 战斗动画（现在是一张静态图；后续可换 Spine 或手写 .tscn 动画）
- [ ] 休息处/商人立绘、自定义能量计数器、角色选择背景（需要 `.tscn`，要引入 Godot/MegaDot 才能做）
