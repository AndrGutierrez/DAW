#!/usr/bin/env bash
set -euo pipefail
if [ "$#" -eq 0 ]; then echo 'Usage: verify-image-size.sh IMAGE [IMAGE...]' >&2; exit 2; fi
limit=120000000
for image in "$@"; do
  # Layer sizes are uncompressed on both classic and containerd image stores.
  # Image inspect Size can instead report compressed content or store usage.
  bytes=$(docker history --no-trunc --human=false --format '{{.Size}}' "$image" | awk '
    { if ($0 !~ /^[0-9]+$/) { invalid=1; exit }; total += $0; count++ }
    END { if (invalid || !count) exit 2; printf "%.0f", total }
  ')
  printf '%s: %s uncompressed layer bytes (limit: strictly below %s bytes)\n' "$image" "$bytes" "$limit"
  if [ "$bytes" -ge "$limit" ]; then echo "Image exceeds the Phase 5 size requirement: $image" >&2; exit 1; fi
done
