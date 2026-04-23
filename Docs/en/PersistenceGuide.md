# Persistence Guide

RitsuLib provides a structured persistence layer for mod data, with scoped storage, profile switching support, backup fallback, and schema migrations.

---

## Main APIs

| API | Purpose |
|---|---|
| `RitsuLibFramework.BeginModDataRegistration(modId)` | Batch registration scope |
| `RitsuLibFramework.GetDataStore(modId)` | Access the mod's `ModDataStore` |
| `ModDataStore.Register<T>(...)` | Register one persistent entry |
| `ModDataStore.Get<T>(key)` | Read data |
| `ModDataStore.Modify<T>(key, ...)` | Mutate data |
| `ModDataStore.Save(key)` / `SaveAll()` | Persist changes |

---

## Why Data Is Registered As Classes

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

## Registering Data

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

## Global vs Profile Scope

`SaveScope` has two values:

- `Global`: shared across all profiles
- `Profile`: isolated per game profile

Design intent:

- use `Global` for mod settings or machine-wide caches
- use `Profile` for unlocks, progression, and run-adjacent player data

Profile-scoped entries are initialized only after profile services are ready.

---

## Reading And Writing

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

## Registration Timing

`BeginModDataRegistration` is the recommended registration pattern because it lets the store defer initialization until the batch is complete.

That helps avoid partial setup states when a mod registers several entries in one place.

At the end of the registration scope:

- global entries initialize immediately
- profile entries initialize when profile services are available

---

## Profile Changes

Profile-scoped entries are aware of profile switching.

When the active profile changes, RitsuLib:

- saves the old profile-scoped data to the old profile path
- reloads the data from the new profile path

This is handled by the framework; mods do not need to manually rebind their profile-scoped stores.

---

## Existing Data Checks

```csharp
if (store.HasExistingData("counter"))
{
    // There was already persisted data on disk
}
```

This is useful when you want different startup behavior for first-time initialization vs loading an existing save.

---

## Recovery And Backup Behavior

The persistence layer tries to be defensive:

- if the main file cannot be read, it attempts backup fallback
- if migrated backup data loads successfully, it can be written back
- if migration or parsing fails badly enough, corrupt data can be renamed with a `.corrupt` suffix
- when recovery fails, the entry falls back to default values

This is meant to keep the mod usable even when local data is damaged.

---

## Migrations

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

## AttachedState vs SavedAttachedState

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

## Recommended Usage Pattern

- define one data class per persisted concept
- use `AttachedState` for ephemeral runtime-only object state
- use `SavedAttachedState` only for model objects that already participate in `SavedProperties`
- keep file names stable after release
- use `Profile` scope by default for progression-like data
- batch registration inside `BeginModDataRegistration`
- add schema versions before you need them, not after a breaking change has already shipped

---

## Related Documents

- [Getting Started](GettingStarted.md)
- [Framework Design](FrameworkDesign.md)
