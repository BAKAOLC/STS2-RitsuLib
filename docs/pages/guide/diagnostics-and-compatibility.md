---
title:
  en: Diagnostics & Compatibility
  zh-CN: 诊断与兼容层
cover: https://wrxinyue.s3.bitiful.net/slay-the-spire-2-wallpaper.webp
---

## Introduction{lang="en"}

::: en

This document describes the diagnostic policy and compatibility layers that RitsuLib adds on top of the base game.

It focuses on:

- one-time warnings for recurring authoring errors
- debug-oriented fallbacks for missing localization and invalid unlock data
- narrow bridge patches where vanilla systems do not process mod content

---

:::

## 简介{lang="zh-CN"}

::: zh-CN

本文说明 RitsuLib 在游戏原版之上提供的诊断策略与兼容层。

重点包括：

- 用于定位重复性数据错误的一次性警告
- 面向调试的缺失本地化与无效解锁数据回退
- 原版系统不处理 Mod 内容时使用的窄桥接补丁

---

:::

## Design Intent{lang="en"}

::: en

RitsuLib does not try to hide every engine limitation. It follows these rules:

- Surface real errors as early as possible
- where vanilla offers no safe extension point, the framework may add a bridge
- if a fallback would conceal too much behavior, keep the system explicit

This layer is deliberately narrow and only handles edge cases.

---

:::

## 设计意图{lang="zh-CN"}

::: zh-CN

RitsuLib 不会试图隐藏所有引擎限制。它遵循以下规则：

- 能尽早暴露真实错误，就尽早暴露
- 原版没有安全扩展点时，框架可以补桥
- 某个回退会掩盖过多行为时，保持系统显式

这层能力是刻意收敛的，只处理边缘问题。

---

:::

## One-Time Warning Policy{lang="en"}

::: en

Some RitsuLib diagnostics warn only once per issue (or once per stable key), including:

- Missing resource paths (`AssetPathDiagnostics`)
- Missing `LocTable` keys when the master toggle and the **LocTable missing keys** toggle are enabled (`[Localization][DebugCompat]`)
- `THE_ARCHITECT` empty-`Lines` fallback when the debug compatibility master toggle and the **THE_ARCHITECT missing dialogue** toggle are enabled (`[Ancient]`)
- Other unlock-related one-shots (for example `ModUnlockMissingRuleWarnings`)

Each stable key or issue class logs at most once so traces stay readable.

---

:::

## 一次性警告策略{lang="zh-CN"}

::: zh-CN

RitsuLib 的部分诊断只会对同一个问题（或同一稳定键）警告一次，包括：

- 缺失资源路径（`AssetPathDiagnostics`）
- **总开关 + LocTable 子项**开启时缺失的 `LocTable` 键（`[Localization][DebugCompat]`）
- **调试总开关 + 建筑师子项**开启时，`THE_ARCHITECT` 无对话注入占位值（`[Ancient]`）
- 其他解锁相关的一次性提示（例如 `ModUnlockMissingRuleWarnings`）

同一稳定键或同一类问题至多记录一次，在可读的日志量下保留定位信息。

---

:::

## Asset Path Diagnostics{lang="en"}

::: en

Explicit asset override paths are validated by `AssetPathDiagnostics`.

When a path is missing:

- A one-time warning is logged (host type, model id, member name, missing path)
- Behavior falls back to the original asset path or original behavior

This matters especially for character assets, where vanilla has almost no safe fallback.

See [Asset Profiles & Fallbacks](/guide/asset-profiles-and-fallbacks).

---

:::

## 资源路径诊断{lang="zh-CN"}

::: zh-CN

显式资源覆写路径由 `AssetPathDiagnostics` 校验。

当资源路径不存在时：

- 输出一次警告（包含宿主类型、模型标识、配置成员名和缺失路径）
- 回退到原始资源路径或原始行为

这对角色资源尤其重要，因为游戏原版对缺失角色资源几乎没有安全兜底。

详见 [资源配置与回退规则](/guide/asset-profiles-and-fallbacks)。

---

:::

## Debug Compatibility Mode{lang="en"}

::: en

Optional compatibility fallbacks are grouped under `debug_compatibility_mode` and per-area toggles in mod settings.

**Default (master toggle off):** vanilla behavior for the patched systems described here.

**Master toggle on:** the settings UI shows a **Compatibility fallbacks** section. Per-feature toggles default to **on**. Turning a toggle **off** removes only that fallback.

| Toggle | Effect when enabled |
|---|---|
| **LocTable missing keys** | Placeholder resolution + one-time `[Localization][DebugCompat]` warnings |
| **Invalid unlock epochs** | Skip the grant + one-time `[Unlocks][DebugCompat]` warnings |
| **THE_ARCHITECT missing dialogue** | Inject empty `Lines` entries for `ModContentRegistry` characters + one-time `[Ancient]` warning |

Except for LocTable missing-key handling, each toggle typically applies only to content registered through RitsuLib.

**`ModUnlockMissingRuleWarnings`** (e.g. missing boss-win rule registration): separate diagnostic path from the debug compatibility toggles.

**Released content:** ship complete localization, timeline data, and dialogue. Treat the table above as an iteration aid.

Windows settings path:

```text
%appdata%\SlayTheSpire2\steam\<user_id>\mod_data\com.ritsukage.sts2-RitsuLib\settings.json
```

---

:::

## Debug 兼容模式{lang="zh-CN"}

::: zh-CN

可选兼容回退由 `debug_compatibility_mode`（总开关）与设置页中的分项子开关控制。

**默认（总开关关）：** 走原版逻辑。

**总开关开：** 游戏内展开 **兼容回退项**；子项默认**开启**。关闭某一子项时，仅移除对应回退。

| 子项 | 开启时 |
|---|---|
| **LocTable 缺键** | 占位解析 + 一次性 `[Localization][DebugCompat]` 警告 |
| **无效解锁纪元（Epoch）** | 跳过该次授予 + 一次性 `[Unlocks][DebugCompat]` 警告 |
| **建筑师缺对话** | 对 `ModContentRegistry` 角色注入空 `Lines` 条目 + 一次性 `[Ancient]` 警告 |

除 LocTable 缺键处理外，各子项通常只作用于通过 RitsuLib 注册的内容。

**`ModUnlockMissingRuleWarnings`**（例如未注册 Boss 胜场规则）：独立于调试兼容子开关的诊断路径。

**发布内容：** 应提供完整本地化、时间线与对话数据；上表仅用于迭代阶段排障。

Windows 下设置文件路径：

```text
%appdata%\SlayTheSpire2\steam\<user_id>\mod_data\com.ritsukage.sts2-RitsuLib\settings.json
```

---

:::

## Registration Conflict Diagnostics{lang="en"}

::: en

RitsuLib checks these conflicts explicitly:

| Conflict | Typical cause |
|---|---|
| Model id collision | Two registered models in the same mod/category share the same CLR type name |
| Epoch id collision | Two epochs resolve to the same `Id` |
| Story id collision | Two stories resolve to the same story identity |

When detected, the framework throws or logs errors — it does not accept ambiguous identity silently.

---

:::

## 注册冲突诊断{lang="zh-CN"}

::: zh-CN

RitsuLib 会显式检查以下冲突：

| 冲突类型 | 常见触发场景 |
|---|---|
| 模型 ID 冲突 | 同 Mod / 同类别下两个已注册模型的 CLR 类型名相同 |
| 纪元 ID 冲突 | 两个纪元解析出同一个 `Id` |
| 故事 ID 冲突 | 两个故事解析出同一个故事标识 |

检测到冲突时抛异常或输出错误日志，不会静默接受模糊身份。

---

:::

## Ancient Dialogue Compatibility Layer{lang="en"}

::: en

Before `AncientDialogueSet.PopulateLocKeys`, the framework appends localization-defined ancient dialogue rows for registered mod characters. Authors own the keys; the framework discovers and injects them so mod characters use the same ancient-dialogue pipeline as vanilla.

### `THE_ARCHITECT` dialogue fallback

Gated on the debug compatibility master toggle and the **THE_ARCHITECT missing dialogue** toggle. If vanilla `TheArchitect.LoadDialogue` yields no dialogue, RitsuLib injects empty `Lines` entries for `ModContentRegistry` characters and logs **`[Ancient]`** once.

For key format, see [Localization & Keywords](/guide/localization-and-keywords).

---

:::

## Ancient 对话兼容层{lang="zh-CN"}

::: zh-CN

框架在 `AncientDialogueSet.PopulateLocKeys` 之前为已注册 Mod 角色追加基于本地化键的 Ancient 对话条目；作者编写键，框架负责发现与注入，使 Mod 角色复用与原版相同的 Ancient 对话管线。

### `THE_ARCHITECT` 对话兜底

受调试兼容 **总开关 + 建筑师子项** 控制。若原版 `TheArchitect.LoadDialogue` 无结果，RitsuLib 对 `ModContentRegistry` 角色注入空 `Lines` 占位值并记录一次 **`[Ancient]`** 警告。

具体键结构见 [本地化与关键词](/guide/localization-and-keywords)。

---

:::

## Unlock Compatibility Bridges{lang="en"}

::: en

Several vanilla progression checks only iterate vanilla characters. RitsuLib applies narrow patches so registered unlock rules participate at the same checkpoints for mod characters:

| Bridge | Description |
|---|---|
| Elite wins | Elite kill count → epoch checks |
| Boss wins | Boss kill count → epoch checks |
| Ascension 1 | Ascension 1 → epoch checks |
| Post-run character unlock | Post-run character-unlock epochs |
| Ascension reveal | Ascension reveal unlock checks |

Bridge patches forward RitsuLib-registered rules into vanilla progression checkpoints that otherwise skip mod characters. They do not introduce a separate progression store.

See [Timeline & Unlocks](/guide/timeline-and-unlocks).

---

:::

## 解锁兼容桥{lang="zh-CN"}

::: zh-CN

若干原版进度检查仅针对 vanilla 角色遍历。RitsuLib 以窄补丁将已注册解锁规则挂到相同检查点，使 Mod 角色在同一节点上参与判定：

| 桥接类型 | 说明 |
|---|---|
| 精英胜场 | 精英击杀计数的纪元判定桥接 |
| Boss 胜场 | Boss 击杀计数的纪元判定桥接 |
| 进阶 1 | 进阶 1 的纪元判定桥接 |
| 局后角色解锁 | 局后角色解锁纪元桥接 |
| 进阶显示 | 进阶显示解锁判定桥接 |

桥接补丁会把 RitsuLib 已注册规则转发到原版会跳过 Mod 角色的进度检查点；不引入独立的进度存储。

详见 [时间线与解锁](/guide/timeline-and-unlocks)。

---

:::

## Freeze Errors{lang="en"}

::: en

If content, timeline, or unlock registration runs after freeze, RitsuLib throws.

That is intentional: late registration often means ModelDb caches are already built, fixed identity rules are in use, and unlock filters are active. Failing fast is the safe choice.

---

:::

## Freeze 异常{lang="zh-CN"}

::: zh-CN

当内容、时间线或解锁在冻结之后还被注册时，RitsuLib 会直接抛异常。

这是诊断机制：一旦晚注册，往往意味着 ModelDb 缓存已建立、固定身份规则已被使用、解锁过滤已在运行。此时最安全的做法是尽早失败。

---

:::

## Troubleshooting notes{lang="en"}

::: en

1. Warnings usually point to mod data or configuration (paths, keys, rules), not random engine failure.
2. Fix missing assets and localization in source data rather than relying on placeholders long term.
3. Debug compatibility fallbacks are for iteration; release builds should ship with the master toggle off, or with per-feature toggles disabled and complete data.
4. Prefer explicit registration APIs; compatibility fallbacks are not a long-term architecture substitute.

---

:::

## 排查要点{lang="zh-CN"}

::: zh-CN

1. 警告多表示 Mod 数据或配置问题（路径、键、规则），而非随机引擎故障。
2. 资源与本地化应在数据源补全，而不是长期依赖占位值或兼容回退。
3. 调试兼容回退用于迭代排障；发布构建宜关闭总开关或关闭子项并交付完整数据。
4. 优先使用显式注册 API；兼容回退不宜作为长期架构依赖。

---

:::

## Related Documents{lang="en"}

::: en

- [Asset Profiles & Fallbacks](/guide/asset-profiles-and-fallbacks)
- [Localization & Keywords](/guide/localization-and-keywords)
- [Timeline & Unlocks](/guide/timeline-and-unlocks)
- [Godot Scene Authoring](/guide/godot-scene-authoring)
- [Framework Design](/guide/framework-design)

:::

## 相关文档{lang="zh-CN"}

::: zh-CN

- [资源配置与回退规则](/guide/asset-profiles-and-fallbacks)
- [本地化与关键词](/guide/localization-and-keywords)
- [时间线与解锁](/guide/timeline-and-unlocks)
- [Godot 场景编写说明](/guide/godot-scene-authoring)
- [框架设计](/guide/framework-design)

:::
