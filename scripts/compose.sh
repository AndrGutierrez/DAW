#!/usr/bin/env bash
set -euo pipefail
if docker compose version >/dev/null 2>&1; then
  exec docker compose "$@"
elif command -v docker-compose >/dev/null 2>&1; then
  exec docker-compose "$@"
elif [ -x '/c/Program Files/Docker/Docker/resources/cli-plugins/docker-compose.exe' ]; then
  exec '/c/Program Files/Docker/Docker/resources/cli-plugins/docker-compose.exe' "$@"
else
  echo 'Docker Compose is not available. Install the Docker Compose plugin.' >&2
  exit 1
fi
