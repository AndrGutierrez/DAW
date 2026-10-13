#!/usr/bin/env bash
set -euo pipefail
if [ "$#" -eq 0 ]; then echo 'Usage: verify-image-size.sh IMAGE [IMAGE...]' >&2; exit 2; fi
limit=120000000
for image in "$@"; do
  bytes=$(docker image inspect "$image" --format '{{.Size}}')
  printf '%s: %s bytes (limit: strictly below %s bytes)\n' "$image" "$bytes" "$limit"
  if [ "$bytes" -ge "$limit" ]; then echo "Image exceeds the Phase 5 size requirement: $image" >&2; exit 1; fi
done
