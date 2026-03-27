# Godot Scene Authoring

This document covers two practical concerns for STS2 mods authoring Godot scenes:

- Scene-facing game types should be subclassed in the mod first, then bound in the editor
- Godot C# scripts in the mod assembly must be registered during initialization

---

## Why Mod-Local Subclasses

> The following describes an engine behavior in the Godot Mono workflow.

In the Godot Mono workflow used for STS2 modding, binding C# types from the game assembly directly to `.tscn` scenes is unreliable in the editor.

Experience shows that opening, serializing, and rebinding works more reliably when the `.tscn` binds to a script type from your own mod assembly.

Practical rule:

- Whenever a scene node needs to behave as an in-game Godot type, add a thin mod-local subclass first, then bind the scene to that subclass

---

## Wrapper Pattern

Do not bind scenes directly to game types such as `NEnergyCounter`. Write a mod-local script:

```csharp
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace MyMod.Scripts
{
    public partial class MyEnergyCounter : NEnergyCounter
    {
    }
}
```

Bind the `.tscn` to `MyEnergyCounter`, not to `NEnergyCounter` directly.

The wrapper can be empty. Its purpose is to give the editor a local script type owned by your mod.

---

## Common Types That Need Wrapping

| Game type | Typical use |
|---|---|
| `NEnergyCounter` | Energy orb scene root |
| `NRestSiteCharacter` | Rest site character scene |
| `NCreatureVisuals` | Character visuals scene |
| `NSelectionReticle` | Selection reticle |
| `MegaLabel` | Label child control |

---

## Generic Binding Examples

Custom energy orb scene:

- Root script → `MyEnergyCounter : NEnergyCounter`
- Label child → `MyCounterLabel : MegaLabel`

Character visuals scene:

- Root script → `MyCreatureVisuals : NCreatureVisuals`

Rest site scene:

- Root script → `MyRestSiteCharacter : NRestSiteCharacter`

The point is not the class names — it is that bound scripts live in your mod assembly.

---

## Editor Rule

Whenever the Godot editor must open, serialize, or rebind a script in your mod scene, prefer a mod-local subclass.

Even when:

- You have no extra logic yet
- Inheritance is a single line
- The runtime type already exists in the game assembly

The wrapper is the compatibility layer between your scene and the editor.

---

## Runtime Script Registration

If your mod uses Godot C# scene scripts, call this during initialization:

```csharp
using System.Reflection;

RitsuLibFramework.EnsureGodotScriptsRegistered(
    Assembly.GetExecutingAssembly(),
    Logger);
```

This lets Godot’s script bridge discover and register C# scripts from your mod assembly.

Do this before content registration so scene scripts resolve reliably at runtime.

---

## Recommended Workflow

1. Pick the game-side base type you need
2. Add a thin mod-local `partial class` that inherits it
3. Bind the `.tscn` to that local script
4. In your entry point, call `EnsureGodotScriptsRegistered(Assembly.GetExecutingAssembly(), Logger)`

---

## When You Do Not Need Wrapping

You usually do not need extra wrapper subclasses for:

- Plain content model classes (card / relic / power / character)
- Pure C# helpers not used as Godot scripts
- Logic classes never bound to `.tscn` resources

This document is only about Godot scenes and script binding.

---

## Related Documents

- [Getting Started](GettingStarted.md)
- [Character & Unlock Scaffolding](CharacterAndUnlockScaffolding.md)
- [Asset Profiles & Fallbacks](AssetProfilesAndFallbacks.md)
- [Diagnostics & Compatibility](DiagnosticsAndCompatibility.md)
