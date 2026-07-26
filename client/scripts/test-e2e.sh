#!/usr/bin/env bash
# Runs the Playwright e2e suite. On machines without root access (e.g. some
# WSL setups), `playwright install --with-deps` can't install the browser's
# OS-level shared libraries (libnspr4, libnss3, libasound2). This script
# detects that case, fetches those libraries into a local user-writable
# cache with `apt-get download` + `dpkg-deb -x` (no root needed), and points
# LD_LIBRARY_PATH at them. On a machine that already has the libraries
# system-wide (or ran `sudo npx playwright install --with-deps`), this is a
# no-op.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
CLIENT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
CACHE_DIR="${PLAYWRIGHT_MISSING_LIBS_DIR:-$HOME/.cache/pw-missing-libs}"

find_chromium_binary() {
  find "$HOME/.cache/ms-playwright" -maxdepth 4 -type f \
    \( -iname "chrome-headless-shell" -o -iname "headless_shell" -o -iname "chrome" \) 2>/dev/null | head -1
}

has_missing_libs() {
  local bin="$1"
  [ -n "$bin" ] && ldd "$bin" 2>/dev/null | grep -q "not found"
}

CHROMIUM_BIN="$(find_chromium_binary)"

if has_missing_libs "$CHROMIUM_BIN"; then
  if [ ! -d "$CACHE_DIR/usr/lib/x86_64-linux-gnu" ]; then
    echo "Playwright's browser is missing shared libraries; fetching a local copy (no root needed)..." >&2
    TMP_DIR="$(mktemp -d)"
    trap 'rm -rf "$TMP_DIR"' EXIT
    (cd "$TMP_DIR" && apt-get download libnspr4 libnss3 libasound2t64)
    mkdir -p "$CACHE_DIR"
    for deb in "$TMP_DIR"/*.deb; do
      dpkg-deb -x "$deb" "$CACHE_DIR"
    done
  fi
  export LD_LIBRARY_PATH="$CACHE_DIR/usr/lib/x86_64-linux-gnu:${LD_LIBRARY_PATH:-}"
fi

cd "$CLIENT_DIR"
exec npx playwright test "$@"
