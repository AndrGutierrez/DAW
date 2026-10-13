#!/usr/bin/env bash
set -euo pipefail
root=$(cd "$(dirname "$0")/../.." && pwd)
fixture=$(mktemp -d)
trap 'rm -f "$fixture/docker" "$fixture/output"; rmdir "$fixture"' EXIT
cat > "$fixture/docker" <<'STUB'
#!/usr/bin/env bash
set -euo pipefail
if [ "$1" != history ]; then echo 'The gate must measure uncompressed layers, not inspect Size.' >&2; exit 9; fi
if [ "${FAKE_FAILURE:-0}" = 1 ]; then exit 8; fi
printf '%s\n' "$FAKE_LAYERS"
STUB
chmod +x "$fixture/docker"
export PATH="$fixture:$PATH"
assert_result() {
  local expected=$1 actual=0
  bash "$root/scripts/verify-image-size.sh" test-api > "$fixture/output" 2>&1 || actual=$?
  if [ "$actual" -ne "$expected" ]; then cat "$fixture/output"; echo "Expected $expected, received $actual" >&2; exit 1; fi
}
# Regression: a compressed inspect size below 120MB must not hide a 133MB image.
export FAKE_LAYERS=$'120000000\n12954585\n0'
assert_result 1
export FAKE_LAYERS=$'60000000\n59999999\n0'
assert_result 0
export FAKE_LAYERS=$'60000000\n60000000'
assert_result 1
export FAKE_LAYERS='not-a-byte-count'
assert_result 2
export FAKE_LAYERS=''
assert_result 2
export FAKE_FAILURE=1
assert_result 2
unset FAKE_FAILURE
printf 'Image budget regression checks passed.\n'
