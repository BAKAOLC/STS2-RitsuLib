# 框架设计

本文不是单纯列 API，而是解释 RitsuLib 为什么这样设计，方便作者理解“它为什么长这样”。

---

## 核心目标

RitsuLib 的设计偏好很明确：

- 显式注册，而不是隐藏魔法
- 固定模型身份，而不是运行时猜名字
- 组合式模板，而不是巨型继承树
- 用干净的 Godot 场景替换资源，而不是就地魔改原版资源
- 兼容补丁只放在边缘问题上，不把整套框架做成黑盒

换句话说，框架会努力缩短常见工作量，但不会把一切都变成不可见的隐式行为。

---

## 固定模型身份

对通过 RitsuLib 内容注册器注册的模型，`ModelId.Entry` 是确定性的：

```text
<MODID>_<CATEGORY>_<TYPENAME>
```

这样做的好处：

- 本地化 Key 稳定且可预测
- 重构时更容易判断影响面
- 内容冲突更容易定位
- 不依赖反射顺序、自动扫描细节或类发现时机

这个取舍是有意识的：发布后的 CLR 类型改名，不再只是“整理代码”，而是一个兼容性决策。

---

## 先注册，再使用

RitsuLib 的核心前提之一，就是显式且尽早地完成注册。

`CreateContentPack(modId)` 是最顺手的入口，但底层各个注册器仍然保持一等公民地位。

框架会在早期引导阶段冻结注册，是因为它追求：

- 稳定的模型身份
- 稳定的模型列表
- 可预测的查找与解锁行为

所以它更倾向于尽早失败，而不是容忍模型图在游戏开始后被悄悄改写。

具体注册模型可见 [内容包与注册器](ContentPacksAndRegistries.md)。

---

## 资源配置，而不是巨型角色基类

RitsuLib 一个很明确的选择，就是使用结构化的资源配置。

它不会把所有角色资源都塞进一个超大的自定义角色基类，而是按职责分组：

- `CharacterSceneAssetSet`
- `CharacterUiAssetSet`
- `CharacterVfxAssetSet`
- `CharacterAudioAssetSet`

这样做的目的，是让意图足够清晰：

- 场景资源放一起
- UI 放一起
- VFX 调整放一起
- 音效放一起

它确实比“只写一个占位角色 ID”更啰嗦，但作为框架，这种结构更容易扩展，也更不容易随着功能增长变成一团乱麻。

---

## 资源安全护栏

资源配置体系并不是孤立存在的，它配套依赖几条安全护栏：

- 角色缺失资源时的占位角色回退
- 完整能量球场景与池级图标的分层 API
- 显式资源路径不存在时的一次性警告

这些不是零散补丁，而是为了让结构化资源 API 在真实内容编写和迁移场景里仍然足够实用。

具体行为与 API 细节见 [资源配置与回退规则](AssetProfilesAndFallbacks.md)。

---

## 兼容补丁放在边缘，而不是渗透整套框架

RitsuLib 确实提供了一些兼容型封装，但它们都尽量收敛在边缘问题上。

框架不希望默认把每个系统都做成黑盒魔法。它只在“原版扩展点不安全”或“作者重复劳动太多”的地方补一层。

典型例子包括可选的 `LocTable` 软降级（`debug_compatibility_mode`）、Ancient 对话追加以及针对已注册角色的窄 `THE_ARCHITECT` 兜底（与该开关无关），还有原版会忽略 Mod 角色时的解锁桥接补丁。

具体兼容层可见 [诊断与兼容层](DiagnosticsAndCompatibility.md)。

---

## 为什么要有自己的补丁层

底层当然还是 Harmony，但 RitsuLib 在上面加了一层统一约定：

- 用 `IPatchMethod` 声明补丁
- 区分 critical / optional
- 支持忽略缺失目标
- 支持分组注册
- 支持动态补丁

目的不是把 Harmony 藏起来，而是把补丁的形状、失败处理和日志风格统一下来，让大型 Mod 更容易维护。

具体流程见 [补丁系统](PatchingGuide.md)。

---

## 为什么持久化按类组织

RitsuLib 的持久化条目是按类注册的，而不是随手塞原始值。

这样做可以自然支持：

- 数据版本字段
- 数据迁移
- 后续扩展字段
- 更清晰的序列化边界

前期会多一点样板，但能显著降低后期“原本只是一个 int，现在长成复杂结构”的维护痛苦。

完整数据设计见 [持久化设计](PersistenceGuide.md)。

---

## 推荐阅读顺序

- [快速入门](GettingStarted.md)
- [内容注册规则](ContentAuthoringToolkit.md)
- [内容包与注册器](ContentPacksAndRegistries.md)
- [角色与解锁脚手架](CharacterAndUnlockScaffolding.md)
- [时间线与解锁](TimelineAndUnlocks.md)
- [资源配置与回退规则](AssetProfilesAndFallbacks.md)
- [补丁系统](PatchingGuide.md)
- [持久化设计](PersistenceGuide.md)
- [本地化与关键词](LocalizationAndKeywords.md)
- [诊断与兼容层](DiagnosticsAndCompatibility.md)
