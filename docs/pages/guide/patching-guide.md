---
title:
  en: Patching
  zh-CN: 补丁系统
cover: https://wrxinyue.s3.bitiful.net/slay-the-spire-2-wallpaper.webp
---

## Create A Patcher{lang="en"}

::: en

Use one patcher per logical area. Apply required patchers through `ApplyRequiredPatcher` so the mod can disable itself on critical failure.

```csharp
var patcher = RitsuLibFramework.CreatePatcher("MyMod", "combat");
patcher.RegisterPatches<MyCombatPatch>();
RitsuLibFramework.ApplyRequiredPatcher(patcher, DisableMod);
```

Use separate patchers when one optional feature can fail without disabling the whole mod.

:::

## 创建 Patcher{lang="zh-CN"}

::: zh-CN

每个逻辑区域使用一个 patcher。必要 patcher 通过 `ApplyRequiredPatcher` 应用，这样关键失败时 Mod 可以关闭自己。

```csharp
var patcher = RitsuLibFramework.CreatePatcher("MyMod", "combat");
patcher.RegisterPatches<MyCombatPatch>();
RitsuLibFramework.ApplyRequiredPatcher(patcher, DisableMod);
```

某个可选功能即使失败也不应关闭整个 Mod 时，把它放进独立 patcher。

:::

## Write Patch Classes{lang="en"}

::: en

Implement `IPatchMethod` for strongly typed target declarations.

```csharp
public sealed class MyCombatPatch : IPatchMethod
{
    public static string PatchId => "my_mod_combat_patch";
    public static string Description => "Adjust combat start behavior";
    public static bool IsCritical => true;

    public static ModPatchTarget[] GetTargets() =>
    [
        new(typeof(CombatRoom), "OnEnter"),
    ];

    public static void Postfix(CombatRoom __instance)
    {
        // Harmony postfix body.
    }
}
```

Use `new ModPatchTarget(type, methodName, parameterTypes)` when overloads need disambiguation. Use `ignoreIfMissing: true` only for optional compatibility targets.

:::

## 编写 Patch 类{lang="zh-CN"}

::: zh-CN

实现 `IPatchMethod`，用强类型方式声明目标。

```csharp
public sealed class MyCombatPatch : IPatchMethod
{
    public static string PatchId => "my_mod_combat_patch";
    public static string Description => "Adjust combat start behavior";
    public static bool IsCritical => true;

    public static ModPatchTarget[] GetTargets() =>
    [
        new(typeof(CombatRoom), "OnEnter"),
    ];

    public static void Postfix(CombatRoom __instance)
    {
        // Harmony postfix body.
    }
}
```

目标方法有重载时，使用 `new ModPatchTarget(type, methodName, parameterTypes)`。只有可选兼容目标才使用 `ignoreIfMissing: true`。

:::

## Dynamic Patches{lang="en"}

::: en

Use dynamic patches when the target method is discovered at runtime.

```csharp
patcher.RegisterDynamicPatch(new DynamicPatchInfo(
    id: "my_mod_dynamic_target",
    originalMethod: resolvedMethod,
    patchType: typeof(MyDynamicPatch),
    isCritical: false,
    description: "Optional runtime target"));

patcher.PatchAll();
```

For ordinary game methods, static `IPatchMethod` classes are easier to read and review.

:::

## 动态补丁{lang="zh-CN"}

::: zh-CN

目标方法需要运行时发现时，使用 dynamic patch。

```csharp
patcher.RegisterDynamicPatch(new DynamicPatchInfo(
    id: "my_mod_dynamic_target",
    originalMethod: resolvedMethod,
    patchType: typeof(MyDynamicPatch),
    isCritical: false,
    description: "Optional runtime target"));

patcher.PatchAll();
```

普通游戏方法优先使用静态 `IPatchMethod` 类，更容易阅读和审查。

:::

## Release Checklist{lang="en"}

::: en

- Give every patch a stable `PatchId`.
- Set `IsCritical = false` for compatibility patches that can safely be skipped.
- Add `parameterTypes` for overloaded targets.
- Use `HarmonyVerifiedIl` or tests for fragile transpilers.
- Prefer lifecycle events and registries when they cover the use case.

:::

## 发布检查{lang="zh-CN"}

::: zh-CN

- 每个 patch 都有稳定 `PatchId`。
- 可以安全跳过的兼容 patch 设置 `IsCritical = false`。
- 有重载的目标方法填写 `parameterTypes`。
- 脆弱 transpiler 使用 `HarmonyVerifiedIl` 或测试覆盖。
- 生命周期事件和注册器能覆盖的场景，优先使用它们。

:::
