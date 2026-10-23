# Issue 2 validation

Related to https://github.com/xgw69/ST2MOD_MAD_SCIENTIST/issues/2. This is partial code-level coverage; it does not complete the 63-card acceptance checklist.

Validated on 2026-10-23 against public-beta v0.111.0 (41cef1ea, Steam build 24724944), installed BaseLib 3.4.7, and source baseline 64be9a65c48bebc5d87c926c6747daab1f72beea. Work used an independent source branch. No game was launched, installed mod replaced, or official save written.

## Fixes

1. Clamp the first durability repair to 20 and avoid overflow when adding subsequent repairs.
2. Allow zero durability consumption without dereferencing an absent power; keep the broken-machine guard.
3. Restrict free Blast costs to the power owner's cards.
4. Cancel card-sourced block multiplicatively after additive modifiers, so Dexterity/listener ordering cannot restore it. Non-card and teammate block remain unaffected.
5. Retain Soul Research's spent single-stack power through combat, guard repeat callbacks, and expose saved Used state plus a spent tooltip. Reapplying the card cannot install a fresh revival in the contract tests.
6. Copy Scientific Research on turn-start hand draws as well as effect draws.
7. Apply Blasting Device's independent Blast discount at X=0 even when no cards are generated.

Also translated eight English power values (four titles and four descriptions), and clarified Soul Research's once-per-combat wording in both languages. ScientificResearch and SoulResearchPower were extracted from multi-class files so the tests link the actual implementations.

The full-build follow-up adds meaningful English/Chinese smart descriptions for all 15 powers, Architect dialogue (including the nontranslated `Both` attack enum), and the missing `FUSED_X_CARD` text keys. Fusion retains its existing unprefixed model IDs; no stored model IDs or saved-property shapes were changed by this follow-up. Soul Research's smart-description path now also selects the spent tooltip after revival.

## Results and limits

| Check | Result | Meaning |
|---|---|---|
| Production-linked contract assertions | 84 passed, 0 failed | Simplified game doubles and actual Harmony; includes four new active/spent tooltip assertions |
| PowerShell static resources | 655 passed | Key parity, smart descriptions/variables, fusion runtime keys, eight ancient groups plus Architect, portraits and PNG headers |
| Original Python resources | 4 passed | Existing validator |
| All production C# against installed assemblies | 0 errors, 18 existing nullable warnings | Compile compatibility only; excludes Godot generators, mod analyzers and packer |
| Full Godot release build | PASS: exit 0, 0 errors, 20 warnings | DLL, PCK and manifest produced in an isolated directory |
| Managed runtime localization | 47 passed | Actual game ID resolver + installed BaseLib postfix, native smart-description key getter and Architect enum; no game initialization |
| Release integrity | PASS | 430 PCK checksums; 206 image remaps, 16 source resources and 2 scenes; DLL header and manifest |
| Game / save-reload / UI | NOT RUN | Maintainer testing required |
| Multiplayer | NOT RUN | No second client available |

The expanded baseline reproduced 7 failing assertions (49 passed, including two failures for the base/upgraded Soul Research case). The later X-boundary run reproduced Blasting Device X=0 (79 passed, 1 failed). The first fixes passed 80/80; the full-build follow-up passes 84/84. Coverage includes fusion deep-copy/order/cost/X/keyword/replay and serialization-double tests, nine Mad Science variants, durability, block lock, temporary Blast costs, Soul Research and Scientific Research. Inventory and 9-choose-3 UI are not covered by executable tests.

Installed Hook.ModifyBlock IL was inspected to verify additive modifiers precede multiplicative modifiers. Two-owner tests verify the owner condition, not multiplayer synchronization. Scientific Research enchantments, Soul Research at very low Max HP and next-combat cleanup, native fusion serialization/material disappearance, all card play/upgrade interactions, dialogue rendering, merchant/rest layout and gestures require actual game testing.

issue-2-card-matrix.csv has 63 rows and keeps static/unit evidence separate from play, upgrade, copy/fusion, save and multiplayer statuses. All game acceptance columns remain unverified.

## Full release build

An initial sandbox restore failed with a Windows TLS credential error. An approved subsequent restore stalled and was stopped; its precise cause was not established. A bounded retry with the official nuget.org source then restored all declared dependencies successfully in 4.69 seconds, without changing versions or system security/network settings.

The initial full build exposed 17 STS001 errors. They are now fixed; the failure is retained only as historical evidence in release-build-before-fix.txt. The successful full build ran the unmodified Godot.NET.Sdk 4.5.1 project, ModAnalyzers 0.2.1, PckPacker 0.1.1 and the scene appender. It exited 0 in 42.62 seconds, with 0 errors and 20 warnings: 18 nullable warnings and two STS003 recommendations to prefix fusion IDs. Neither analyzer severity nor packing requirements were relaxed.

The fusion recommendations remain visible because changing to ICustomModel would rename existing model IDs. The installed game's ModelDb.GetEntry and BaseLib's AdjustID postfix were executed in a managed checker: they resolve FUSED_CARD and FUSED_X_CARD, matching both languages. This verifies identity compatibility, not a native save round-trip.

The key rules were checked against the declared [analyzer source](https://github.com/Alchyr/StS2ModAnalyzers/blob/521037e385cdbbfba0a7c511c7e01d52b9e9b2de/ModAnalyzers/ModAnalyzers/LocalizationAnalyzer.cs) and [BaseLib ID postfix](https://github.com/Alchyr/BaseLib-StS2/blob/255b1531a5cb6ba7e4e4b76dc3dd2afbc4a42b23/Patches/Content/PrefixIdPatch.cs). Installed PowerModel IL confirmed that mutable hover tips use SmartDescription, so the spent Soul Research state must override its key as well as Description.

Artifacts are isolated at .validation/deploy/MadScientist/: MadScientist.dll (139,776 bytes), MadScientist.pck (65,027,644 bytes), and MadScientist.json. SHA-256 hashes are recorded in release-integrity.txt. Pack validation checks every entry checksum, source text/scene bytes, and every source PNG's import mapping. It does not prove rendering or gameplay.

## Reproduction and evidence

See tests/ISSUE-2.md for the exact build argument structure and validation commands, and tests/run_issue2.ps1 for the offline compile/contract/resource workflow. Logs in issue-2-evidence/ include baseline failures, final passes, restored dependencies and the successful full build. Absolute local paths were replaced with placeholders for publication. Assembly SHA-256 hashes are included; proprietary DLL/PCK files and saves are not.

Maintainers should test a matching DLL/PCK in a disposable user-data environment with Steam-cloud isolation. The existing headless script shares real settings/saves and must not be used on a valuable profile. Multiplayer additionally requires a second matching client. This PR relates to issue #2 without closing it.
