#!/usr/bin/env bash
#
# Sube (o actualiza) la colección y el entorno de Postman a tu cuenta.
#
# Requisitos:
#   - POSTMAN_API_KEY            Obligatoria. Se crea en https://web.postman.co/settings/me/api-keys
#   - POSTMAN_WORKSPACE_ID       Opcional. Coloca la colección/entorno en un workspace concreto.
#   - POSTMAN_COLLECTION_UID     Opcional. Si existe, actualiza esa colección en vez de crear una nueva.
#   - POSTMAN_ENVIRONMENT_UID    Opcional. Si existe, actualiza ese entorno en vez de crear uno nuevo.
#
# Uso:
#   bash scripts/push-postman.sh
#
# Puedes definir las variables en tu .env (no versionado).

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
COLLECTION="$ROOT/postman/Cattle-Management.postman_collection.json"
ENVIRONMENT="$ROOT/postman/Daw.postman_environment.json"

if [ -f "$ROOT/.env" ]; then
  set -a
  # shellcheck disable=SC1091
  . "$ROOT/.env"
  set +a
fi

if [ -z "${POSTMAN_API_KEY:-}" ]; then
  echo "Error: define POSTMAN_API_KEY (ver https://web.postman.co/settings/me/api-keys)." >&2
  exit 1
fi

QUERY=""
if [ -n "${POSTMAN_WORKSPACE_ID:-}" ]; then
  QUERY="?workspace=${POSTMAN_WORKSPACE_ID}"
fi

# Construye el cuerpo {"collection": ...} | {"environment": ...} a partir del archivo.
build_body() {
  local key="$1" source="$2" target="$3"
  python3 - "$key" "$source" "$target" <<'PY'
import json
import sys

key, source, target = sys.argv[1], sys.argv[2], sys.argv[3]
payload = json.load(open(source, encoding="utf-8"))
payload.pop("_postman_id", None)
json.dump({key: payload}, open(target, "w", encoding="utf-8"))
PY
}

push() {
  local label="$1" key="$2" source="$3" uid="$4" base="$5"
  local tmp method url
  tmp="$(mktemp)"
  build_body "$key" "$source" "$tmp"

  if [ -n "$uid" ]; then
    method="PUT"
    url="${base}/${uid}${QUERY}"
    echo "Actualizando ${label} ${uid}..."
  else
    method="POST"
    url="${base}${QUERY}"
    echo "Creando ${label}..."
  fi

  local response
  response="$(curl -sS -X "$method" "$url" \
    -H "X-Api-Key: ${POSTMAN_API_KEY}" \
    -H "Content-Type: application/json" \
    --data @"$tmp")"
  rm -f "$tmp"
  echo "$response"
}

push "la colección" "collection" "$COLLECTION" "${POSTMAN_COLLECTION_UID:-}" "https://api.getpostman.com/collections"
if [ -f "$ENVIRONMENT" ]; then
  push "el entorno" "environment" "$ENVIRONMENT" "${POSTMAN_ENVIRONMENT_UID:-}" "https://api.getpostman.com/environments"
fi
echo
