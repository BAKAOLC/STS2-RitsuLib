#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")" && pwd)"
cd "$ROOT"
uv run python "$ROOT/scripts/release_cli.py" "$@"
