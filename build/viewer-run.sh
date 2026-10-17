#!/usr/bin/env bash
# SPDX-License-Identifier: GPL-3.0-or-later
set -euo pipefail
app=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
exec "$app/PrePoMax.Avalonia" "$@"
