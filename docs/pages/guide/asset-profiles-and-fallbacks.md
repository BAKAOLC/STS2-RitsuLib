---
title:
  en: Asset Profiles & Fallbacks
  zh-CN: 资源配置与回退规则
cover: https://wrxinyue.s3.bitiful.net/slay-the-spire-2-wallpaper.webp
---

## Introduction{lang="en"}

::: en

This is the reference document for asset-profile structure, placeholder fallback, and asset-path diagnostics.

RitsuLib uses asset profiles to describe overrideable art, scenes, materials, and related resources.

This document explains the structure behind those profiles and the fallback rules that make them safe to use.

---

:::

## 简介{lang="zh-CN"}

::: zh-CN

本文是资源配置结构、占位角色回退与资源路径诊断的参考文档。

RitsuLib 使用资源配置对象来描述可覆写的美术、场景、材质以及相关资源。

本文专门解释这些配置对象的结构，以及它们背后的回退规则。

---

:::

## Why Asset Profiles Exist{lang="en"}

::: en

Asset overrides could have been exposed as a long flat list of virtual properties.

RitsuLib instead groups them into profile records because that scales better:

- related assets stay together
- partial overrides remain readable
- fallback merging stays explicit
- migration from placeholder-based systems is possible without abandoning structure

For characters, this is especially important because character assets span scenes, UI, VFX, audio, Spine, and multiplayer-specific textures.

---

:::

## 为什么要有资源配置对象{lang="zh-CN"}

::: zh-CN

资源覆写当然可以做成一长串平铺的虚属性。

但 RitsuLib 选择把它们组织成记录类型形式的资源配置，因为这样更适合长期扩展：

- 相关资源会自然聚合在一起
- 局部覆写时可读性更高
- 回退合并规则更明确
- 从默认依赖占位角色的旧框架迁移时，也不用放弃结构化设计

对角色尤其如此，因为角色资源横跨场景、UI、VFX、音频、Spine 和多人模式贴图。

---

:::

## Character Asset Profile Structure{lang="en"}

::: en

`CharacterAssetProfile` is split into several nested record groups:

- `CharacterSceneAssetSet`
- `CharacterUiAssetSet`
- `CharacterVfxAssetSet`
- `CharacterSpineAssetSet`
- `CharacterAudioAssetSet`
- `CharacterMultiplayerAssetSet`

This lets you override only one category without turning the other categories into noise.

Example:

```csharp
public override CharacterAssetProfile AssetProfile => new(
    Scenes: new(
        VisualsPath: "res://MyMod/scenes/character/my_character.tscn",
        EnergyCounterPath: "res://MyMod/ui/energy/my_energy_counter.tscn"),
    Ui: new(
        IconTexturePath: "res://MyMod/ui/top_panel/icon.png",
        MapMarkerPath: "res://MyMod/map/map_marker.png"),
    Audio: new(
        AttackSfx: "event:/sfx/characters/my_character/attack"));
```

---

:::

## 角色资源配置结构{lang="zh-CN"}

::: zh-CN

`CharacterAssetProfile` 被拆成多个嵌套记录类型：

- `CharacterSceneAssetSet`
- `CharacterUiAssetSet`
- `CharacterVfxAssetSet`
- `CharacterSpineAssetSet`
- `CharacterAudioAssetSet`
- `CharacterMultiplayerAssetSet`

这样你只改一个类别时，不会把其他类别也拖成噪音。

例如：

```csharp
public override CharacterAssetProfile AssetProfile => new(
    Scenes: new(
        VisualsPath: "res://MyMod/scenes/character/my_character.tscn",
        EnergyCounterPath: "res://MyMod/ui/energy/my_energy_counter.tscn"),
    Ui: new(
        IconTexturePath: "res://MyMod/ui/top_panel/icon.png",
        MapMarkerPath: "res://MyMod/map/map_marker.png"),
    Audio: new(
        AttackSfx: "event:/sfx/characters/my_character/attack"));
```

---

:::

## Placeholder Character Fallback{lang="en"}

::: en

`ModCharacterTemplate` now exposes:

```csharp
public virtual string? PlaceholderCharacterId => "ironclad";
```

Behavior:

- your explicit `AssetProfile` is read first
- missing fields are filled from `CharacterAssetProfiles.FromCharacterId(PlaceholderCharacterId)`
- if `PlaceholderCharacterId` is `null`, fallback is disabled entirely

This gives you BaseLib-style migration convenience without flattening the whole character API.

---

:::

## 占位角色回退{lang="zh-CN"}

::: zh-CN

`ModCharacterTemplate` 现在提供：

```csharp
public virtual string? PlaceholderCharacterId => "ironclad";
```

它的行为是：

- 先读取你显式写下的 `AssetProfile`
- 缺失项再从 `CharacterAssetProfiles.FromCharacterId(PlaceholderCharacterId)` 补齐
- 如果 `PlaceholderCharacterId` 为 `null`，则彻底关闭回退

这让你既拥有类似 BaseLib 的迁移便利，又保留了 Ritsu 式的结构化角色 API。

---

:::

## How Character Profile Merging Works{lang="en"}

::: en

RitsuLib merges character profiles category-by-category and field-by-field.

That means:

- providing a custom `Scenes` record does not erase `Ui`
- providing only `RestSiteAnimPath` does not erase `MerchantAnimPath`
- providing only `AttackSfx` does not erase the other default SFX entries

This is important because character assets are rarely replaced all at once.

---

:::

## 角色资源配置如何合并{lang="zh-CN"}

::: zh-CN

RitsuLib 对角色资源配置的合并是“按类别、按字段”进行的。

这意味着：

- 你提供一个自定义 `Scenes` 记录，不会影响 `Ui`
- 你只写 `RestSiteAnimPath`，不会把 `MerchantAnimPath` 清空
- 你只写 `AttackSfx`，不会把其余默认音效抹掉

这点非常重要，因为角色资源在实际开发里几乎从来不是一次性全量替换的。

---

:::

## Character Asset Profile Helpers{lang="en"}

::: en

`CharacterAssetProfiles` provides several helper APIs:

- `FromCharacterId(string)`
- `Ironclad()` / `Silent()` / `Defect()` / `Regent()` / `Necrobinder()`
- `Resolve(profile, placeholderCharacterId)`
- `Merge(fallback, profile)`
- `FillMissingFrom(...)`
- `WithPlaceholder(...)`
- `WithScenes(...)`, `WithUi(...)`, `WithVfx(...)`, `WithSpine(...)`, `WithAudio(...)`, `WithMultiplayer(...)`

These helpers exist for two main use cases:

- partial authoring of new characters
- migration from frameworks that assumed a placeholder character from the start

---

:::

## CharacterAssetProfiles 辅助 API{lang="zh-CN"}

::: zh-CN

`CharacterAssetProfiles` 提供了这些工具方法：

- `FromCharacterId(string)`
- `Ironclad()` / `Silent()` / `Defect()` / `Regent()` / `Necrobinder()`
- `Resolve(profile, placeholderCharacterId)`
- `Merge(fallback, profile)`
- `FillMissingFrom(...)`
- `WithPlaceholder(...)`
- `WithScenes(...)`、`WithUi(...)`、`WithVfx(...)`、`WithSpine(...)`、`WithAudio(...)`、`WithMultiplayer(...)`

它们主要服务两类场景：

- 新角色的局部资源编写
- 从默认假定占位角色的旧框架迁移到 RitsuLib

---

:::

## Content Asset Profiles{lang="en"}

::: en

RitsuLib also provides profile records for other content:

- `CardAssetProfile`
- `RelicAssetProfile`
- `PowerAssetProfile`
- `OrbAssetProfile`
- `PotionAssetProfile`
- `AfflictionAssetProfile`
- `EnchantmentAssetProfile`
- `ActAssetProfile`

These are intentionally much smaller because their asset surfaces are smaller.

---

:::

## 其他内容的资源配置{lang="zh-CN"}

::: zh-CN

RitsuLib 也为其他内容提供了类似的资源配置记录类型：

- `CardAssetProfile`
- `RelicAssetProfile`
- `PowerAssetProfile`
- `OrbAssetProfile`
- `PotionAssetProfile`
- `AfflictionAssetProfile`
- `EnchantmentAssetProfile`
- `ActAssetProfile`

这些配置对象更小，是因为它们各自的资源表面本来就更小。

---

:::

## Path Builder Helpers{lang="en"}

::: en

For common vanilla-style asset conventions, there are helper factories:

- `CharacterAssetProfiles.FromCharacterId(...)`
- `ContentAssetProfiles.Card(...)`
- `ContentAssetProfiles.Relic(...)`
- `ContentAssetProfiles.Power(...)`
- `ContentAssetProfiles.Orb(...)`
- `ContentAssetProfiles.Potion(...)`
- `ContentAssetProfiles.Affliction(...)`
- `ContentAssetProfiles.Enchantment(...)`
- `ContentAssetProfiles.Act(...)`

There is also `CharacterAssetPathHelper` for deriving character-related default asset paths such as visuals, energy counter, select background, and map marker.

These helpers are most useful when your assets intentionally follow a conventional naming layout.

If those assets are backed by custom Godot scenes, remember that scene roots and scripted child nodes often need mod-local wrapper classes for stable editor binding. See [Godot Scene Authoring](/guide/godot-scene-authoring).

---

:::

## 路径辅助工厂{lang="zh-CN"}

::: zh-CN

对于符合原版命名习惯的资源布局，RitsuLib 提供了几个常用辅助方法：

- `CharacterAssetProfiles.FromCharacterId(...)`
- `ContentAssetProfiles.Card(...)`
- `ContentAssetProfiles.Relic(...)`
- `ContentAssetProfiles.Power(...)`
- `ContentAssetProfiles.Orb(...)`
- `ContentAssetProfiles.Potion(...)`
- `ContentAssetProfiles.Affliction(...)`
- `ContentAssetProfiles.Enchantment(...)`
- `ContentAssetProfiles.Act(...)`

另外还有 `CharacterAssetPathHelper`，可用于推导角色相关默认路径，例如角色视觉场景、能量球、角色选择背景、小地图标记等。

当你的资源布局本来就遵循某种命名约定时，这些辅助方法会很省事。

如果这些资源背后是自定义 Godot 场景，请记得场景根节点和带脚本的子节点往往需要使用 Mod 本地包装类，编辑器绑定才会更稳定。详见 [Godot 场景编写说明](/guide/godot-scene-authoring)。

---

:::

## Energy Counter vs Big Energy Icon vs Text Icon{lang="en"}

::: en

RitsuLib treats these as separate concerns:

- `CustomEnergyCounterPath`: full combat UI counter scene
- `BigEnergyIconPath`: large pool-linked icon resolved through `EnergyIconHelper`
- `TextEnergyIconPath`: small icon used inside rich text

Why this matters:

- a scene replacement is the right abstraction for a custom counter
- a texture path is the right abstraction for a pool icon
- keeping them separate avoids overloading one API with three unrelated jobs

---

:::

## 能量球场景、大能量图标、文本图标是三层能力{lang="zh-CN"}

::: zh-CN

RitsuLib 明确把它们拆开：

- `CustomEnergyCounterPath`：完整战斗能量球场景
- `BigEnergyIconPath`：通过 `EnergyIconHelper` 解析的大图标
- `TextEnergyIconPath`：富文本描述里的小图标

这样拆的原因是：

- 场景替换才是自定义能量球的正确抽象
- 纹理路径才是池图标的正确抽象
- 把三件事混进一个 API 只会让职责变糊

---

:::

## Missing Path Diagnostics{lang="en"}

::: en

RitsuLib now validates asset-path overrides through `AssetPathDiagnostics`.

Current behavior:

- empty path -> ignore override
- existing path -> use override
- missing path -> log a one-time warning and fall back to the base asset

The warning includes:

- the owner type
- the model entry when available
- the specific profile member name
- the missing path

This makes broken resource wiring much easier to debug.

---

:::

## 缺失路径诊断{lang="zh-CN"}

::: zh-CN

RitsuLib 现在通过 `AssetPathDiagnostics` 统一校验资源路径覆写。

当前行为：

- 路径为空 -> 忽略 override
- 路径存在 -> 使用 override
- 路径不存在 -> 输出一次警告，并回退到原始资源

警告里会尽量带上：

- 宿主类型
- 若可用则带上模型条目标识
- 对应的配置成员名
- 缺失路径本身

这让资源接错线时比以前更容易定位。

---

:::

## What Gets Path Validation{lang="en"}

::: en

Path validation covers resource-like overrides such as:

- card textures, materials, overlays, and banners
- relic / power / orb / potion icons
- act backgrounds
- character visuals, energy counters, map assets, trail scenes, and Spine data
- pool energy icon paths

It does not validate non-resource strings such as audio event ids.

So character SFX override fields are still treated as plain values, not `ResourceLoader` paths.

---

:::

## 哪些内容会做路径校验{lang="zh-CN"}

::: zh-CN

路径校验主要覆盖这类“真正的资源路径”：

- 卡牌贴图、材质、覆盖层、横幅
- 遗物 / 能力 / 球体 / 药水图标
- Act 背景
- 角色视觉场景、能量球、小地图资源、轨迹场景、Spine 数据
- 卡池级能量图标路径

而像音效事件 id 这种“不是 Godot 资源路径”的字符串不会走 `ResourceLoader` 校验。

所以角色 SFX 覆写字段仍然被当作普通字符串值处理，而不是资源路径。

---

:::

## Recommended Character Authoring Pattern{lang="en"}

::: en

For most custom characters, this pattern works well:

1. leave `PlaceholderCharacterId` at `ironclad` or switch it to the base character you want to inherit from
2. override only the assets that are truly custom
3. use pool-level `BigEnergyIconPath` / `TextEnergyIconPath` for energy icon concerns
4. use `CustomEnergyCounterPath` only when you need a real counter scene replacement

This keeps the authoring surface small while preserving safe fallback behavior.

---

:::

## 推荐的角色资源写法{lang="zh-CN"}

::: zh-CN

对大多数自定义角色，比较推荐的模式是：

1. 保留 `PlaceholderCharacterId = "ironclad"`，或者改成你想继承风格的基础角色
2. 只覆写真正自定义的资源
3. 能量图标相关优先放在 pool 级 `BigEnergyIconPath` / `TextEnergyIconPath`
4. 只有真的要换完整能量球 UI 时，再使用 `CustomEnergyCounterPath`

这样内容编写面会比较小，同时还能保留安全回退。

---

:::

## Recommended Content Authoring Pattern{lang="en"}

::: en

For cards and other content:

- use `AssetProfile` when several asset fields belong together
- use a direct `Custom...Path` override only for one-off exceptions
- prefer helper factories like `ContentAssetProfiles.Card(...)` when your resource layout matches the helper's expectations

The profile approach is especially good for keeping portrait, frame, overlay, and banner decisions in one place.

---

:::

## 推荐的普通内容资源写法{lang="zh-CN"}

::: zh-CN

对卡牌及其他内容：

- 当多个资源字段属于同一个决策时，优先使用 `AssetProfile`
- 只有单点特例时，再直接覆写某个 `Custom...Path`
- 当你的资源布局与辅助方法约定一致时，优先考虑 `ContentAssetProfiles.Card(...)` 这类工厂

尤其是卡牌，把立绘、边框、覆盖层、横幅放在一个配置对象里通常会更清晰。

---

:::

## Related Documents{lang="en"}

::: en

- [Character & Unlock Templates](/guide/character-and-unlock-scaffolding)
- [Content Authoring Toolkit](/guide/content-authoring-toolkit)
- [Godot Scene Authoring](/guide/godot-scene-authoring)
- [Diagnostics & Compatibility](/guide/diagnostics-and-compatibility)
- [Framework Design](/guide/framework-design)

:::

## 相关文档{lang="zh-CN"}

::: zh-CN

- [角色与解锁模板](/guide/character-and-unlock-scaffolding)
- [内容注册规则](/guide/content-authoring-toolkit)
- [Godot 场景编写说明](/guide/godot-scene-authoring)
- [诊断与兼容层](/guide/diagnostics-and-compatibility)
- [框架设计](/guide/framework-design)

:::
