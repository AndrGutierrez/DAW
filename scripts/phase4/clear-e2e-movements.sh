#!/usr/bin/env bash
set -euo pipefail
# Test fixture cleanup only. Never execute against the demo or production database.
[[ "${E2E_ISOLATED_DATABASE:-}" == "1" ]] || { printf '%s\n' 'A disposable E2E database is required.' >&2; exit 1; }
[[ $# -gt 0 ]] || exit 0
if [[ "${OS:-}" == "Windows_NT" ]]; then
  docker_bin='/c/Program Files/Docker/Docker/resources/bin/docker.exe'
else
  docker_bin='docker'
fi
export MSYS_NO_PATHCONV=1
project="$("$docker_bin" inspect --format '{{ index .Config.Labels "com.docker.compose.project" }}' daw-phase4-empty-db)"
[[ "$project" == 'daw-phase4-empty' ]] || { printf '%s\n' 'The PostgreSQL container is not the named disposable project.' >&2; exit 1; }
ids=''
for id in "$@"; do
  [[ "$id" =~ ^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$ ]] || { printf '%s\n' 'Invalid fixture identifier.' >&2; exit 1; }
  ids="${ids}${ids:+,}'$id'"
done
"$docker_bin" exec daw-phase4-empty-db psql -X -v ON_ERROR_STOP=1 -U "${E2E_POSTGRES_USER:?}" -d "${E2E_POSTGRES_DB:?}" -c "DELETE FROM \"AnimalMovements\" WHERE \"AnimalId\" IN ($ids);" >/dev/null
