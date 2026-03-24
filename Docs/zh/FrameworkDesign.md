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

RitsuLib 把作者接口拆成几类注册器：

- 内容注册器
- 关键词注册器
- Timeline 注册器
- 解锁注册器
- 持久化数据存储

`CreateContentPack(modId)` 是最常用的入口，但底层注册器仍然保留，行为也保持显式。

框架会在游戏早期冻结内容注册。这样做是为了：

- 模型身份只确定一次
- 后续查找保持稳定
- 避免游戏运行过程中突然插入内容导致的时序问题

这个设计更偏向“尽早暴露错误”，而不是容忍后期偷偷改动模型图。

---

## 资源 Profile，而不是巨型角色基类

RitsuLib 一个很明确的选择，就是使用 asset profile。

它不会把所有角色资源都塞进一个超大的自定义角色基类，而是按职责分组：

- `CharacterSceneAssetSet`
- `CharacterUiAssetSet`
- `CharacterVfxAssetSet`
- `CharacterAudioAssetSet`

这样做的目的，是让意图足够清晰：

- scenes 放一起
- UI 放一起
- VFX 调整放一起
- 音效放一起

它确实比“只写一个占位角色 ID”更啰嗦，但作为框架，这种结构更容易扩展，也更不容易随着功能增长变成一团乱麻。

---

## Placeholder 回退是安全层，不只是便利功能

原版游戏对缺失角色资源几乎没有可靠兜底。

所以 placeholder 继承并不是单纯图省事，而是稳定性要求。

RitsuLib 现在通过 `ModCharacterTemplate.PlaceholderCharacterId` 提供这层能力：

- 默认值：`ironclad`
- 未填写的角色资源会从该基础角色 profile 中补齐
- 返回 `null` 可以彻底关闭回退

这样既保留了 Ritsu 式的显式 profile 设计，也解决了从别的框架迁移时必须手填商人、休息点、小地图、默认音效这些重复工作的痛点。

这里最关键的设计点是：placeholder 是补齐，不是覆盖。

- 你自己写的 profile 仍然是主来源
- 只有缺失项才会继承

---

## 自定义能量球与池能量图标是两层能力

RitsuLib 把它们明确拆开：

- `CustomEnergyCounterPath`：战斗里的能量球场景
- `BigEnergyIconPath`：通过 `EnergyIconHelper` 使用的大图标
- `TextEnergyIconPath`：富文本描述里的小图标

这是有意为之的。

战斗能量球是场景层 UI，应该有场景层 API。
卡池图标则是内容资源管线的一部分，应该挂在 pool 模型上。

这样既保留了完整自定义 `NEnergyCounter` 的干净做法，也补上了作者期望的图标级封装。

---

## 缺失路径会 warning，并回退

RitsuLib 现在会更积极地校验显式资源路径。

当前行为：

- 路径为空：忽略 override
- 路径存在：使用 override
- 路径不存在：输出一次 warning，并回退到原始资源

这对角色资源尤其重要，因为错误路径否则往往会在更晚的 UI 加载阶段才暴露，而且报错位置很绕。

之所以是“一次 warning”，是为了既能看到问题，又不让日志在每帧或每次打开界面时刷屏。

---

## 兼容补丁放在边缘，而不是渗透整套框架

RitsuLib 确实提供了一些兼容型封装，但它们都尽量收敛在边缘问题上：

- `LocTable` 缺失 Key 的 debug 兼容模式
- 通过本地化 Key 自动补 Ancient 对话
- 原版进度/解锁钩子不足时的桥接补丁

框架不希望默认把每个系统都做成黑盒魔法。
它只在“原版扩展点不安全”或“作者重复劳动太多”的地方补一层。

---

## 为什么要有自己的补丁层

底层当然还是 Harmony，但 RitsuLib 在上面加了一层统一约定：

- 用 `IPatchMethod` 声明补丁
- 区分 critical / optional
- 支持 ignore-if-missing target
- 支持分组注册
- 支持动态补丁

目的不是把 Harmony 藏起来，而是把补丁的形状、失败处理和日志风格统一下来，让大型 Mod 更容易维护。

具体流程见 [PatchingGuide.md](PatchingGuide.md)。

---

## 为什么持久化是 class-based

RitsuLib 的持久化条目是按 class 注册的，而不是随手塞 primitive。

这样做可以自然支持：

- schema version 字段
- 数据迁移
- 后续扩展字段
- 更清晰的序列化边界

前期会多一点样板，但能显著降低后期“原本只是一个 int，现在长成复杂结构”的维护痛苦。

完整数据设计见 [PersistenceGuide.md](PersistenceGuide.md)。

---

## 推荐阅读顺序

- [GettingStarted.md](GettingStarted.md)
- [ContentAuthoringToolkit.md](ContentAuthoringToolkit.md)
- [CharacterAndUnlockScaffolding.md](CharacterAndUnlockScaffolding.md)
- [PatchingGuide.md](PatchingGuide.md)
- [PersistenceGuide.md](PersistenceGuide.md)
- [LocalizationAndKeywords.md](LocalizationAndKeywords.md)
