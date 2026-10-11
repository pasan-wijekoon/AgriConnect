"""Scripted security checks against a running AgriConnect API (complements the OWASP ZAP scan).

    uv run --project agentic-ai python testing-evidence/security/security_checks.py [BASE_URL]

Run it against a THROWAWAY database only: it fires failed logins and tampered requests.
Exit code is the number of failed checks. Results are also written to security-checks-results.json.
"""
import json
import re
import sys
from pathlib import Path

import httpx

BASE = sys.argv[1] if len(sys.argv) > 1 else "http://localhost:5000"
results = []
STACK = re.compile(r"(\bat [A-Za-z_.]+\(|Npgsql|Microsoft\.|System\.|StackTrace|--->)")


def record(cid, title, expected, actual, ok):
    results.append({"id": cid, "title": title, "expected": expected, "actual": actual, "status": "Passed" if ok else "Failed"})
    print(f"{'PASS' if ok else 'FAIL'}  {cid}  {title}\n      expected: {expected}\n      actual:   {actual}")


def login(client, email, password="password"):
    r = client.post("/api/auth/login", json={"email": email, "password": password})
    return r.json()["token"] if r.status_code == 200 else None


def auth(token):
    return {"Authorization": f"Bearer {token}"}


with httpx.Client(base_url=BASE, timeout=30) as c:
    buyer, farmer, farmer2, officer = (login(c, e) for e in (
        "buyer@agriconnect.lk", "farmer@agriconnect.lk", "farmer2@agriconnect.lk", "officer@agriconnect.lk"))

    # --- Authentication / authorization (BE-A-05..08, BE-A-13, XP-W-05) -------------------------
    r = c.get("/api/listings")
    record("SEC-A1", "No token on a protected endpoint", "401", r.status_code, r.status_code == 401)
    r = c.get("/api/listings", headers=auth(buyer[:-5] + "XXXXX"))
    record("SEC-A2", "Tampered JWT signature", "401", r.status_code, r.status_code == 401)
    r = c.post("/api/orders", headers=auth(farmer), json={"listingId": "4f2b1001-0000-0000-0000-000000000001", "quantity": 1, "deliveryPreference": "Pickup"})
    record("SEC-A3", "Farmer places an order (Buyer-only action)", "403", r.status_code, r.status_code == 403)
    r = c.get("/api/analytics/anomalies", headers=auth(buyer))
    record("SEC-A4", "Buyer reads officer analytics queue", "403", r.status_code, r.status_code == 403)

    # IDOR: another user's order by id
    orders = c.get("/api/orders?size=1", headers=auth(buyer)).json()["items"]
    if orders:
        oid = orders[0]["id"]
        r = c.get(f"/api/orders/{oid}", headers=auth(farmer2))
        record("SEC-A5", "IDOR: farmer2 reads another farmer's buyer order by id", "403 or 404, no order data", r.status_code,
               r.status_code in (403, 404) and "quantity" not in r.text)
        r = c.post(f"/api/orders/{oid}/cancel", headers=auth(farmer2), json={"reason": "idor"})
        record("SEC-A6", "IDOR: unrelated farmer cancels the buyer's order", "403 or 404", r.status_code, r.status_code in (403, 404))

    # --- SEC-03 injection ----------------------------------------------------------------------
    baseline = c.get("/api/listings?search=Carrot", headers=auth(buyer)).json().get("totalCount")
    leaks = []
    for payload in ["' OR '1'='1", "'; DROP TABLE \"Order\";--", "Carrot' UNION SELECT NULL--", "%27%20OR%201=1--"]:
        r = c.get("/api/listings", params={"search": payload}, headers=auth(buyer))
        body_has_sql = bool(STACK.search(r.text)) or "syntax error" in r.text.lower()
        every = r.status_code == 200 and r.json().get("totalCount", 0) > (baseline or 0)
        if r.status_code >= 500 or body_has_sql or every:
            leaks.append((payload, r.status_code))
    record("SEC-03", "SQL injection payloads in the search parameter", "no 5xx, no SQL text, no extra rows", "clean" if not leaks else leaks, not leaks)
    r = c.get("/api/listings", params={"sortBy": "price; DROP TABLE x"}, headers=auth(buyer))
    record("SEC-03b", "SQL injection in sortBy", "no 5xx / no SQL text", r.status_code, r.status_code < 500 and not STACK.search(r.text))

    # --- SEC-05 error responses leak nothing ---------------------------------------------------
    r = c.post("/api/auth/login", content="{bad json", headers={"Content-Type": "application/json"})
    record("SEC-05a", "Malformed JSON body", "400 and no stack trace", r.status_code, r.status_code == 400 and not STACK.search(r.text))
    r = c.get("/api/orders/not-a-guid", headers=auth(buyer))
    record("SEC-05b", "Malformed id in route", "4xx and no stack trace", r.status_code, 400 <= r.status_code < 500 and not STACK.search(r.text))
    r = c.options("/api/listings", headers={"Origin": "https://evil.example", "Access-Control-Request-Method": "GET"})
    allow = r.headers.get("access-control-allow-origin")
    record("SEC-05c", "CORS does not allow an arbitrary origin", "no Access-Control-Allow-Origin for evil.example", allow, allow not in ("*", "https://evil.example"))

    # --- SEC-07 price tampering ----------------------------------------------------------------
    listing = c.get("/api/listings?pageSize=1", headers=auth(buyer)).json()["items"][0]
    r = c.post("/api/orders", headers=auth(buyer), json={"listingId": listing["id"], "quantity": 1, "deliveryPreference": "Pickup",
                                                           "price": 0.01, "unitPrice": 0.01, "totalPrice": 0.01, "status": "Completed"})
    ok = r.status_code == 201 and r.json().get("status") == "Pending" and not any(k in r.json() for k in ("price", "unitPrice", "totalPrice"))
    record("SEC-07", "Client-supplied price/status fields on order creation", "201, status Pending, client price ignored", f"{r.status_code} {r.json().get('status') if r.status_code == 201 else ''}", ok)
    if r.status_code == 201:
        c.post(f"/api/orders/{r.json()['id']}/cancel", headers=auth(buyer), json={"reason": "security test cleanup"})

    # --- SEC-04 stored XSS: the API stores text verbatim, so safety depends on output encoding --
    # (checked statically: the web app never uses dangerouslySetInnerHTML; see the report)

    # --- SEC-06 brute force --------------------------------------------------------------------
    codes = [c.post("/api/auth/login", json={"email": "buyer@agriconnect.lk", "password": f"wrong-{i}"}).status_code for i in range(40)]
    throttled = any(s in (423, 429) for s in codes)
    record("SEC-06", "40 failed logins in a row for one account", "throttled/locked (429/423) at some point", f"{sorted(set(codes))} (throttled={throttled})", throttled)

Path(__file__).with_name("security-checks-results.json").write_text(json.dumps(results, indent=2), encoding="utf8")
failed = [r for r in results if r["status"] == "Failed"]
print(f"\n{len(results) - len(failed)} passed, {len(failed)} failed")
sys.exit(len(failed))
