#!/usr/bin/env bash
set -euo pipefail
project_directory=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
demo_directory="$project_directory/Builds/Linux-Current"
if [[ ! -x "$demo_directory/SysARchitect.x86_64" ]]; then
    printf '%s\n' 'Build the current demo with Unity menu: SysARchitect > Build Linux Desktop Demo.' >&2
    exit 1
fi
cat "$demo_directory/BUILD-INFO.txt"
exec "$demo_directory/SysARchitect.x86_64" "$@"
