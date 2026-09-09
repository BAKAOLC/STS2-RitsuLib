# STS2-RitsuLib

[![Dev build](https://github.com/BAKAOLC/STS2-RitsuLib/actions/workflows/dev-build.yml/badge.svg?branch=dev)](https://github.com/BAKAOLC/STS2-RitsuLib/actions/workflows/dev-build.yml)
[![Release](https://img.shields.io/github/v/release/BAKAOLC/STS2-RitsuLib)](https://github.com/BAKAOLC/STS2-RitsuLib/releases)
[![NuGet](https://img.shields.io/nuget/v/STS2.RitsuLib)](https://www.nuget.org/packages/STS2.RitsuLib)
[![License](https://img.shields.io/github/license/BAKAOLC/STS2-RitsuLib)](LICENSE)

Shared framework library for Slay the Spire 2 mods.

[Documentation](https://sts2-ritsulib.ritsukage.com/) |
[Releases](https://github.com/BAKAOLC/STS2-RitsuLib/releases) |
[Chinese README](README.zh.md)

RitsuLib gives mod authors a stable layer for content registration, lifecycle hooks, Harmony patching, persistence,
settings UI, localization, audio, runtime UI, diagnostics, and compatibility helpers. It sits beside the base game API
and other libraries such as [BaseLib](https://github.com/Alchyr/BaseLib-StS2) instead of replacing them.

## What It Covers

| Area                | Examples                                                                                         |
|---------------------|--------------------------------------------------------------------------------------------------|
| Content authoring   | Cards, relics, potions, characters, events, encounters, timelines, unlocks, keywords, tags       |
| Runtime integration | Lifecycle events, patch helpers, Godot script registration, custom target types, runtime hotkeys |
| Data and settings   | JSON-backed stores, run-saved data, migrations, player-facing settings pages                     |
| Presentation        | FMOD helpers, top-bar buttons, card piles, toast messages, shell themes, export helpers          |
| Compatibility       | API capability gates, diagnostics, startup audits, analyzer-friendly conventions                 |

```mermaid
flowchart LR
    Mod[Your mod] --> NuGet[STS2.RitsuLib NuGet]
    Mod --> Manifest[mod_manifest dependency]
    Manifest --> Runtime[STS2-RitsuLib runtime mod]
    Runtime --> Registries[Content and UI registries]
    Runtime --> Lifecycle[Lifecycle and patching]
    Runtime --> Data[Persistence and settings]
    Runtime --> Diagnostics[Diagnostics and compatibility]
```

## Install

Add the NuGet package to your mod project:

```xml
<PackageReference Include="STS2.RitsuLib" />
```

Then declare the runtime dependency in `mod_manifest.json`.

For game API 0.105.x and newer, use the object form:

```json
{
  "dependencies": [
    { "id": "STS2-RitsuLib" }
  ]
}
```

For older game API branches, use the legacy string form because older manifest parsers may fail on dependency objects:

```json
{
  "dependencies": [
    "STS2-RitsuLib"
  ]
}
```

If your project does not use Central Package Management, let your package manager or IDE choose the current compatible
package version instead of copying a pinned version from this README.

## Package Choices

| Scenario                                                   | Compile-time package                  | Runtime install                            |
|------------------------------------------------------------|---------------------------------------|--------------------------------------------|
| Highest supported game API, usually the game's beta branch | `STS2.RitsuLib`                       | `STS2-RitsuLib` from GitHub releases       |
| Stable or older game API branch                            | `STS2.RitsuLib.Compat.<api-version>`  | Matching release asset or variant pack     |
| Player needs one folder for several API branches           | Your mod still references one package | `STS2-RitsuLib.<version>.variant-pack.zip` |

Every runtime package installs one complete `mods/STS2-RitsuLib/` folder. Its root `STS2-RitsuLib.dll` is the loader.
Common code, reusable UI, and settings live in `shared/`; the compatibility facade and game integration live in
`compat/<api-version>/`. A variant pack includes several API versions while storing shared modules once. Install the
whole folder, including both subdirectories and the module manifest; copying only the root DLL is insufficient.

Images, translations, and bundled themes are distributed in the installation's `assets.zip`, shared across game
versions. Keep this archive with the runtime. Development Debug builds use a loose `assets/` directory instead.

Your NuGet package reference and existing namespaces remain supported. NuGet supplies all compile-time modules, and the
compatibility facade forwards existing public types for previously compiled mods. Manual assembly references must
include the facade, Runtime, Shared, Ui, and Settings DLLs from the same package. Reflection that enumerates a single
assembly must account for types now belonging to separate assemblies.

For directory references, import the installation's `RitsuLib.References.props` to add all five assembly references. A
single-version installation selects its only target automatically. For a bundle, set the compile-time target first:

```xml
<PropertyGroup>
    <RitsuLibReferenceTarget>0.111.0</RitsuLibReferenceTarget>
</PropertyGroup>
<Import Project="path/to/STS2-RitsuLib/RitsuLib.References.props" />
```

These references do not copy the framework into your mod directory; install the complete runtime separately.

Reusable controls, layout containers, shell themes, and toasts are provided by the UI module without a Settings assembly
dependency. New general-purpose entry points include `RitsuControlFactory`, `RitsuVerticalStack`, and
`RitsuFixedWidthScrollContent`; existing `ModSettings*` control names remain available for compatibility.

The main `STS2.RitsuLib` package follows the highest Slay the Spire 2 API supported by this repository. Because the
game's highest API is often on a beta branch, use a compat package when your mod is meant for another public game
branch.

## Main Entry Points

Most mods start from these APIs:

| Need                                                               | Use                                                                           |
|--------------------------------------------------------------------|-------------------------------------------------------------------------------|
| Register models, keywords, epochs, card piles, and top-bar buttons | `RitsuLibFramework.CreateContentPack(modId)`                                  |
| Patch game methods with diagnostics                                | `RitsuLibFramework.CreatePatcher(modId, patcherName)`                         |
| React to framework or game timing                                  | `RitsuLibFramework.SubscribeLifecycle<TEvent>(...)`                           |
| Store profile or account data                                      | `RitsuLibFramework.BeginModDataRegistration(modId)` and `GetDataStore(modId)` |
| Store run-scoped data                                              | `RitsuLibFramework.GetRunSavedDataStore(modId)`                               |
| Add player-editable settings pages                                 | `RitsuLibFramework.RegisterModSettings(modId, configure)`                     |

Minimal content-pack registration:

```csharp
RitsuLibFramework.CreateContentPack("MyMod")
    .Card<MyCardPool, MyStrike>()
    .Relic<MyRelicPool, MyStarterRelic>()
    .Apply();
```

Start with the [getting-started guide](https://sts2-ritsulib.ritsukage.com/guide/getting-started), then use the topic
pages for the feature you are adding.

## Documentation

| Topic                         | Link                                                                    |
|-------------------------------|-------------------------------------------------------------------------|
| Getting started               | https://sts2-ritsulib.ritsukage.com/guide/getting-started               |
| Content authoring             | https://sts2-ritsulib.ritsukage.com/guide/content-authoring-toolkit     |
| Lifecycle events              | https://sts2-ritsulib.ritsukage.com/guide/lifecycle-events              |
| Patching                      | https://sts2-ritsulib.ritsukage.com/guide/patching-guide                |
| Persistence                   | https://sts2-ritsulib.ritsukage.com/guide/persistence-guide             |
| Mod settings                  | https://sts2-ritsulib.ritsukage.com/guide/mod-settings                  |
| Diagnostics and compatibility | https://sts2-ritsulib.ritsukage.com/guide/diagnostics-and-compatibility |

RitsuLib's own docs are concise feature references. For a broader Chinese walkthrough of Slay the Spire 2 modding, use:

[SlayTheSpire2 Modding Tutorials](https://glitchedreme.github.io/SlayTheSpire2ModdingTutorials/index.html)

## Related Libraries

For minion, summon, companion-card, or guardian-style mechanics, prefer
[MinionLib](https://github.com/FuYnAloft/MinionLib). It focuses on creating and summoning minions, minion actions,
minion-card interactions, guardian behavior, custom targeting, and minion positioning. RitsuLib remains the general
framework layer and does not try to replace that specialized library.

## Optional Analyzer

The old companion analyzer
[STS2-ModAnalyzers-RitsuLib](https://github.com/BAKAOLC/STS2-ModAnalyzers-RitsuLib)
(`STS2.ModAnalyzers.RitsuLib`) is archived and no longer maintained.

For RitsuLib-style mods, the recommended optional analyzer is
[STS2RitsuLibModAnalyzers](https://github.com/Miooowo/STS2-RitsuLibModAnalyzers)
(`Miooowo.STS2RitsuLib.ModAnalyzers`). It provides Roslyn diagnostics for RitsuLib localization and resource paths, and
its package can automatically pass common project files to the analyzer through `buildTransitive`.

This analyzer is provided, maintained, and supported by a third party. RitsuLib does not guarantee that it fully matches
current RitsuLib capabilities or that all analyzer behavior is correct.

## Contributing

See the [contribution guide](CONTRIBUTING.md) for development setup, code and public API conventions, validation, and
pull request preparation.

The library uses standard .NET `Debug` and `Release` configurations. GodotSharp and its source generators are explicit
dependencies; building RitsuLib does not require a Godot project, editor, or export workflow.

## Acknowledgements

See [ACKNOWLEDGEMENTS.md](ACKNOWLEDGEMENTS.md) for the people and users who helped shape RitsuLib.

## License

MIT
