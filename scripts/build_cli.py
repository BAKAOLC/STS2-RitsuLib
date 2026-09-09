from __future__ import annotations

import argparse
import subprocess
from pathlib import Path

from release_lib.bundle import compose_bundle_zip
from release_lib.msbuild_eval import get_csproj_property
from release_lib.nuget import build_artifacts
from release_lib.repo_layout import RITSULIB_CSPROJ_NAME, bundle_staging
from release_lib.version_sync import read_csproj_version


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="Build, package, or bundle RitsuLib without release or Git operations.")
    parser.add_argument("command", choices=("build", "pack", "bundle"))
    parser.add_argument("--configuration", choices=("Debug", "Release"))
    parser.add_argument("--compat-targets", help="latest, all, or a comma-separated list of declared API versions")
    parser.add_argument("--signature-root", type=Path)
    parser.add_argument("--game-dir", type=Path)
    args = parser.parse_args(argv)
    repo = Path(__file__).resolve().parents[1]
    project = repo / RITSULIB_CSPROJ_NAME
    configuration = args.configuration or ("Debug" if args.command == "build" else "Release")
    declared = get_csproj_property(project, "RitsuLibCompatTargets").split(";")
    latest = get_csproj_property(project, "RitsuLibLatestApiCompat")
    selection = args.compat_targets or ("all" if args.command == "bundle" else "latest")
    targets = declared if selection == "all" else [latest] if selection == "latest" else selection.split(",")
    if not targets or len(set(targets)) != len(targets) or any(target not in declared for target in targets):
        parser.error("Select distinct versions from RitsuLibCompatTargets: " + ", ".join(declared))
    signature_root = args.signature_root.resolve() if args.signature_root else None
    game_dir = args.game_dir.resolve() if args.game_dir else None
    if args.command == "build":
        for target in targets:
            command = ["dotnet", "build", str(project), "-c", configuration, f"/p:Sts2ApiCompat={target}"]
            if signature_root:
                command.append(f"/p:Sts2ApiSignatureRoot={signature_root}")
            if game_dir:
                command.append(f"/p:Sts2Dir={game_dir}")
            subprocess.run(command, cwd=repo, check=True)
        return 0
    staging = repo / bundle_staging(configuration) if args.command == "bundle" else None
    packages, archives = build_artifacts(
        repo,
        configuration=configuration,
        skip_build=False,
        compat_targets=targets,
        sts2_api_signature_root=signature_root,
        sts2_dir=game_dir,
        bundle_staging_root=staging,
    )
    for artifact in [*packages, *archives]:
        print(artifact)
    if staging is not None:
        print(compose_bundle_zip(
            repo,
            configuration=configuration,
            effective_version=read_csproj_version(project),
            sts2_api_signature_root=signature_root,
            sts2_dir=game_dir,
            bundle_staging_root=staging,
        ))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
