# 持久化设计

RitsuLib 提供了一套结构化的 Mod 数据持久化层，支持作用域存储、档位切换、备份回退以及 schema 迁移。

---

## 主要 API

| API | 作用 |
|---|---|
| `RitsuLibFramework.BeginModDataRegistration(modId)` | 批量注册作用域 |
| `RitsuLibFramework.GetDataStore(modId)` | 获取该 Mod 的 `ModDataStore` |
| `ModDataStore.Register<T>(...)` | 注册一个持久化条目 |
| `ModDataStore.Get<T>(key)` | 读取数据 |
| `ModDataStore.Modify<T>(key, ...)` | 修改数据 |
| `ModDataStore.Save(key)` / `SaveAll()` | 持久化写盘 |

---

## 为什么数据以 class 形式注册

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

## 注册数据

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

## Global 与 Profile 作用域

`SaveScope` 只有两个值：

- `Global`：所有档位共享
- `Profile`：按游戏档位隔离

设计建议：

- Mod 设置、机器级缓存适合 `Global`
- 解锁、进度、玩家档位相关数据适合 `Profile`

`Profile` 作用域的数据只会在档位服务准备好之后初始化。

---

## 读取与写入

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

## 注册时机

推荐始终通过 `BeginModDataRegistration` 批量注册。

这样做的好处是，数据存储器可以在整个批次结束后再统一初始化，避免半注册状态。

作用域结束时：

- 全局条目会立即初始化
- 档位条目会在档位服务可用时初始化

---

## 档位切换

档位作用域的数据会自动感知档位切换。

当当前档位改变时，RitsuLib 会：

- 先把旧档位数据保存回旧档位路径
- 再从新档位路径重新加载

这部分由框架接管，Mod 不需要手写档位切换时的重绑定逻辑。

---

## 判断是否已有存档数据

```csharp
if (store.HasExistingData("counter"))
{
    // 磁盘上已经存在旧数据
}
```

这个判断常用于区分“首次初始化”和“读取旧存档”两种启动路径。

---

## 备份与恢复行为

持久化层会尽量采用保守策略：

- 主文件读取失败时尝试备份回退
- 如果从备份成功恢复并完成迁移，可以写回主文件
- 当迁移或解析严重失败时，损坏文件可能被重命名为 `.corrupt`
- 若恢复失败，则回退为默认值

目标是：即使本地数据损坏，Mod 仍尽量保持可用。

---

## 数据迁移

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

## AttachedState 与 SavedAttachedState

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

## 推荐实践

- 每个持久化概念定义一个独立 class
- 纯运行时对象状态优先使用 `AttachedState`
- 只有模型对象本来就参与 `SavedProperties` 时才使用 `SavedAttachedState`
- 发布后尽量保持 `fileName` 稳定
- 进度类数据默认优先考虑 `Profile`
- 始终在 `BeginModDataRegistration` 中批量注册
- schema version 最好在真正需要迁移前就准备好

---

## 相关文档

- [快速入门](GettingStarted.md)
- [框架设计](FrameworkDesign.md)
