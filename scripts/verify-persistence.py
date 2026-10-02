"""Verify a created and updated record survives an externally managed API restart.

Use only against a disposable PostgreSQL-backed API. Set DAW_VERIFY_BASE_URL
and DAW_VERIFY_ADMIN_PASSWORD. Run prepare, restart the API, then run verify.
The checkpoint stores an identifier and expected data, never credentials/tokens.
"""
import json
import os
import pathlib
import sys
import urllib.error
import urllib.request
import uuid

mode, checkpoint_path = sys.argv[1:]
assert mode in ("prepare", "verify"), "Expected prepare or verify"
checkpoint = pathlib.Path(checkpoint_path)
base = os.environ["DAW_VERIFY_BASE_URL"].rstrip("/")
token = None


def request(method, path, body=None):
    headers = {"Content-Type": "application/json"}
    if token:
        headers["Authorization"] = "Bearer " + token
    req = urllib.request.Request(base + path, None if body is None else json.dumps(body).encode(), headers, method=method)
    try:
        response = urllib.request.urlopen(req, timeout=30)
    except urllib.error.HTTPError as error:
        response = error
    with response:
        raw = response.read()
        return response.status, json.loads(raw) if raw else None


status, auth = request("POST", "/api/auth/login", {
    "username": os.environ.get("DAW_VERIFY_ADMIN_USERNAME", "admin"),
    "password": os.environ["DAW_VERIFY_ADMIN_PASSWORD"],
})
assert status == 200, "Admin login failed"
token = auth["accessToken"]
if mode == "prepare":
    data = {"name": "Persistence before restart", "code": "PERSIST-" + uuid.uuid4().hex[:10]}
    status, row = request("POST", "/api/farms", data)
    assert status == 201, row
    data["name"] = "Persistence updated before restart"
    status, updated = request("PUT", "/api/farms/" + row["id"], data)
    assert status == 200 and updated["data"]["name"] == data["name"], updated
    checkpoint.write_text(json.dumps({"baseUrl": base, "id": row["id"], "name": data["name"]}), encoding="utf-8")
    print(json.dumps({"prepared": True, "id": row["id"], "created": 201, "updated": 200}))
else:
    expected = json.loads(checkpoint.read_text(encoding="utf-8"))
    assert expected["baseUrl"] == base, "The API URL differs from the checkpoint"
    status, row = request("GET", "/api/farms/" + expected["id"])
    assert status == 200 and row["data"]["name"] == expected["name"], row
    assert request("DELETE", "/api/farms/" + expected["id"])[0] == 204
    print(json.dumps({"passed": True, "id": expected["id"], "readAfterRestart": status, "updatedValuePreserved": True, "cleanup": 204}))
