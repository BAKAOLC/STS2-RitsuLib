# 框架设计

本文说明 RitsuLib 的核心架构决策，以及这些决策对 Mod 实现方式的影响。

---

## 核心目标

RitsuLib 以少量明确的设计原则为核心：

- 使用显式注册，而非不透明、无约束的「魔法」式发现
- 使用固定模型身份，而非运行时推断名称
- 使用可组合的资源记录，而非大型继承层级
- 使用场景替换，而非原版资源原地修改
- 仅在原版缺少安全扩展点时引入兼容回退

框架会减少重复性工作，但不会把 Mod 运行时结构隐藏为不可见行为。

可选的 **CLR 特性**注册仍然是显式的：只有通过 `ModTypeDiscoveryHub.RegisterModAssembly` 登记的程序集才会参与扫描；每条特性最终仍对应与普通代码相同的注册器调用；`AutoRegistrationAttribute.Inherit` 默认为 **关闭**，避免子类在未声明的情况下继承基类上的特性。说明见 [内容包与注册器](ContentPacksAndRegistries.md#clr-特性注册可选)。

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

这一取舍是明确的：已发布的 CLR 类型一旦改名，就属于兼容性变更。

---

## 先注册，再使用

RitsuLib 要求在早期引导阶段完成显式注册。

`CreateContentPack(modId)` 是便捷入口，但底层注册器仍然是第一层概念。

框架在早期引导阶段冻结注册，以保证：

- 稳定的模型身份
- 稳定的模型列表
- 可预测的查找与解锁行为

因此，框架选择尽早失败，而不是在运行时系统已开始消费模型后继续修改模型图。

具体注册模型可见 [内容包与注册器](ContentPacksAndRegistries.md)。

---

## 资源配置，而不是大型角色基类

角色内容编写围绕结构化资源配置展开。

RitsuLib 不要求把所有角色资源塞进一个单体基类，而是按职责分组：

- `CharacterSceneAssetSet`
- `CharacterUiAssetSet`
- `CharacterVfxAssetSet`
- `CharacterAudioAssetSet`

这样可以保持职责边界清晰：

- 场景资源放一起
- UI 放一起
- VFX 调整放一起
- 音效放一起

这种方式确实比单一占位属性更冗长，但更利于独立扩展各类资源能力。

---

## 资源安全机制

资源配置体系配套了一组小范围的安全机制：

- 角色缺失资源时的占位角色回退
- 完整能量球场景与池级图标的分层 API
- 显式资源路径不存在时的一次性警告

这些行为属于同一设计目标的一部分，用于保证结构化资源 API 在迁移和未完成内容阶段仍然可用。

具体行为与 API 细节见 [资源配置与回退规则](AssetProfilesAndFallbacks.md)。

---

## 兼容层保持收敛

RitsuLib 提供兼容型补丁，但范围刻意保持收敛。

框架不会用自动化去覆盖所有引擎限制。只有在原版扩展点不安全，或重复劳动明显过高时，才会加入兼容回退。

典型例子包括：`debug_compatibility_mode` 下的 `LocTable` 与 `THE_ARCHITECT` 回退、Ancient 对话键注入，以及原版进度检查跳过 Mod 角色时使用的解锁桥接补丁。

具体兼容层可见 [诊断与兼容层](DiagnosticsAndCompatibility.md)。

---

## 为什么要有自己的补丁层

底层仍然是 Harmony，但 RitsuLib 在其上增加了一层统一约定：

- 用 `IPatchMethod` 声明补丁
- 区分 critical / optional
- 支持忽略缺失目标
- 支持分组注册
- 支持动态补丁

目的不是隐藏 Harmony，而是统一补丁声明、失败处理与日志行为，降低大型 Mod 的维护成本。

具体流程见 [补丁系统](PatchingGuide.md)。

---

## 为什么持久化按类组织

RitsuLib 的持久化条目是按类注册的，而不是随手塞原始值。

这样做可以自然支持：

- 数据版本字段
- 数据迁移
- 后续扩展字段
- 更清晰的序列化边界

前期会增加少量样板，但可以避免原始值存档在后期演化为复杂结构时的维护问题。

完整数据设计见 [持久化设计](PersistenceGuide.md)。

---

## 推荐阅读顺序

- [快速入门](GettingStarted.md)
- [内容注册规则](ContentAuthoringToolkit.md)
- [内容包与注册器](ContentPacksAndRegistries.md)
- [角色与解锁模板](CharacterAndUnlockScaffolding.md)
- [时间线与解锁](TimelineAndUnlocks.md)
- [资源配置与回退规则](AssetProfilesAndFallbacks.md)
- [补丁系统](PatchingGuide.md)
- [持久化设计](PersistenceGuide.md)
- [本地化与关键词](LocalizationAndKeywords.md)
- [诊断与兼容层](DiagnosticsAndCompatibility.md)
