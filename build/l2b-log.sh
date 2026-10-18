#!/usr/bin/env bash
# SPDX-License-Identifier: GPL-3.0-or-later
set -euo pipefail
repo=$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)
# Windows worktree gitdir paths are intentionally kept native. Never rewrite .git.
git_repo() {
  if [[ -f "$repo/.git" ]] && grep -q 'gitdir: [A-Za-z]:' "$repo/.git"; then
    git.exe -C "$(wslpath -w "$repo")" "$@" | tr -d '\r'
  else git -C "$repo" "$@"; fi
}
log=${1:?log path}; shift
mkdir -p "$(dirname "$log")"
{
  date -u --iso-8601=seconds
  echo "git rev: $(git_repo rev-parse HEAD)"
  echo 'git status --short:'
  git_repo status --short
  printf 'command:'; printf ' %q' "$@"; echo
  set +e
  "$@"
  result=$?
  echo "exit=$result"
  echo "git rev at end: $(git_repo rev-parse HEAD)"
  echo 'git status --short at end:'
  git_repo status --short
  exit "$result"
} > "$log" 2>&1
