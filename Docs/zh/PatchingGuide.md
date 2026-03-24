# 补丁系统

RitsuLib 底层仍然使用 Harmony，但在上面包了一层补丁系统，用来统一补丁声明形状、注册方式和失败处理。

---

## 主要类型

| 类型 | 作用 |
|---|---|
| `RitsuLibFramework.CreatePatcher(...)` | 创建 `ModPatcher` |
| `ModPatcher` | 注册并应用补丁 |
| `IPatchMethod` | 单个补丁的静态声明接口 |
| `IModPatches` | 用于分组注册多个补丁 |
| `DynamicPatchBuilder` | 处理运行时发现目标的方法补丁 |

---

## 常规流程

```csharp
var patcher = RitsuLibFramework.CreatePatcher("MyMod", "core-patches");
patcher.RegisterPatch<MySinglePatch>();
patcher.RegisterPatches<MyPatchSet>();

if (!patcher.PatchAll())
    throw new InvalidOperationException("Required patches failed.");
```

推荐做法：

- 每个逻辑区域使用一个 patcher
- 先注册完所有补丁
- 最后统一调用一次 `PatchAll()`
- 如果返回 `false`，就把它视为该 patcher 的启动失败

---

## 用 `IPatchMethod` 编写单个补丁

`IPatchMethod` 是最常见的补丁形式。

```csharp
using STS2RitsuLib.Patching.Models;

public class ExamplePatch : IPatchMethod
{
    public static string PatchId => "example_patch";
    public static string Description => "Log when the method runs";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(SomeType), nameof(SomeType.SomeMethod))];
    }

    public static void Prefix()
    {
        // Harmony prefix
    }
}
```

需要注意：

- `PatchId` 在同一个 patcher 里必须唯一
- `GetTargets()` 可以返回一个或多个目标
- `Prefix`、`Postfix`、`Transpiler`、`Finalizer` 通过命名约定发现
- 如果这些方法一个都没有，补丁会被视为失败

---

## 用 `IModPatches` 分组注册

如果你希望一个类型统一注册多个补丁，可以实现 `IModPatches`：

```csharp
using STS2RitsuLib.Patching.Core;
using STS2RitsuLib.Patching.Models;

public class MyPatchSet : IModPatches
{
    public static void AddTo(ModPatcher patcher)
    {
        patcher.RegisterPatch<ExamplePatch>();
        patcher.RegisterPatch<AnotherPatch>();
    }
}
```

然后这样注册：

```csharp
patcher.RegisterPatches<MyPatchSet>();
```

这就是旧文档里那种“直接 apply 一个补丁集合对象”的现代替代写法。

---

## Critical 与 Optional 补丁

每个 `IPatchMethod` 都可以声明 `IsCritical`。

- `true`：失败后 `PatchAll()` 会失败，patcher 会回滚
- `false`：失败会记录日志，但 patcher 仍可能整体成功

什么时候该设成 `true`：

- 缺了这个补丁 Mod 根本无法安全运行

什么时候适合 `false`：

- 纯 UI / 表现增强
- 兼容性补丁
- 最佳努力型功能

---

## Ignore Missing Target

`ModPatchTarget` 支持 `ignoreIfMissing`：

```csharp
public static ModPatchTarget[] GetTargets()
{
    return [new(typeof(SomeType), "SomeOptionalMethod", ignoreIfMissing: true)];
}
```

适用场景：

- 某个目标只在部分游戏版本存在
- 某个兼容目标可能不存在
- 缺失目标本来就是预期情况

它和 `IsCritical = false` 不是一回事：

- `ignoreIfMissing` 表示“目标不存在也不算错误”
- `IsCritical = false` 表示“目标存在，但补丁失败不应终止整个 patcher”

---

## 一个补丁作用多个目标

一个 `IPatchMethod` 可以同时补多个方法，只要它们共享同一套 Harmony 逻辑。

RitsuLib 会把 `GetTargets()` 自动展开成多个 `ModPatchInfo`。
当 target 不止一个时，框架会自动把目标名附加到 patch id 上，避免冲突。

这样你就能把相关逻辑放在一起，而不需要手动复制多个补丁类。

---

## 动态补丁

当补丁目标需要运行时发现时，可以使用 `DynamicPatchBuilder`。

```csharp
using HarmonyLib;
using STS2RitsuLib.Patching.Builders;

var builder = new DynamicPatchBuilder("my_dynamic")
    .AddMethod(
        targetType: typeof(SomeType),
        methodName: "SomeMethod",
        postfix: DynamicPatchBuilder.FromMethod(typeof(MyRuntimePatch), nameof(MyRuntimePatch.Postfix)),
        isCritical: false,
        description: "Runtime-discovered patch");

patcher.ApplyDynamic(builder, rollbackOnCriticalFailure: false);
```

常见用途：

- patch 运行时生成的类型
- 通过反射扫描后决定 patch 哪些 getter
- patch 一组动态发现的方法

---

## 日志与补丁边界

`CreatePatcher(ownerModId, patcherName, patcherLabel)` 会为每个 patcher 生成：

- 稳定的 Harmony id：`<ownerModId>.<patcherName>`
- 独立的日志前缀
- 独立的注册和应用生命周期

实际开发里，把 patcher 按功能拆开通常非常值得，因为日志会清晰很多。

---

## 推荐结构

对中大型 Mod，比较建议这样组织：

- 每个功能区一个 patch namespace
- 每个功能区一个 `IModPatches` 分组类型
- 每个 `IPatchMethod` 只做一件明确的事
- 兼容补丁默认设为 `IsCritical = false`

这也是 RitsuLib 自己组织内部框架补丁时采用的方式。

---

## 常见错误

- 还没注册完补丁就调用 `PatchAll()`
- 没必要的兼容补丁却标成 critical
- 真正意图是“目标可能不存在”，却只写了 `IsCritical = false`
- `IPatchMethod` 里没有 `Prefix` / `Postfix` / `Transpiler` / `Finalizer`
- 把所有不相关补丁都塞进一个巨大 patcher，导致日志难读

---

## 相关文档

- [GettingStarted.md](GettingStarted.md)
- [FrameworkDesign.md](FrameworkDesign.md)
