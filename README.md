# STS2-RitsuLib

Shared framework library for Slay the Spire 2 mods.

Chinese README: [README.zh.md](README.zh.md)

RitsuLib gives mod authors a stable set of APIs for content registration, model identity, lifecycle hooks, persistence,
settings UI, localization, audio, UI extensions, and compatibility helpers. It is designed to sit beside the base game
API and other libraries such as [BaseLib](https://github.com/Alchyr/BaseLib-StS2).

Documentation site: https://sts2-ritsulib.ritsukage.com/

## Install

Reference the NuGet package in your mod project:

```xml
<PackageReference Include="STS2.RitsuLib" />
```

Then declare the runtime dependency in `mod_manifest.json`. For game API 0.105.x and newer, use the object form:

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

If your project does not use central package management, let your package manager or IDE choose the current compatible
package version instead of copying a pinned version from this README. Older game API branches use the matching
`STS2.RitsuLib.Compat.<api-version>` package.

## Runtime Package Choices

For normal mod development, reference one NuGet package from your project:

- `STS2.RitsuLib` for the current supported game API branch.
- `STS2.RitsuLib.Compat.<api-version>` when your mod intentionally targets an older Slay the Spire 2 API branch.

For players, [GitHub releases](https://github.com/BAKAOLC/STS2-RitsuLib/releases) may also provide
`STS2-RitsuLib.<version>.variant-pack.zip`. Use this asset, not the per-compat `*.github.zip` files, when you want one
installed `mods/STS2-RitsuLib/` folder that chooses the matching RitsuLib build for the running game. The root
`STS2-RitsuLib.dll` is a loader, and the real API-specific builds live under `lib/<api-version>/`.

Downstream mods still declare the runtime dependency by mod id. Match the manifest format to the game API branch you
target.

For 0.105.x and newer:

```json
{
  "dependencies": [
    { "id": "STS2-RitsuLib" }
  ]
}
```

For older branches:

```json
{
  "dependencies": [
    "STS2-RitsuLib"
  ]
}
```

The variant pack does not change your compile-time NuGet reference. It only changes how the runtime RitsuLib mod is
installed for users who need one folder to support multiple game API branches.

## Main Entry Points

- `RitsuLibFramework.CreateContentPack(modId)` for content, keywords, timeline entries, card piles, and top-bar buttons.
- `RitsuLibFramework.CreatePatcher(modId, patcherName)` for Harmony patches with RitsuLib diagnostics.
- `RitsuLibFramework.SubscribeLifecycle<TEvent>(...)` for framework and game lifecycle events.
- `RitsuLibFramework.GetDataStore(modId)` with `BeginModDataRegistration(modId)` for JSON-backed mod data.
- `RitsuLibFramework.RegisterModSettings(modId, configure)` for player-editable settings pages.

Start with the getting-started guide, then use the topic pages for the specific feature you are adding.

## Optional Analyzer

RitsuLib-style mods can also use the companion Roslyn analyzer:
[STS2-ModAnalyzers-RitsuLib](https://github.com/BAKAOLC/STS2-ModAnalyzers-RitsuLib) (`STS2.ModAnalyzers.RitsuLib`).

## License

MIT
