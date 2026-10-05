"""Order -> approval -> pickup schedule -> completion, as Buyer / Officer / Farmer, against the live API.
Passwords are read from the seed comment at run time and never printed."""
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
            return e.code, raw[:200]


results = []


def step(name, ok, detail=""):
    results.append((name, ok, detail))
    print(f"{'PASS' if ok else 'FAIL'}  {name}  {detail}")


def login(email):
    st, b = call("POST", "/auth/login", body={"email": email, "password": PW})
    return b["token"], b["id"]


buyer, buyer_id = login("buyer@agriconnect.lk")
officer, _ = login("officer@agriconnect.lk")
farmer, _ = login("farmer@agriconnect.lk")
farmer2, _ = login("farmer2@agriconnect.lk")

st, pub = call("GET", "/listings?status=Published&pageSize=50", buyer)
listing = next(l for l in pub["items"] if l["cropName"] == "Carrots")
qty_before = listing["quantity"]
step("Buyer sees a published listing", st == 200, f"{listing['cropName']} · {listing['regionName']} · {qty_before} kg")

# --- Buyer places the order -----------------------------------------------------------
st, order = call("POST", "/orders", buyer, {"listingId": listing["id"], "quantity": 50, "deliveryPreference": "Pickup"})
step("Buyer places an order for 50 kg", st == 201 and order["status"] == "Pending", f"status {st}, order status '{order.get('status') if isinstance(order, dict) else order}'")
oid = order["id"]

st, mine = call("GET", "/orders", buyer)
items = mine["items"] if isinstance(mine, dict) else mine
step("Order appears in the buyer's My Orders", any(o["id"] == oid for o in items), f"{len(items)} order(s)")

st, _ = call("GET", f"/orders/{oid}", farmer2)
step("Another farmer cannot open the order (ownership)", st in (403, 404), f"status {st}")

st, bad = call("POST", "/orders", buyer, {"listingId": listing["id"], "quantity": 999999, "deliveryPreference": "Pickup"})
step("Ordering more than the stock is refused", st in (400, 409), f"status {st}")

st, bad = call("POST", "/orders", buyer, {"listingId": listing["id"], "quantity": 0, "deliveryPreference": "Pickup"})
step("Ordering 0 kg is refused", st == 400, f"status {st}")

st, _ = call("PUT", f"/orders/{oid}/status", buyer, {"status": "Approved"})
step("Buyer cannot approve their own order", st == 403, f"status {st}")

# --- Officer reviews ------------------------------------------------------------------
st, queue = call("GET", "/orders?status=Pending&pageSize=50", officer)
q = queue["items"] if isinstance(queue, dict) else queue
step("Officer sees it in the Orders queue", st == 200 and any(o["id"] == oid for o in q), f"{len(q)} pending")

st, ap = call("PUT", f"/orders/{oid}/status", officer, {"status": "Approved"})
step("Officer approves the order", st == 200 and ap["status"] == "Approved", f"status {st}, now '{ap.get('status') if isinstance(ap, dict) else ap}'")

# --- Pickup schedule (the AI agent proposes it automatically) --------------------------
st, sch = call("GET", f"/orders/{oid}/schedule", officer)
step("A pickup slot was proposed automatically", st == 200 and sch["status"] == "Proposed", f"status {st}, slot status '{sch.get('status') if isinstance(sch, dict) else sch}'")
if st == 200:
    print("      proposed:", sch.get("slotStart"), "->", sch.get("slotEnd"), "| conflictChecked:", sch.get("conflictChecked"))

st, _ = call("PUT", f"/orders/{oid}/schedule/decision", buyer, {"decision": "Approve"})
step("Buyer cannot approve the pickup slot", st == 403, f"status {st}")

st, dec = call("PUT", f"/orders/{oid}/schedule/decision", officer, {"decision": "Approve"})
step("Officer approves the pickup slot", st == 200 and dec["status"] in ("Confirmed", "Approved"), f"status {st}, slot now '{dec.get('status') if isinstance(dec, dict) else dec}'")

st, o2 = call("GET", f"/orders/{oid}", buyer)
step("Order becomes Scheduled", st == 200 and o2["status"] == "Scheduled", f"order status '{o2.get('status') if isinstance(o2, dict) else o2}'")

st, sc_b = call("GET", f"/orders/{oid}/schedule", buyer)
step("Buyer can see the confirmed pickup slot", st == 200, f"status {st}")

# --- Complete ---------------------------------------------------------------------------
st, done = call("PUT", f"/orders/{oid}/status", officer, {"status": "Completed"})
step("Officer marks the order Completed", st == 200 and done["status"] == "Completed", f"status {st}")

st, bad = call("POST", f"/orders/{oid}/cancel", buyer, {"reason": "test"})
step("A completed order cannot be cancelled", st in (400, 409), f"status {st}")

# --- Cancel path with stock release ----------------------------------------------------
st, pub2 = call("GET", "/listings?status=Published&pageSize=50", buyer)
stock_mid = next(l for l in pub2["items"] if l["id"] == listing["id"])["quantity"]
st, o3 = call("POST", "/orders", buyer, {"listingId": listing["id"], "quantity": 20, "deliveryPreference": "Pickup"})
st1, pub3 = call("GET", "/listings?status=Published&pageSize=50", buyer)
stock_reserved = next(l for l in pub3["items"] if l["id"] == listing["id"])["quantity"]
st, c = call("POST", f"/orders/{o3['id']}/cancel", buyer, {"reason": "Changed my mind"})
step("Buyer cancels a pending order", st == 200 and c["status"] == "Cancelled", f"status {st}")
st1, pub4 = call("GET", "/listings?status=Published&pageSize=50", buyer)
stock_after = next(l for l in pub4["items"] if l["id"] == listing["id"])["quantity"]
print(f"      stock for the listing: before orders {qty_before} kg, after the first order {stock_mid} kg, with 20 kg reserved {stock_reserved} kg, after cancelling {stock_after} kg")
step("Cancelling released the reserved stock", stock_after == stock_mid, f"{stock_reserved} -> {stock_after} kg")

passed = sum(1 for _, ok, _ in results if ok)
print(f"\nSUMMARY: {passed} passed, {len(results) - passed} failed, {len(results)} total")
json.dump([{"step": n, "pass": ok, "detail": d} for n, ok, d in results], open(sys.argv[1] if len(sys.argv) > 1 else "order_flow_results.json", "w"), indent=2)
