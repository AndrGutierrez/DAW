"""Verify production transactions against a disposable PostgreSQL-backed API.

Uses only Python's standard library. Required environment variables:
DAW_VERIFY_BASE_URL, DAW_VERIFY_ADMIN_PASSWORD. This script creates test records.
"""
import concurrent.futures
import datetime
import json
import os
import threading
import urllib.error
import urllib.request
import uuid

base = os.environ["DAW_VERIFY_BASE_URL"].rstrip("/")
password = os.environ["DAW_VERIFY_ADMIN_PASSWORD"]
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


status, auth = request("POST", "/api/auth/login", {"username": os.environ.get("DAW_VERIFY_ADMIN_USERNAME", "admin"), "password": password})
assert status == 200, "Admin login failed"
token = auth["accessToken"]
status, farms = request("GET", "/api/farms")
farm_id = next(row["id"] for row in farms if row["data"]["code"] == "DEMO")
status, species = request("GET", "/api/species")
species_id = next(row["id"] for row in species if row["data"]["code"] == "BO")
today = datetime.datetime.now(datetime.timezone.utc).date().isoformat()


def animal():
    status, row = request("POST", "/api/animals", {"farmId": farm_id, "speciesId": species_id, "internalTag": "RACE-" + uuid.uuid4().hex[:12], "sex": "Female", "purpose": "DualPurpose"})
    assert status == 201, row
    return row["id"]


def production(animal_id, operation_id, product="Meat", method="Slaughter"):
    return {"farmId": farm_id, "animalId": animal_id, "date": today, "productType": product, "method": method,
            "quantity": 100 if product == "Meat" else 1, "unit": "Kilogram" if product == "Meat" else "Liter" if product == "Milk" else "Unit", "operationId": operation_id}


def race(first, second):
    barrier = threading.Barrier(2)

    def send(body):
        barrier.wait(timeout=5)
        return request("POST", "/api/production", body)

    with concurrent.futures.ThreadPoolExecutor(max_workers=2) as pool:
        futures = [pool.submit(send, body) for body in (first, second)]
        replies = [future.result(timeout=40) for future in futures]
    assert sorted(status for status, _ in replies) == [201, 409], replies
    return [status for status, _ in replies]


results = {}
first_animal = animal()
results["two_slaughters_one_animal"] = race(production(first_animal, str(uuid.uuid4())), production(first_animal, str(uuid.uuid4())))
_, rows = request("GET", "/api/production")
assert sum(row["data"]["animalId"] == first_animal for row in rows) == 1
_, state = request("GET", "/api/animals/" + first_animal)
assert state["status"] == "Dead"

animal_a, animal_b = animal(), animal()
operation = str(uuid.uuid4())
results["one_operation_two_animals"] = race(production(animal_a, operation), production(animal_b, operation, "Hide"))
_, rows = request("GET", "/api/production")
assert sum(row["data"]["operationId"] == operation for row in rows) == 1
states = [request("GET", "/api/animals/" + animal_id)[1]["status"] for animal_id in (animal_a, animal_b)]
assert sorted(states) == ["Active", "Dead"], states

milk_animal = animal()
operation = str(uuid.uuid4())
results["duplicate_yield"] = race(production(milk_animal, operation, "Milk", "Milking"), production(milk_animal, operation, "Milk", "Milking"))
_, rows = request("GET", "/api/production")
assert sum(row["data"]["operationId"] == operation for row in rows) == 1

_, categories = request("GET", "/api/categories")
status, product = request("POST", "/api/products", {"sku": "ZERO-" + uuid.uuid4().hex[:8], "name": "Zero sentinel test", "categoryId": categories[0]["id"], "price": 12.34, "costPrice": 10.01, "unit": "Kilogram"})
assert status == 201 and product["data"]["unit"] == "Kilogram", product
status, stock = request("POST", "/api/inventory", {"farmId": farm_id, "productId": product["id"], "stock": 0, "minStock": 0, "maxStock": 10})
assert status == 201 and stock["data"]["minStock"] == 0, stock
_, saved = request("GET", "/api/inventory/" + stock["id"])
assert saved["data"]["minStock"] == 0
assert request("DELETE", "/api/inventory/" + stock["id"])[0] == 204
assert request("DELETE", "/api/products/" + product["id"])[0] == 204
results["explicit_zero_and_kilogram_preserved"] = True
print(json.dumps({"passed": True, "scenarios": results}, indent=2))
