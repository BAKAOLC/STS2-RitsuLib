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
- `Sts2ApiSignatureRoot`: root for versioned API signatures. Each selected target and the shared baseline target
  require `sts2.dll`, `0Harmony.dll`, and `SmartFormat.dll` under `<root>/<api-version>/`. Use this for reproducible
  compatibility builds. Omitting it references the installed game and does not establish compatibility with older APIs.

Close the game, then run the following from the repository root:

```powershell
dotnet build STS2-RitsuLib.sln
```

The default build generates the manifest, builds the viewer, and installs RitsuLib into
`<Sts2Dir>/mods/STS2-RitsuLib/`, replacing an existing variant pack with the local single-API build. RitsuLib is a
DLL-only mod and does not require PCK export.

Use `Debug` for development and `Release` for distributable builds. The solution and every component share these two
configurations; the old Godot export configurations and project files have been removed. Build settings and output
paths are centralized in `build/RitsuLib.Build.props`, with explicit GodotSharp/source-generator references in
`Directory.Build.targets`.

```powershell
uv run python scripts/build_cli.py build
uv run python scripts/build_cli.py build --compat-targets all
uv run python scripts/build_cli.py pack --compat-targets 0.109.0
uv run python scripts/build_cli.py bundle
```

`build` defaults to Debug and the latest target; `pack` defaults to Release and the latest target; `bundle` defaults to
Release and every declared target. All accept `--configuration`, `--compat-targets`, `--signature-root`, and `--game-dir`.
Builds retain normal copy-to-game behavior for the latest target. Pack and bundle produce distribution artifacts without
installing them. Compatibility targets run sequentially.

| Output | Location |
| --- | --- |
| Facade and resolved compile dependencies | `artifacts/bin/<configuration>/<api>/` |
| Component build outputs | `artifacts/modules/<configuration>/shared/<project>/` and `compat/<api>/` |
| Intermediate files | `artifacts/obj/<project>/<configuration>/<api>/` |
| Complete single-target installation | `artifacts/runtime/<configuration>/<api>/` |
| Complete combined installation | `artifacts/bundle/<configuration>/` |
| NuGet and symbol packages | `artifacts/packages/<configuration>/nuget/` |
| Single-target and bundle ZIPs | `artifacts/packages/<configuration>/github/` |

Single-target outputs survive builds of other targets. Shared modules always use the baseline API; the combined
installation stores them once. Compile-output DLLs and the installation-root loader have different roles: use NuGet or
the installation's `RitsuLib.References.props` for consumers, and install the complete runtime directory for players.

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
`RitsuLibCompatTargets` declared in [build/RitsuLib.Compatibility.props](build/RitsuLib.Compatibility.props). Select each target with
`/p:Sts2ApiCompat=<version>` and provide matching reference assemblies. Only the latest API target installs into the
game directory.

Module ownership is declared in [build/RitsuLib.Modules.items](build/RitsuLib.Modules.items). Shared contains stable
contracts and infrastructure; Ui depends on Shared; Settings depends on Shared and Ui; Runtime adds version-specific
game integration. The root project is a generated type-forwarding facade. Shared, Ui, Settings, and the loader compile
against `RitsuLibSharedApiCompat`, independently of the selected Runtime target. Keep version-varying calls in Runtime
behind internal host contracts, and keep reusable UI independent of settings registration.

Normal builds deploy a complete single-target module tree. Artifact builds and CI package that same tree, combine
variants only when shared modules match byte for byte, and validate manifests, hashes, and XML documentation.
Run `uv run python scripts/ci/ci_build.py --signature-root <root>` to exercise the artifact workflow locally;
it does not install into the live game directory.

For documentation changes, check links, examples, and consistency between languages. For documentation-site changes,
use the tool versions in [docs/package.json](docs/package.json) and [the docs workflow](.github/workflows/gh-pages.yml),
then run from `docs/`:

```powershell
pnpm install --frozen-lockfile
pnpm build
```

Preview the affected pages to check layout and navigation.

## Pull requests

Prefer `dev` as the PR merge target (base branch), unless maintainers specify another target.

Use [the PR template](.github/PULL_REQUEST_TEMPLATE.md) to explain the problem, the resulting behavior, and validation.
Include examples or screenshots where they help reviewers. State which game/API versions were tested and be honest
about checks you haven't finished.

Keep changes focused and update the corresponding English and Chinese documentation together. Leave local
configuration, game binaries, and generated output out of the diff. Coordinate version and release metadata changes
with maintainers.
