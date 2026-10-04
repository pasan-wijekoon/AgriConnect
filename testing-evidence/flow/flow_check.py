"""Full role/workflow check against the live API. Passwords are read from the seed comment at
runtime so they are never written into this script or printed."""
import json, re, sys, urllib.request, urllib.error, datetime as dt

BASE = "http://localhost:5000/api"
SEED = r"C:\Users\Yashodha\OneDrive\Documents\GitHub\AgriConnect\backend\src\config\AgriConnectDbContext.cs"
m = re.search(r'Demo users \(password: "([^"]+)"; the admin uses "([^"]+)"\)', open(SEED, encoding="utf-8").read())
DEMO_PW, ADMIN_PW = m.group(1), m.group(2)
ACCOUNTS = {
    "Farmer": ("farmer@agriconnect.lk", DEMO_PW),
    "Buyer": ("buyer@agriconnect.lk", DEMO_PW),
    "Officer": ("officer@agriconnect.lk", DEMO_PW),
    "Administrator": ("admin@agriconnect.lk", ADMIN_PW),
}


def call(method, path, token=None, body=None):
    req = urllib.request.Request(BASE + path, method=method, data=json.dumps(body).encode() if body is not None else None)
    req.add_header("Content-Type", "application/json")
    if token:
        req.add_header("Authorization", f"Bearer {token}")
    try:
        with urllib.request.urlopen(req, timeout=30) as r:
            raw = r.read().decode()
            return r.status, (json.loads(raw) if raw else None)
    except urllib.error.HTTPError as e:
        raw = e.read().decode()
        try:
            return e.code, json.loads(raw)
        except Exception:
            return e.code, raw[:200]


results = []


def check(case, ok, detail=""):
    results.append((case, ok, detail))
    print(f"{'PASS' if ok else 'FAIL'}  {case}  {detail}")


tokens = {}
print("== 1. Login (real JWT) ==")
for role, (email, pw) in ACCOUNTS.items():
    st, body = call("POST", "/auth/login", body={"email": email, "password": pw})
    ok = st == 200 and isinstance(body, dict) and "token" in body
    check(f"login as {role}", ok, f"status {st}, api role '{body.get('role') if ok else '-'}'")
    if ok:
        tokens[role] = body["token"]

print("\n== 2. Component D access matrix (expected -> actual) ==")
st, filters = call("GET", "/analytics/filters", tokens.get("Farmer"))
crop = next(c for c in filters["crops"] if c.get("hasPriceHistory"))
trend = f"/analytics/price-trends?cropId={crop['id']}&from=2026-06-01&to=2026-12-31&bucket=week"
matrix = [
    ("GET filters", "GET", "/analytics/filters", {"Farmer": 200, "Buyer": 200, "Officer": 200, "Administrator": 200}),
    ("GET price-trends", "GET", trend, {"Farmer": 200, "Buyer": 200, "Officer": 200, "Administrator": 200}),
    ("GET anomalies", "GET", "/analytics/anomalies", {"Farmer": 403, "Buyer": 403, "Officer": 200, "Administrator": 200}),
    ("GET shortages", "GET", "/analytics/shortages", {"Farmer": 403, "Buyer": 403, "Officer": 200, "Administrator": 200}),
    ("POST snapshots/refresh", "POST", "/analytics/snapshots/refresh", {"Farmer": 403, "Buyer": 403, "Officer": 403, "Administrator": 200}),
    ("GET reports", "GET", "/reports", {"Farmer": 403, "Buyer": 403, "Officer": 403, "Administrator": 200}),
]
for name, method, path, expected in matrix:
    st, _ = call(method, path, None)
    check(f"{name} with no token", st == 401, f"expected 401, got {st}")
    for role, want in expected.items():
        st, _ = call(method, path, tokens[role])
        check(f"{name} as {role}", st == want, f"expected {want}, got {st}")

print("\n== 3. Component D data content ==")
st, body = call("GET", trend, tokens["Officer"])
pts = body["points"]
check("price-trends returns weekly points with real prices", st == 200 and len(pts) > 0 and all(p["avgPrice"] > 0 for p in pts), f"{len(pts)} points for {crop['name']}")
check("crops without prices are flagged", any(not c["hasPriceHistory"] for c in filters["crops"]), f"{sum(1 for c in filters['crops'] if c['hasPriceHistory'])} of {len(filters['crops'])} crops have data")
st, anomalies0 = call("GET", "/analytics/anomalies?size=100", tokens["Officer"])
st, shortages = call("GET", "/analytics/shortages", tokens["Officer"])
check("anomaly queue and shortages return seeded data", anomalies0["total"] > 0 and len(shortages["items"]) > 0, f"{anomalies0['total']} anomaly flags, {len(shortages['items'])} shortage events")

print("\n== 4. Business workflow: Farmer -> Officer -> Buyer ==")
_, crops = call("GET", "/crops", tokens["Farmer"])
_, regions = call("GET", "/regions", tokens["Farmer"])
carrot = next(c for c in crops if c["name"] == "Carrots")
now = dt.datetime.now(dt.timezone.utc).replace(tzinfo=None)
listing_req = {
    "cropId": carrot["id"], "regionId": regions[0]["id"], "quantity": 100, "unit": "kg", "claimedGrade": "Grade A",
    "pickupWindowStart": (now + dt.timedelta(days=3)).isoformat() + "Z", "pickupWindowEnd": (now + dt.timedelta(days=6)).isoformat() + "Z",
    "minPrice": 5000, "description": "Flow check listing (deliberately priced far above market)", "photoUrls": ["https://example.com/p.jpg"],
}
st, listing = call("POST", "/listings", tokens["Farmer"], listing_req)
check("Farmer creates a listing", st == 201, f"status {st}, listing status '{listing.get('status') if isinstance(listing, dict) else listing}'")
lid = listing["id"] if st == 201 else None
if lid:
    st, mine = call("GET", "/listings/my", tokens["Farmer"])
    check("Farmer sees it in My Listings", st == 200 and any(l["id"] == lid for l in mine.get("items", mine) if isinstance(l, dict)), f"status {st}")
    st, _ = call("POST", "/listings", tokens["Buyer"], listing_req)
    check("Buyer cannot create a listing", st == 403, f"status {st}")
    st, pub = call("GET", "/listings?status=Published&pageSize=100", tokens["Buyer"])
    visible = any(l["id"] == lid for l in pub.get("items", []))
    check("Buyer cannot see the unpublished listing", st == 200 and not visible, f"status {st}")
    st, pend = call("GET", "/listings/pending-inspection", tokens["Officer"])
    check("Officer sees it waiting for inspection", st == 200 and any(l.get("id") == lid for l in (pend if isinstance(pend, list) else pend.get("items", []))), f"status {st}")
    st, _ = call("GET", "/listings/pending-inspection", tokens["Farmer"])
    check("Farmer cannot use the officer inspection queue", st == 403, f"status {st}")
    st, body = call("POST", f"/listings/{lid}/publish", tokens["Officer"])
    check("Officer publish is gated until inspected (expect refusal, not 5xx)", st in (400, 409, 422), f"status {st}: {str(body)[:150]}")
    st, order = call("POST", "/orders", tokens["Buyer"], {"listingId": lid, "quantity": 5, "deliveryPreference": "Pickup"})
    check("Buyer cannot order an unpublished listing", st in (400, 404, 409), f"status {st}: {str(order)[:120]}")

st, anomalies1 = call("GET", "/analytics/anomalies?size=100", tokens["Officer"])
check("D integration: a new overpriced listing creates an anomaly flag", anomalies1["total"] > anomalies0["total"],
      f"flags before {anomalies0['total']}, after {anomalies1['total']}")

print("\n== 5. Orders by role ==")
st, _ = call("GET", "/orders", tokens["Buyer"]);  check("Buyer lists own orders", st == 200, f"status {st}")
st, _ = call("GET", "/orders", tokens["Officer"]); check("Officer lists orders", st == 200, f"status {st}")
st, _ = call("GET", "/orders", tokens["Administrator"]); check("Administrator lists orders", st == 200, f"status {st}")

passed = sum(1 for r in results if r[1])
print(f"\nSUMMARY: {passed} passed, {len(results) - passed} failed, {len(results)} total")
json.dump([{"case": c, "pass": ok, "detail": d} for c, ok, d in results], open(sys.argv[1] if len(sys.argv) > 1 else "flow_results.json", "w"), indent=2)
