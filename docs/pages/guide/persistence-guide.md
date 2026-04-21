---
title:
  en: Persistence Guide
  zh-CN: 持久化设计
cover: https://wrxinyue.s3.bitiful.net/slay-the-spire-2-wallpaper.webp
---

## Introduction{lang="en"}

::: en

RitsuLib provides a structured persistence layer for mod data, with scoped storage, profile switching support, backup fallback, and schema migrations.

---

:::

## 简介{lang="zh-CN"}

::: zh-CN

RitsuLib 提供了一套结构化的 Mod 数据持久化层，支持作用域存储、档位切换、备份回退以及 schema 迁移。

---

:::

## Main APIs{lang="en"}

::: en

| API | Purpose |
|---|---|
| `RitsuLibFramework.BeginModDataRegistration(modId)` | Batch registration scope |
| `RitsuLibFramework.GetDataStore(modId)` | Access the mod's `ModDataStore` |
| `ModDataStore.Register<T>(...)` | Register one persistent entry |
| `ModDataStore.Get<T>(key)` | Read data |
| `ModDataStore.Modify<T>(key, ...)` | Mutate data |
| `ModDataStore.Save(key)` / `SaveAll()` | Persist changes |

---

:::

## 主要 API{lang="zh-CN"}

::: zh-CN

| API | 作用 |
|---|---|
| `RitsuLibFramework.BeginModDataRegistration(modId)` | 批量注册作用域 |
| `RitsuLibFramework.GetDataStore(modId)` | 获取该 Mod 的 `ModDataStore` |
| `ModDataStore.Register<T>(...)` | 注册一个持久化条目 |
| `ModDataStore.Get<T>(key)` | 读取数据 |
| `ModDataStore.Modify<T>(key, ...)` | 修改数据 |
| `ModDataStore.Save(key)` / `SaveAll()` | 持久化写盘 |

---

:::

## Why Data Is Registered As Classes{lang="en"}

::: en

Persistent entries are registered as `class` types with a parameterless constructor.

This allows the framework to support:

- structured JSON payloads
- future schema expansion
- versioned migration
- safer defaults and cloning

So instead of registering a raw integer, define a small data object:

```csharp
public sealed class CounterData
{
    public int Value { get; set; }
}
```

---

:::

## 为什么数据以 class 形式注册{lang="zh-CN"}

::: zh-CN

RitsuLib 的持久化条目要求是带无参构造的类。

这么做是为了自然支持：

- 结构化 JSON
- 后续字段扩展
- schema 迁移
- 更安全的默认值克隆

所以不要注册一个裸 `int`，而是定义一个小数据对象：

```csharp
public sealed class CounterData
{
    public int Value { get; set; }
}
```

---

:::

## Registering Data{lang="en"}

::: en

```csharp
using STS2RitsuLib.Data;
using STS2RitsuLib.Utils.Persistence;

using (RitsuLibFramework.BeginModDataRegistration("MyMod"))
{
    var store = RitsuLibFramework.GetDataStore("MyMod");

    store.Register<CounterData>(
        key: "counter",
        fileName: "counter.json",
        scope: SaveScope.Profile,
        defaultFactory: () => new CounterData(),
        autoCreateIfMissing: true);
}
```

Parameters worth understanding:

- `key`: lookup key inside the store
- `fileName`: file name written under the resolved mod-data path
- `scope`: `Global` or `Profile`
- `defaultFactory`: default value when no file exists or recovery is needed
- `autoCreateIfMissing`: immediately write the default file when missing

---

:::

## 注册数据{lang="zh-CN"}

::: zh-CN

```csharp
using STS2RitsuLib.Data;
using STS2RitsuLib.Utils.Persistence;

using (RitsuLibFramework.BeginModDataRegistration("MyMod"))
{
    var store = RitsuLibFramework.GetDataStore("MyMod");

    store.Register<CounterData>(
        key: "counter",
        fileName: "counter.json",
        scope: SaveScope.Profile,
        defaultFactory: () => new CounterData(),
        autoCreateIfMissing: true);
}
```

这些参数的含义需要特别注意：

- `key`：在 store 内部查找该条目的键
- `fileName`：写入磁盘时使用的文件名
- `scope`：`Global` 或 `Profile`
- `defaultFactory`：没有文件或需要恢复时使用的默认值
- `autoCreateIfMissing`：文件不存在时是否立即写出默认文件

---

:::

## Global vs Profile Scope{lang="en"}

::: en

`SaveScope` has two values:

- `Global`: shared across all profiles
- `Profile`: isolated per game profile

Design intent:

- use `Global` for mod settings or machine-wide caches
- use `Profile` for unlocks, progression, and run-adjacent player data

Profile-scoped entries are initialized only after profile services are ready.

---

:::

## Global 与 Profile 作用域{lang="zh-CN"}

::: zh-CN

`SaveScope` 只有两个值：

- `Global`：所有档位共享
- `Profile`：按游戏档位隔离

设计建议：

- Mod 设置、机器级缓存适合 `Global`
- 解锁、进度、玩家档位相关数据适合 `Profile`

`Profile` 作用域的数据只会在档位服务准备好之后初始化。

---

:::

## Reading And Writing{lang="en"}

::: en

```csharp
var store = RitsuLibFramework.GetDataStore("MyMod");

var counter = store.Get<CounterData>("counter");

store.Modify<CounterData>("counter", data =>
{
    data.Value += 1;
});

store.Save("counter");
```

Notes:

- `Get<T>` returns the live registered object
- `Modify<T>` is just a convenience wrapper around that live object
- saving is explicit unless you choose to save immediately after mutation

---

:::

## 读取与写入{lang="zh-CN"}

::: zh-CN

```csharp
var store = RitsuLibFramework.GetDataStore("MyMod");

var counter = store.Get<CounterData>("counter");

store.Modify<CounterData>("counter", data =>
{
    data.Value += 1;
});

store.Save("counter");
```

几点说明：

- `Get<T>` 返回的是当前注册条目的活动对象
- `Modify<T>` 本质上只是对这个活动对象做一次包装
- 保存默认是显式的，是否每次改完立刻写盘由作者自己决定

---

:::

## Registration Timing{lang="en"}

::: en

`BeginModDataRegistration` is the recommended registration pattern because it lets the store defer initialization until the batch is complete.

That helps avoid partial setup states when a mod registers several entries in one place.

At the end of the registration scope:

- global entries initialize immediately
- profile entries initialize when profile services are available

---

:::

## 注册时机{lang="zh-CN"}

::: zh-CN

推荐始终通过 `BeginModDataRegistration` 批量注册。

这样做的好处是，数据存储器可以在整个批次结束后再统一初始化，避免半注册状态。

作用域结束时：

- 全局条目会立即初始化
- 档位条目会在档位服务可用时初始化

---

:::

## Profile Changes{lang="en"}

::: en

Profile-scoped entries are aware of profile switching.

When the active profile changes, RitsuLib:

- saves the old profile-scoped data to the old profile path
- reloads the data from the new profile path

This is handled by the framework; mods do not need to manually rebind their profile-scoped stores.

---

:::

## 档位切换{lang="zh-CN"}

::: zh-CN

档位作用域的数据会自动感知档位切换。

当当前档位改变时，RitsuLib 会：

- 先把旧档位数据保存回旧档位路径
- 再从新档位路径重新加载

这部分由框架接管，Mod 不需要手写档位切换时的重绑定逻辑。

---

:::

## Existing Data Checks{lang="en"}

::: en

```csharp
if (store.HasExistingData("counter"))
{
    // There was already persisted data on disk
}
```

This is useful when you want different startup behavior for first-time initialization vs loading an existing save.

---

:::

## 判断是否已有存档数据{lang="zh-CN"}

::: zh-CN

```csharp
if (store.HasExistingData("counter"))
{
    // 磁盘上已经存在旧数据
}
```

这个判断常用于区分“首次初始化”和“读取旧存档”两种启动路径。

---

:::

## Recovery And Backup Behavior{lang="en"}

::: en

The persistence layer tries to be defensive:

- if the main file cannot be read, it attempts backup fallback
- if migrated backup data loads successfully, it can be written back
- if migration or parsing fails badly enough, corrupt data can be renamed with a `.corrupt` suffix
- when recovery fails, the entry falls back to default values

This is meant to keep the mod usable even when local data is damaged.

---

:::

## 备份与恢复行为{lang="zh-CN"}

::: zh-CN

持久化层会尽量采用保守策略：

- 主文件读取失败时尝试备份回退
- 如果从备份成功恢复并完成迁移，可以写回主文件
- 当迁移或解析严重失败时，损坏文件可能被重命名为 `.corrupt`
- 若恢复失败，则回退为默认值

目标是：即使本地数据损坏，Mod 仍尽量保持可用。

---

:::

## Migrations{lang="en"}

::: en

`Register<T>` accepts both migration config and migration steps:

```csharp
store.Register<MyData>(
    key: "settings",
    fileName: "settings.json",
    scope: SaveScope.Global,
    defaultFactory: () => new MyData(),
    migrationConfig: new ModDataMigrationConfig(currentDataVersion: 2, minimumSupportedDataVersion: 1),
    migrations:
    [
        new SettingsV1ToV2Migration(),
    ]);
```

Migration rules:

- if no config is registered, data is deserialized directly
- if config exists, the framework reads the schema version field
- migrations run in version order
- data below the minimum supported version is rejected for recovery
- successfully migrated data is saved back in the new format

Use migrations when a file format is published and later evolves.

---

:::

## 数据迁移{lang="zh-CN"}

::: zh-CN

`Register<T>` 支持同时传入迁移配置与迁移步骤：

```csharp
store.Register<MyData>(
    key: "settings",
    fileName: "settings.json",
    scope: SaveScope.Global,
    defaultFactory: () => new MyData(),
    migrationConfig: new ModDataMigrationConfig(currentDataVersion: 2, minimumSupportedDataVersion: 1),
    migrations:
    [
        new SettingsV1ToV2Migration(),
    ]);
```

迁移规则：

- 没有 migration config 时，直接反序列化
- 有 config 时，框架会先读取 schema version 字段
- migration 会按版本顺序执行
- 低于最小支持版本的数据会被拒绝并进入恢复路径
- 成功迁移后的数据会回写成新格式

只要文件格式已经发布并且后续会演进，就建议尽早引入迁移版本号。

---

:::

## AttachedState vs SavedAttachedState{lang="en"}

::: en

`AttachedState<TKey, TValue>` is for runtime-only sidecar state on reference objects.

Use it when:

- the value only matters during the current process
- the key object already defines the lifetime you want
- you do not want to subclass or mutate the target type

`SavedAttachedState<TKey, TValue>` is the persisted counterpart for objects that already flow through `SavedProperties.FromInternal(...)` and `SavedProperties.FillInternal(...)`.

Use it when:

- the key is a model object that participates in vanilla save serialization
- the attached value should survive save/load round-trips
- the value type is already supported by `SavedProperties`

Supported value types are:

- `int`
- `bool`
- `string`
- `ModelId`
- enums
- `int[]`
- enum arrays
- `SerializableCard`
- `SerializableCard[]`
- `List<SerializableCard>`

Example:

```csharp
using STS2RitsuLib.Utils;

private static readonly SavedAttachedState<MyModel, int> BonusDamage =
    new("bonus_damage", () => 0);

BonusDamage[model] = 4;

var bonus = BonusDamage.GetOrCreate(model);
```

Notes:

- persisted names must be globally unique after the `"{typeof(TKey).Name}_{name}"` prefix is applied
- `SavedAttachedState` is not a generic JSON sideband channel; it is intentionally limited to `SavedProperties`-compatible value types
- reward-specific `EncounterState` sideband serialization remains a special-case implementation, not the default persistence pattern

---

:::

## AttachedState 与 SavedAttachedState{lang="zh-CN"}

::: zh-CN

`AttachedState<TKey, TValue>` 用于给引用类型对象挂运行时 sidecar 状态。

适合场景：

- 值只在当前进程内有效
- 希望状态生命周期跟随 key 对象
- 不想为目标类型做继承或直接改模型字段

`SavedAttachedState<TKey, TValue>` 是它的可持久化版本，面向已经会经过 `SavedProperties.FromInternal(...)` 和 `SavedProperties.FillInternal(...)` 的对象。

适合场景：

- key 是会参与原生存档序列化的模型对象
- 附加值需要跨 save/load 保留
- 值类型本身受 `SavedProperties` 支持

当前支持的值类型：

- `int`
- `bool`
- `string`
- `ModelId`
- enum
- `int[]`
- enum 数组
- `SerializableCard`
- `SerializableCard[]`
- `List<SerializableCard>`

示例：

```csharp
using STS2RitsuLib.Utils;

private static readonly SavedAttachedState<MyModel, int> BonusDamage =
    new("bonus_damage", () => 0);

BonusDamage[model] = 4;

var bonus = BonusDamage.GetOrCreate(model);
```

说明：

- 持久化字段名在套用 `"{typeof(TKey).Name}_{name}"` 前缀后必须全局唯一
- `SavedAttachedState` 不是任意 JSON sideband 通道，而是刻意限制在 `SavedProperties` 可表示的值类型范围内
- reward 专用的 `EncounterState` sideband 序列化依然只是特例，不是默认推荐模式

---

:::

## Recommended Usage Pattern{lang="en"}

::: en

- define one data class per persisted concept
- use `AttachedState` for ephemeral runtime-only object state
- use `SavedAttachedState` only for model objects that already participate in `SavedProperties`
- keep file names stable after release
- use `Profile` scope by default for progression-like data
- batch registration inside `BeginModDataRegistration`
- add schema versions before you need them, not after a breaking change has already shipped

---

:::

## 推荐实践{lang="zh-CN"}

::: zh-CN

- 每个持久化概念定义一个独立 class
- 纯运行时对象状态优先使用 `AttachedState`
- 只有模型对象本来就参与 `SavedProperties` 时才使用 `SavedAttachedState`
- 发布后尽量保持 `fileName` 稳定
- 进度类数据默认优先考虑 `Profile`
- 始终在 `BeginModDataRegistration` 中批量注册
- schema version 最好在真正需要迁移前就准备好

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
