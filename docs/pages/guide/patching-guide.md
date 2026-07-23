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

For common targets, use `PatchTarget` factory helpers to make the intent explicit:

```csharp
public static ModPatchTarget[] GetTargets() =>
[
    PatchTarget.Method<CombatRoom>("OnEnter"),
    PatchTarget.Method<SaveManager>(nameof(SaveManager.SaveRun), typeof(AbstractRoom), typeof(bool)),
    PatchTarget.Getter<Player>(nameof(Player.Piles)),
    PatchTarget.OptionalGetter<PowerModel>("PackedIconPath"),
];
```

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

常见目标可以使用 `PatchTarget` 工厂辅助方法，让意图更明确：

```csharp
public static ModPatchTarget[] GetTargets() =>
[
    PatchTarget.Method<CombatRoom>("OnEnter"),
    PatchTarget.Method<SaveManager>(nameof(SaveManager.SaveRun), typeof(AbstractRoom), typeof(bool)),
    PatchTarget.Getter<Player>(nameof(Player.Piles)),
    PatchTarget.OptionalGetter<PowerModel>("PackedIconPath"),
];
```

:::

## Private Member Access{lang="en"}

::: en

Use `PrivateAccess` when a patch must call private game methods or read private fields. It wraps `AccessTools` with required-member checks, so missing members fail with clearer errors during patch initialization.

```csharp
private static readonly AccessTools.FieldRef<NCombatUi, CombatState> StateRef =
    PrivateAccess.FieldRef<NCombatUi, CombatState>("_state");

private static readonly Func<NCardPlay, CardModel?> GetCard =
    PrivateAccess.DeclaredGetterDelegate<NCardPlay, Func<NCardPlay, CardModel?>>("Card");

private static readonly Action<NCardPlay, bool> Cleanup =
    PrivateAccess.DeclaredMethodDelegate<NCardPlay, Action<NCardPlay, bool>>(
        "Cleanup",
        typeof(bool));
```

Prefer declared-member helpers when the target is a known private implementation detail on that exact type. Use inherited-member helpers only when the patch intentionally accepts members from a base type.

:::

## 私有成员访问{lang="zh-CN"}

::: zh-CN

patch 必须调用私有游戏方法或读取私有字段时，使用 `PrivateAccess`。它包装了 `AccessTools` 并做必需成员检查，因此成员缺失会在 patch 初始化阶段给出更清晰的错误。

```csharp
private static readonly AccessTools.FieldRef<NCombatUi, CombatState> StateRef =
    PrivateAccess.FieldRef<NCombatUi, CombatState>("_state");

private static readonly Func<NCardPlay, CardModel?> GetCard =
    PrivateAccess.DeclaredGetterDelegate<NCardPlay, Func<NCardPlay, CardModel?>>("Card");

private static readonly Action<NCardPlay, bool> Cleanup =
    PrivateAccess.DeclaredMethodDelegate<NCardPlay, Action<NCardPlay, bool>>(
        "Cleanup",
        typeof(bool));
```

目标是确切类型上的私有实现细节时，优先使用 declared-member helper。只有 patch 有意接受基类成员时，才使用 inherited-member helper。

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

## Transpiler Wrappers{lang="en"}

::: en

Use `HarmonyIl`, `HarmonyIlPattern`, and `HarmonyIlRewriter` for transpilers that must edit IL.
Do not directly build long ad hoc `CodeInstruction` chains in patch bodies.

```csharp
var rewriter = HarmonyIlRewriter.From(instructions);
var pattern = HarmonyIlPattern.Sequence(
    HarmonyIl.IsLdstr("prefix"),
    HarmonyIl.IsLdloc(),
    HarmonyIl.IsCall(concatMethod),
    HarmonyIl.IsStloc());

var report = rewriter.TryInsertAfterFirst(
    "MyPatch insert override",
    pattern,
    [
        HarmonyIl.Ldarg(0),
        HarmonyIl.Call(overrideMethod),
    ],
    code => code.Any(instruction => HarmonyIl.IsCallTo(instruction, overrideMethod)));

report.RequireSucceeded();
if (report.Applied > 0)
    report.RequireExactly(1);

return rewriter.InstructionsChecked("MyPatch insert override");
```

Use report expectations to prove the rewrite happened, or that equivalent IL was already present.
Use `InstructionsChecked` to validate common structural errors such as missing branch labels or invalid reflection operands.
For methods that another mod may have already modified, pass an `alreadySatisfied` predicate and use anchor searches such as `TryFindAfter` / `TryFindBefore` instead of replacing wide instruction spans.

:::

## Async Patch Helpers{lang="en"}

::: en

For async methods that can be composed at the method boundary, prefer a normal Harmony postfix that replaces
`ref Task __result` / `ref Task<T> __result` with `HarmonyAsyncTaskBridge.After(...)`.

For async state-machine transpilers, use `HarmonyAsyncIl` to recognize compiler-generated await sites before
rewriting them. `RedirectAwaitedCalls` is the narrowest option: it only redirects calls that are directly awaited
by the state machine and requires the replacement method to preserve the stack shape and exact awaitable return
type. Use `ReplaceAwaitedCalls` when the replacement must load extra state-machine fields or emit a short custom
instruction payload before returning the same awaitable type.

```csharp
var rewriter = HarmonyIlRewriter.From(instructions);
var report = HarmonyAsyncIl.RedirectAwaitedCalls(
    rewriter,
    "Redirect awaited OnPlay",
    originalOnPlayMethod,
    wrapperMethod,
    code => code.Any(instruction => HarmonyIl.IsCallTo(instruction, wrapperMethod)));

report.RequireSucceeded();
report.RequireExactly(1);
return rewriter.InstructionsChecked("Redirect awaited OnPlay");
```

This helper does not create new async states. If a patch needs an additional independent await point, prefer a
task-wrapper design or a narrower awaited-call wrapper first.

:::

## Original IL Inspection Helpers{lang="en"}

::: en

Submods that need to classify method behavior can inspect original IL without creating a transpiler.
`GetOriginalIl()` resolves an async method to its generated `MoveNext` body by default and exposes both the
instruction snapshot and its ordered call targets:

```csharp
var body = onPlayMethod.GetOriginalIl();
var directlyCallsTarget = body.HasCall(IsTargetMethod);
```

For explicitly bounded helper-method traversal, use `HasOriginalIlCall()` or
`FindOriginalIlCallPath()`:

```csharp
var path = onPlayMethod.FindOriginalIlCallPath(
    isTarget: IsTargetMethod,
    shouldTraverse: called => called.DeclaringType == cardType);
```

Traversal is opt-in and returns the shortest matching path. Supply a narrow `shouldTraverse` predicate; the helper
does not infer virtual dispatch, delegate targets, reflection calls, or arbitrary transitive behavior.
These APIs inspect original method IL. They do not include Harmony transpiler output and do not cache
domain-specific classifications.

This inspection layer and `HarmonyAsyncIl` serve different purposes. Inspection resolves an async method body and
queries ordinary calls. `HarmonyAsyncIl` recognizes compiler-generated await-site structure when a transpiler must
safely redirect or replace a directly awaited call.

:::

## Transpiler 包装器{lang="zh-CN"}

::: zh-CN

需要编辑 IL 的 transpiler 使用 `HarmonyIl`、`HarmonyIlPattern` 和 `HarmonyIlRewriter`。
不要在 patch 方法里直接手写很长的 `CodeInstruction` 链条。

```csharp
var rewriter = HarmonyIlRewriter.From(instructions);
var pattern = HarmonyIlPattern.Sequence(
    HarmonyIl.IsLdstr("prefix"),
    HarmonyIl.IsLdloc(),
    HarmonyIl.IsCall(concatMethod),
    HarmonyIl.IsStloc());

var report = rewriter.TryInsertAfterFirst(
    "MyPatch insert override",
    pattern,
    [
        HarmonyIl.Ldarg(0),
        HarmonyIl.Call(overrideMethod),
    ],
    code => code.Any(instruction => HarmonyIl.IsCallTo(instruction, overrideMethod)));

report.RequireSucceeded();
if (report.Applied > 0)
    report.RequireExactly(1);

return rewriter.InstructionsChecked("MyPatch insert override");
```

用 report expectation 证明改写已经发生，或证明等价 IL 已经存在。
用 `InstructionsChecked` 验证常见结构问题，例如 branch label 缺失、反射 operand 类型错误。
目标方法可能已被其它 mod 修改时，传入 `alreadySatisfied` 谓词，并优先使用 `TryFindAfter` / `TryFindBefore` 这类锚点搜索，避免替换过宽的指令区间。

:::

## Async Patch 辅助工具{lang="zh-CN"}

::: zh-CN

如果 async 方法可以在方法边界组合，优先使用普通 Harmony postfix，把 `ref Task __result` /
`ref Task<T> __result` 替换为 `HarmonyAsyncTaskBridge.After(...)`。

如果必须 patch async 状态机 transpiler，使用 `HarmonyAsyncIl` 先识别编译器生成的 await 点，再做改写。
`RedirectAwaitedCalls` 是最窄的选项：它只重定向被状态机直接 await 的调用，并要求替换方法保持相同的栈形状和完全一致的 awaitable 返回类型。
如果替换逻辑需要加载额外的状态机字段，或需要发出一小段自定义指令 payload，使用 `ReplaceAwaitedCalls`，但 payload 末尾仍必须返回同一个 awaitable 类型。

```csharp
var rewriter = HarmonyIlRewriter.From(instructions);
var report = HarmonyAsyncIl.RedirectAwaitedCalls(
    rewriter,
    "Redirect awaited OnPlay",
    originalOnPlayMethod,
    wrapperMethod,
    code => code.Any(instruction => HarmonyIl.IsCallTo(instruction, wrapperMethod)));

report.RequireSucceeded();
report.RequireExactly(1);
return rewriter.InstructionsChecked("Redirect awaited OnPlay");
```

这个辅助工具不会创建新的 async state。如果 patch 需要额外的独立 await 点，优先考虑 task-wrapper 设计或更窄的 awaited-call wrapper。

:::

## 原始 IL 检查工具{lang="zh-CN"}

::: zh-CN

子 Mod 需要判断方法行为时，可以直接检查原始 IL，不必为此创建 transpiler。
`GetOriginalIl()` 默认把 async 方法解析到生成的 `MoveNext` 方法体，并提供指令快照和按顺序排列的调用目标：

```csharp
var body = onPlayMethod.GetOriginalIl();
var directlyCallsTarget = body.HasCall(IsTargetMethod);
```

需要在明确边界内继续检查辅助方法时，使用 `HasOriginalIlCall()` 或
`FindOriginalIlCallPath()`：

```csharp
var path = onPlayMethod.FindOriginalIlCallPath(
    isTarget: IsTargetMethod,
    shouldTraverse: called => called.DeclaringType == cardType);
```

方法下钻必须显式启用，并返回最短匹配路径。请提供范围尽量窄的 `shouldTraverse` 谓词；工具不会推断虚调用分派、
委托目标、反射调用或任意传递行为。这些 API 检查的是原始方法 IL，不包含 Harmony transpiler 的输出，也不缓存
特定业务的分类结果。

这个检查层与 `HarmonyAsyncIl` 的用途不同：检查层负责解析 async 方法体并查询普通调用；
`HarmonyAsyncIl` 负责在 transpiler 必须安全重定向或替换直接 await 调用时，识别编译器生成的 await 点结构。

:::

## Release Checklist{lang="en"}

::: en

- Give every patch a stable `PatchId`.
- Set `IsCritical = false` for compatibility patches that can safely be skipped.
- Add `parameterTypes` for overloaded targets.
- Use `HarmonyIlRewriter` / `HarmonyIlPattern` wrappers, report expectations, and `InstructionsChecked` for fragile transpilers.
- Prefer lifecycle events and registries when they cover the use case.

:::

## 发布检查{lang="zh-CN"}

::: zh-CN

- 每个 patch 都有稳定 `PatchId`。
- 可以安全跳过的兼容 patch 设置 `IsCritical = false`。
- 有重载的目标方法填写 `parameterTypes`。
- 脆弱 transpiler 使用 `HarmonyIlRewriter` / `HarmonyIlPattern` 包装器、report expectation 和 `InstructionsChecked`。
- 生命周期事件和注册器能覆盖的场景，优先使用它们。

:::
