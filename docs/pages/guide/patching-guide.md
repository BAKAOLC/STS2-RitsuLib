---
title:
  en: Patching Guide
  zh-CN: 补丁系统
cover: https://wrxinyue.s3.bitiful.net/slay-the-spire-2-wallpaper.webp
---

## Introduction{lang="en"}

::: en

RitsuLib uses Harmony underneath, but wraps it in a patching layer that standardizes declaration shape, registration, and failure handling.

---

:::

## 简介{lang="zh-CN"}

::: zh-CN

RitsuLib 底层仍然使用 Harmony，但在上面包了一层补丁系统，用来统一补丁声明形状、注册方式和失败处理。

---

:::

## Main Types{lang="en"}

::: en

| Type | Purpose |
|---|---|
| `RitsuLibFramework.CreatePatcher(...)` | Create a `ModPatcher` instance |
| `ModPatcher` | Register and apply patches |
| `IPatchMethod` | Static patch declaration contract |
| `IModPatches` | Group multiple patch registrations together |
| `DynamicPatchBuilder` | Build patches from runtime-discovered methods |

---

:::

## 主要类型{lang="zh-CN"}

::: zh-CN

| 类型 | 作用 |
|---|---|
| `RitsuLibFramework.CreatePatcher(...)` | 创建 `ModPatcher` |
| `ModPatcher` | 注册并应用补丁 |
| `IPatchMethod` | 单个补丁的静态声明接口 |
| `IModPatches` | 用于分组注册多个补丁 |
| `DynamicPatchBuilder` | 处理运行时发现目标的方法补丁 |

---

:::

## The Normal Workflow{lang="en"}

::: en

```csharp
var patcher = RitsuLibFramework.CreatePatcher("MyMod", "core-patches");
patcher.RegisterPatch<MySinglePatch>();
patcher.RegisterPatches<MyPatchSet>();

if (!patcher.PatchAll())
    throw new InvalidOperationException("Required patches failed.");
```

Recommended pattern:

- create one patcher per logical patch area
- register all patches first
- call `PatchAll()` once
- treat a `false` return as a startup failure for that patcher

---

:::

## 常规流程{lang="zh-CN"}

::: zh-CN

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

:::

## Writing A Single Patch With `IPatchMethod`{lang="en"}

::: en

`IPatchMethod` is the most common patch shape.

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

Important points:

- `PatchId` must be unique within the patcher
- `GetTargets()` can return one or many targets
- `Prefix`, `Postfix`, `Transpiler`, and `Finalizer` are discovered by name
- if none of those methods exist, patch application fails

---

:::

## 用 `IPatchMethod` 编写单个补丁{lang="zh-CN"}

::: zh-CN

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

:::

## Grouping Patches With `IModPatches`{lang="en"}

::: en

When you want one type to register several patches, implement `IModPatches`:

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

Then register the group with:

```csharp
patcher.RegisterPatches<MyPatchSet>();
```

This is the preferred replacement for older "apply this patch bundle object" examples.

---

:::

## 用 `IModPatches` 分组注册{lang="zh-CN"}

::: zh-CN

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

:::

## Critical vs Optional Patches{lang="en"}

::: en

Each `IPatchMethod` can declare `IsCritical`.

- `true`: failure causes `PatchAll()` to fail and the patcher rolls back
- `false`: failure is logged, but the patcher may still succeed overall

Use `IsCritical = true` when the mod cannot safely run without the patch.
Use `false` for cosmetic features, optional compatibility hooks, or best-effort enhancements.

---

:::

## Critical 与 Optional 补丁{lang="zh-CN"}

::: zh-CN

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

:::

## Ignore Missing Targets{lang="en"}

::: en

`ModPatchTarget` supports an `ignoreIfMissing` flag:

```csharp
public static ModPatchTarget[] GetTargets()
{
    return [new(typeof(SomeType), "SomeOptionalMethod", ignoreIfMissing: true)];
}
```

Use this when:

- a target only exists on some game versions
- a compatibility target may not be present
- the patch is optional by design

This differs from `IsCritical = false`:

- `ignoreIfMissing` means "missing target is expected and not an error"
- `IsCritical = false` means "target exists, but patch failure should not abort the patcher"

---

:::

## Ignore Missing Target{lang="zh-CN"}

::: zh-CN

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

:::

## Multiple Targets In One Patch{lang="en"}

::: en

One `IPatchMethod` can patch several methods that share the same Harmony logic.

RitsuLib automatically expands `GetTargets()` into multiple `ModPatchInfo` entries.
If there is more than one target, the framework appends the target name to the generated patch id.

That lets you keep related logic together without manually duplicating patch classes.

---

:::

## 一个补丁作用多个目标{lang="zh-CN"}

::: zh-CN

一个 `IPatchMethod` 可以同时补多个方法，只要它们共享同一套 Harmony 逻辑。

RitsuLib 会把 `GetTargets()` 自动展开成多个 `ModPatchInfo`。
当目标不止一个时，框架会自动把目标名附加到补丁标识上，避免冲突。

这样你就能把相关逻辑放在一起，而不需要手动复制多个补丁类。

---

:::

## Dynamic Patches{lang="en"}

::: en

Use `DynamicPatchBuilder` when targets are discovered at runtime.

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

Use dynamic patches when static `GetTargets()` is not practical, for example:

- patching generated runtime types
- patching property getters selected from reflection scans
- patching a variable set of discovered methods

---

:::

## 动态补丁{lang="zh-CN"}

::: zh-CN

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

- 给运行时生成的类型打补丁
- 通过反射扫描后决定要给哪些属性读取器打补丁
- 给一组动态发现的方法打补丁

---

:::

## Logging And Patch Boundaries{lang="en"}

::: en

`CreatePatcher(ownerModId, patcherName, patcherLabel)` gives each patcher:

- a stable Harmony id: `<ownerModId>.<patcherName>`
- its own logger prefix
- independent registration and application lifecycle

Splitting patchers by feature area is usually worth it because logs stay easier to read.

---

:::

## 日志与补丁边界{lang="zh-CN"}

::: zh-CN

`CreatePatcher(ownerModId, patcherName, patcherLabel)` 会为每个补丁器生成：

- 稳定的 Harmony id：`<ownerModId>.<patcherName>`
- 独立的日志前缀
- 独立的注册和应用生命周期

实际开发里，把补丁器按功能拆开通常非常值得，因为日志会清晰很多。

---

:::

## Suggested Structure{lang="en"}

::: en

For medium or large mods, this layout works well:

- one patch namespace per feature area
- one `IModPatches` type per feature area
- small `IPatchMethod` classes with one clear purpose each
- optional compatibility patches marked `IsCritical = false`

This matches how RitsuLib itself organizes its internal framework patchers.

---

:::

## 推荐结构{lang="zh-CN"}

::: zh-CN

对中大型 Mod，比较建议这样组织：

- 每个功能区一个补丁命名空间
- 每个功能区一个 `IModPatches` 分组类型
- 每个 `IPatchMethod` 只做一件明确的事
- 兼容补丁默认设为 `IsCritical = false`

这也是 RitsuLib 自己组织内部框架补丁时采用的方式。

---

:::

## Common Mistakes{lang="en"}

::: en

- calling `PatchAll()` before registering all patches
- marking compatibility patches as critical without a real need
- using `IsCritical = false` when `ignoreIfMissing` is the real intent
- writing an `IPatchMethod` with no `Prefix` / `Postfix` / `Transpiler` / `Finalizer`
- keeping all unrelated patches in one giant patcher with unreadable logs

---

:::

## 常见错误{lang="zh-CN"}

::: zh-CN

- 还没注册完补丁就调用 `PatchAll()`
- 没必要的兼容补丁却标成 critical
- 真正意图是“目标可能不存在”，却只写了 `IsCritical = false`
- `IPatchMethod` 里没有 `Prefix` / `Postfix` / `Transpiler` / `Finalizer`
- 把所有不相关补丁都塞进一个巨大 patcher，导致日志难读

---

:::

## Related Documents{lang="en"}

::: en

- [Getting Started](/guide/getting-started)
- [Framework Design](/guide/framework-design)

:::

## 相关文档{lang="zh-CN"}

::: zh-CN

- [快速入门](/guide/getting-started)
- [框架设计](/guide/framework-design)

:::
