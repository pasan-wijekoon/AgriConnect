"""Farmer sets / edits a floor price -> Buyer sees it -> anomaly queue reacts. Live API, Carrots (Kandy) listing.
Restores the original price at the end. Passwords are read from the seed comment and never printed."""
import json, re, sys, urllib.request, urllib.error

BASE = "http://localhost:5000/api"
SEED = r"C:\Users\Yashodha\OneDrive\Documents\GitHub\AgriConnect\backend\src\config\AgriConnectDbContext.cs"
PW = re.search(r'Demo users \(password: "([^"]+)"', open(SEED, encoding="utf-8").read()).group(1)


def call(method, path, token=None, body=None):
    req = urllib.request.Request(BASE + path, method=method, data=json.dumps(body).encode() if body is not None else None)
    req.add_header("Content-Type", "application/json")
    if token:
        req.add_header("Authorization", "Bearer " + token)
    try:
        with urllib.request.urlopen(req, timeout=60) as r:
            raw = r.read().decode()
            return r.status, (json.loads(raw) if raw else None)
    except urllib.error.HTTPError as e:
        raw = e.read().decode()
        try:
            return e.code, json.loads(raw)
        except Exception:
            return e.code, raw[:160]


results = []


def check(name, ok, detail=""):
    results.append((name, ok, detail))
    print(f"{'PASS' if ok else 'FAIL'}  {name}  {detail}")


def login(email):
    return call("POST", "/auth/login", body={"email": email, "password": PW})[1]["token"]


farmer, farmer2, buyer, officer = (login(e) for e in ("farmer@agriconnect.lk", "farmer2@agriconnect.lk", "buyer@agriconnect.lk", "officer@agriconnect.lk"))

st, mine = call("GET", "/listings/my?pageSize=100", farmer)
items = mine["items"] if isinstance(mine, dict) else mine
tom = next(l for l in items if l["cropName"] == "Carrots" and l["status"] == "Published")
lid, original = tom["id"], tom.get("minPrice")
check("Farmer finds the published Carrots listing", True, f"status {tom['status']}, price {original}")


def buyer_view():
    _, pub = call("GET", "/listings?status=Published&pageSize=50", buyer)
    return next((l for l in pub["items"] if l["id"] == lid), None)


# --- farmer edits the price ----------------------------------------------------------------
st, upd = call("PUT", f"/listings/{lid}", farmer, {"minPrice": 120})
check("Farmer saves a floor price of 120", st == 200 and upd["minPrice"] == 120, f"status {st}")
check("The listing stays Published after the edit", st == 200 and upd["status"] == "Published", f"status '{upd.get('status') if isinstance(upd, dict) else upd}'")
v = buyer_view()
check("Buyer still sees it, now with the price", v is not None and v.get("minPrice") == 120, f"price seen by buyer: {v.get('minPrice') if v else 'listing gone'}")

# --- invalid / forbidden edits -------------------------------------------------------------
for label, price in (("zero", 0), ("negative", -5), ("above the maximum", 1_000_000)):
    st, _ = call("PUT", f"/listings/{lid}", farmer, {"minPrice": price})
    check(f"A price that is {label} is refused", st == 400, f"status {st}")
st, _ = call("PUT", f"/listings/{lid}", farmer2, {"minPrice": 130})
check("Another farmer cannot edit this listing", st in (403, 404), f"status {st}")
st, _ = call("PUT", f"/listings/{lid}", buyer, {"minPrice": 130})
check("A buyer cannot edit a listing", st == 403, f"status {st}")
check("Price is unchanged after the refused edits", buyer_view().get("minPrice") == 120, f"price {buyer_view().get('minPrice')}")

# --- Component D: does changing the price re-check it for anomalies? -----------------------
_, before = call("GET", "/analytics/anomalies?size=100", officer)
st, _ = call("PUT", f"/listings/{lid}", farmer, {"minPrice": 99999})
_, after = call("GET", "/analytics/anomalies?size=100", officer)
flag = next((f for f in after["items"] if f["listingId"] == lid and f["status"] == "Open"), None)
check("D integration: raising the price far above fair puts the new price on the listing's anomaly flag",
      flag is not None and flag["listingPrice"] == 99999,
      f"flag price {flag['listingPrice'] if flag else 'no open flag'}, deviation {flag['deviationPercent'] if flag else '-'}%, flags before {before['total']} / after {after['total']}")
_, again = call("GET", "/analytics/anomalies?size=100", officer)
check("There is still only one open flag for the listing", sum(1 for f in again["items"] if f["listingId"] == lid and f["status"] == "Open") == 1,
      f"{sum(1 for f in again['items'] if f['listingId'] == lid and f['status'] == 'Open')} open flag(s)")

# --- restore ---------------------------------------------------------------------------------
call("PUT", f"/listings/{lid}", farmer, {"minPrice": original if original else 120})
if flag:
    call("PATCH", f"/analytics/anomalies/{flag['id']}", officer, {"status": "Dismissed"})

passed = sum(1 for _, ok, _ in results if ok)
print(f"\nSUMMARY: {passed} passed, {len(results) - passed} failed, {len(results)} total")
json.dump([{"check": n, "pass": ok, "detail": d} for n, ok, d in results], open(sys.argv[1] if len(sys.argv) > 1 else "price_edit_results.json", "w"), indent=2)
