# Patching Guide

RitsuLib uses Harmony underneath, but wraps it in a patching layer that standardizes declaration shape, registration, and failure handling.

---

## Main Types

| Type | Purpose |
|---|---|
| `RitsuLibFramework.CreatePatcher(...)` | Create a `ModPatcher` instance |
| `ModPatcher` | Register and apply patches |
| `IPatchMethod` | Static patch declaration contract |
| `IModPatches` | Group multiple patch registrations together |
| `DynamicPatchBuilder` | Build patches from runtime-discovered methods |

---

## The Normal Workflow

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

## Writing A Single Patch With `IPatchMethod`

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

## Grouping Patches With `IModPatches`

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

## Critical vs Optional Patches

Each `IPatchMethod` can declare `IsCritical`.

- `true`: failure causes `PatchAll()` to fail and the patcher rolls back
- `false`: failure is logged, but the patcher may still succeed overall

Use `IsCritical = true` when the mod cannot safely run without the patch.
Use `false` for cosmetic features, optional compatibility hooks, or best-effort enhancements.

---

## Ignore Missing Targets

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

## Multiple Targets In One Patch

One `IPatchMethod` can patch several methods that share the same Harmony logic.

RitsuLib automatically expands `GetTargets()` into multiple `ModPatchInfo` entries.
If there is more than one target, the framework appends the target name to the generated patch id.

That lets you keep related logic together without manually duplicating patch classes.

---

## Dynamic Patches

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

## Logging And Patch Boundaries

`CreatePatcher(ownerModId, patcherName, patcherLabel)` gives each patcher:

- a stable Harmony id: `<ownerModId>.<patcherName>`
- its own logger prefix
- independent registration and application lifecycle

Splitting patchers by feature area is usually worth it because logs stay easier to read.

---

## Suggested Structure

For medium or large mods, this layout works well:

- one patch namespace per feature area
- one `IModPatches` type per feature area
- small `IPatchMethod` classes with one clear purpose each
- optional compatibility patches marked `IsCritical = false`

This matches how RitsuLib itself organizes its internal framework patchers.

---

## Common Mistakes

- calling `PatchAll()` before registering all patches
- marking compatibility patches as critical without a real need
- using `IsCritical = false` when `ignoreIfMissing` is the real intent
- writing an `IPatchMethod` with no `Prefix` / `Postfix` / `Transpiler` / `Finalizer`
- keeping all unrelated patches in one giant patcher with unreadable logs

---

## Related Documents

- [GettingStarted.md](GettingStarted.md)
- [FrameworkDesign.md](FrameworkDesign.md)
