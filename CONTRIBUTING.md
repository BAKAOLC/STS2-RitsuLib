# Contributing to RitsuLib

[中文](CONTRIBUTING.zh.md)

Bug reports, fixes, reusable APIs, documentation, and translations are all welcome. Keep each contribution focused on
a single clear problem or use case.

## Issues and feature requests

Search existing issues first, then use the repository's issue templates. For bugs, include the game and RitsuLib versions, relevant mods, reproduction steps, expected behavior, and logs.
Runtime logs are in `logs/godot.log` under the game's user data directory:

- Windows: `%AppData%\SlayTheSpire2\logs\godot.log`
- macOS: `~/Library/Application Support/SlayTheSpire2/logs/godot.log`
- Linux: `~/.local/share/SlayTheSpire2/logs/godot.log`

Remove sensitive information before sharing them.

Before adding an API or making substantial changes, describe the consuming mod's needs and discuss the proposed
behavior with maintainers.

## Development setup

You'll need:

- a .NET SDK supporting `net9.0`;
- Node.js and npm, for the bundled log viewer;
- game assemblies matching the API target.

Open `STS2-RitsuLib.sln` in Rider (or Visual Studio with ReSharper) to use the repository's formatting and
inspections.

The project finds local game installations automatically. To point it at specific paths, create a `local.props` based
on [local.props.template](local.props.template):

- `Sts2Dir`: game installation directory for local testing.
- `Sts2ApiSignatureRoot`: optional root for versioned API signatures. Each `<root>/<Sts2ApiCompat>/` folder must contain
  `sts2.dll`, `0Harmony.dll`, and `SmartFormat.dll`. Omit this property to reference the installed game.

Close the game, then run the following from the repository root:

```powershell
dotnet build STS2-RitsuLib.sln
```

The default build generates the manifest, builds the viewer, and installs RitsuLib into
`<Sts2Dir>/mods/STS2-RitsuLib/`, replacing an existing variant pack with the local single-API build. RitsuLib is a
DLL-only mod and does not require PCK export.

## Code and API design

Follow [.editorconfig](.editorconfig) and the surrounding code. Keep identifiers and implementation comments in
English, and use localization for translated UI text. Implement game behavior through mod-owned code, patches, and
resources; treat game files and recovered source as read-only references.

Public and protected APIs are long-term compatibility commitments. Add APIs for concrete consumer needs and preserve
existing source and binary compatibility. Provide English and Chinese XML documentation covering usage, inputs,
outputs, and any lifecycle or ownership requirements.

Validate inputs and define failure behavior. For shared APIs, account for multiple mods, repeated calls, and resource
lifetime. Make registration conflicts and callback ordering explicit.

## Validation

Format changed C# files with the repository-configured ReSharper formatter and resolve all inspection findings.
Verify the build, verify the affected behavior in the game, and check the logs. For public API changes, also build and
test a consuming mod. Bug fixes should cover the original reproduction and relevant edge cases.

Changes affecting game API compatibility or package/manifest generation require sequential builds of all
`RitsuLibCompatTargets` declared in [STS2-RitsuLib.csproj](STS2-RitsuLib.csproj). Select each target with
`/p:Sts2ApiCompat=<version>` and provide matching reference assemblies. Only the latest API target installs into the
game directory.

For documentation changes, check links, examples, and consistency between languages. For documentation-site changes,
use the tool versions in [docs/package.json](docs/package.json) and [the docs workflow](.github/workflows/gh-pages.yml),
then run from `docs/`:

```powershell
pnpm install --frozen-lockfile
pnpm build
```

Preview the affected pages to check layout and navigation.

## Pull requests

Use [the PR template](.github/PULL_REQUEST_TEMPLATE.md) to explain the problem, the resulting behavior, and validation.
Include examples or screenshots where they help reviewers. State which game/API versions were tested and be honest
about checks you haven't finished.

Keep changes focused and update the corresponding English and Chinese documentation together. Leave local
configuration, game binaries, and generated output out of the diff. Coordinate version and release metadata changes
with maintainers.
