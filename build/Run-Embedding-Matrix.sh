#!/usr/bin/env bash
# SPDX-License-Identifier: GPL-3.0-or-later
set -euo pipefail
repo=$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)
# WSLg mounts its own read-only X0 socket directory. A private mount namespace
# gives Weston writable sockets without changing that mount or exposing a GUI.
exec unshare --mount --propagation private bash "$repo/build/Hidden-X11-Namespace.sh" "$repo" "$@"
