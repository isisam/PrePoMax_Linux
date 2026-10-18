#!/usr/bin/env bash
# SPDX-License-Identifier: GPL-3.0-or-later
set -euo pipefail
repo=$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)
export PYTHONPATH="$repo/tests/L2b" PPM_MATRIX_BACKENDS=generic PPM_MATRIX_SCALES=1,2
exec unshare --mount --propagation private bash "$repo/build/L2c-X11-Namespace.sh" "$repo" "$@"
