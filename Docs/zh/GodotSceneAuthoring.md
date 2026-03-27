# Godot 场景编写说明

本文说明 STS2 Mod 在 Godot 场景编写时的两个实践性问题：

- 面向场景的游戏类型应先在 Mod 里继承一层本地子类，再让编辑器绑定
- Mod 程序集里的 Godot C# 脚本需要在初始化时注册

---

## 为什么需要本地子类

> 以下涉及 Godot Mono 工作流的一个引擎行为特征。

在当前 STS2 modding 使用的 Godot Mono 工作流里，来自游戏程序集的 C# 类型直接绑定到 `.tscn` 场景上时，编辑器行为并不稳定。

实际经验表明，只有当 `.tscn` 里绑定的是 Mod 自己程序集里的脚本类型时，编辑器的打开、序列化、重新绑定才更可靠。

稳定的经验法则：

- 只要场景节点需要表现为游戏里的 Godot 类型，就先在 Mod 本地继承一个子类，再让场景绑定这个子类

---

## 包装子类模式

不要让场景直接绑定 `NEnergyCounter` 这种游戏类型，而是先写一个本地脚本：

```csharp
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace MyMod.Scripts
{
    public partial class MyEnergyCounter : NEnergyCounter
    {
    }
}
```

然后在 `.tscn` 里绑定 `MyEnergyCounter`，而不是直接绑定 `NEnergyCounter`。

这个包装子类完全可以是空的。它存在的意义是给编辑器一个属于 Mod 自身的本地脚本类型。

---

## 常见需要包装的类型

| 游戏类型 | 典型用途 |
|---|---|
| `NEnergyCounter` | 能量球场景根节点 |
| `NRestSiteCharacter` | 休息点角色场景 |
| `NCreatureVisuals` | 角色视觉场景 |
| `NSelectionReticle` | 选择准星 |
| `MegaLabel` | 标签子控件 |

---

## 通用绑定示例

自定义能量球场景：

- 根脚本 → `MyEnergyCounter : NEnergyCounter`
- 标签子节点 → `MyCounterLabel : MegaLabel`

角色视觉场景：

- 根脚本 → `MyCreatureVisuals : NCreatureVisuals`

休息点场景：

- 根脚本 → `MyRestSiteCharacter : NRestSiteCharacter`

重点不在于类名，而在于场景里绑定的脚本应属于 Mod 自己的程序集。

---

## 编辑器侧规则

只要 Godot 编辑器需要打开、序列化或重新绑定某个 Mod 场景里的脚本，就优先使用 Mod 本地子类。

即使：

- 暂时没有额外逻辑
- 只是简单继承一行
- 运行时目标类型在游戏程序集里已存在

这个包装本质上是场景与编辑器之间的兼容层。

---

## 运行时脚本注册

如果 Mod 使用了 Godot C# 场景脚本，需在初始化阶段调用：

```csharp
using System.Reflection;

RitsuLibFramework.EnsureGodotScriptsRegistered(
    Assembly.GetExecutingAssembly(),
    Logger);
```

这让 Godot 的脚本桥接层发现并注册 Mod 程序集里的 C# 脚本。

应在内容注册之前完成此步骤，确保运行时能稳定发现场景脚本。

---

## 推荐工作流

1. 确定需要的游戏侧基类
2. 在 Mod 本地创建继承它的薄包装 `partial class`
3. 让 `.tscn` 绑定这个本地脚本
4. 在初始化入口调用 `EnsureGodotScriptsRegistered(Assembly.GetExecutingAssembly(), Logger)`

---

## 不需要包装的情况

以下内容通常不需要额外包装子类：

- 普通内容模型类（card / relic / power / character）
- 不作为 Godot 脚本使用的纯 C# 辅助类
- 从不绑定到 `.tscn` 场景资源的逻辑类

本文只针对 Godot 场景编写与脚本绑定问题。

---

## 相关文档

- [快速入门](GettingStarted.md)
- [角色与解锁脚手架](CharacterAndUnlockScaffolding.md)
- [资源配置与回退规则](AssetProfilesAndFallbacks.md)
- [诊断与兼容层](DiagnosticsAndCompatibility.md)
