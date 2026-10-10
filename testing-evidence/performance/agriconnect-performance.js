// SE3110 performance test for the AgriConnect API (real JWT auth, real PostgreSQL).
//
// Scenarios
//   browse_load    20 VUs, 30s   read mix: GET /api/listings, price-trends, collection-centres  (PERF load)
//   browse_stress  ramp to 200   same mix, to find where the API starts to degrade               (PERF stress)
//   login_load     10 VUs, 20s   POST /api/auth/login (password hashing is the expensive part)
//   order_race     60 orders from 30 VUs against ONE listing whose stock is smaller than the
//                  total demand. FR9 says reserved quantity must never exceed available stock, so
//                  teardown re-reads the listing and checks the arithmetic; it then cancels every
//                  order it created so the dev database is left as it was found.
//
// Run (API on :5000, seeded dev database):
//   docker run --rm -i --add-host=host.docker.internal:host-gateway -e BASE_URL=http://host.docker.internal:5000 \
//     -v "$PWD/testing-evidence/performance:/out" grafana/k6 run \
//     --summary-export=/out/k6-agriconnect-summary.json - < testing-evidence/performance/agriconnect-performance.js
import http from 'k6/http';
import { check, sleep } from 'k6';
import { Counter } from 'k6/metrics';

const BASE = __ENV.BASE_URL || 'http://localhost:5000';
const JSON_HEADERS = { 'Content-Type': 'application/json' };
const ORDER_QTY = 10;

const ordersCreated = new Counter('orders_created');
const ordersRejected = new Counter('orders_rejected_insufficient_stock');
const ordersUnexpected = new Counter('orders_unexpected_status');

export const options = {
  scenarios: {
    browse_load: { executor: 'constant-vus', vus: 20, duration: '30s', exec: 'browse', startTime: '0s' },
    browse_stress: {
      executor: 'ramping-vus', exec: 'browse', startTime: '35s',
      stages: [{ duration: '15s', target: 100 }, { duration: '15s', target: 200 }, { duration: '10s', target: 0 }],
    },
    login_load: { executor: 'constant-vus', vus: 10, duration: '20s', exec: 'login', startTime: '80s' },
    order_race: { executor: 'shared-iterations', vus: 30, iterations: 60, maxDuration: '60s', exec: 'orderRace', startTime: '105s' },
  },
  thresholds: {
    'http_req_duration{scenario:browse_load}': ['p(95)<500'], // SRS response-time target under normal load
    'http_req_failed{scenario:browse_load}': ['rate<0.01'],
    'http_req_duration{scenario:login_load}': ['p(95)<1000'],
    'checks{scenario:browse_load}': ['rate>0.99'],
    'checks{scenario:order_race}': ['rate==1'], // no 5xx
    'checks{phase:fr9}': ['rate==1'], // stock arithmetic verified in teardown
    orders_unexpected_status: ['count==0'],
  },
};

// GET /api/orders is paged (page, size<=100 by default 20); walk every page.
function allOrders(token) {
  const out = [];
  for (let page = 1; page < 50; page++) {
    const items = http.get(`${BASE}/api/orders?page=${page}&size=100`, auth(token)).json('items');
    out.push(...items);
    if (items.length < 100) break;
  }
  return out;
}

function getToken(email) {
  const res = http.post(`${BASE}/api/auth/login`, JSON.stringify({ email, password: 'password' }), { headers: JSON_HEADERS });
  if (res.status !== 200) throw new Error(`login ${email} failed: ${res.status}`);
  return res.json('token');
}

const auth = (token) => ({ headers: { ...JSON_HEADERS, Authorization: `Bearer ${token}` } });

export function setup() {
  const buyer = getToken('buyer@agriconnect.lk');
  const officer = getToken('officer@agriconnect.lk');

  const filters = http.get(`${BASE}/api/analytics/filters`, auth(officer)).json();
  const cropId = filters.crops.find((c) => c.hasPriceHistory).id;

  // The race target must be a published listing with enough stock for several orders but
  // not for all 60 of them.
  const listings = http.get(`${BASE}/api/listings?pageSize=50`, auth(buyer)).json('items');
  const target = listings.find((l) => l.availableQuantity >= ORDER_QTY * 5 && l.availableQuantity < ORDER_QTY * 40);
  if (!target) throw new Error('no suitable listing for the order race; reseed the dev database');

  // Orders that already exist for this buyer must not be counted (or cancelled) by this run.
  const existing = allOrders(buyer).map((o) => o.id);

  return { buyer, officer, cropId, listingId: target.id, availableBefore: target.availableQuantity, existing };
}

export function browse(data) {
  const l = http.get(`${BASE}/api/listings?pageSize=20`, auth(data.buyer));
  check(l, { 'listings 200': (r) => r.status === 200, 'listings has items': (r) => r.json('items').length > 0 });

  const t = http.get(
    `${BASE}/api/analytics/price-trends?cropId=${data.cropId}&from=2026-06-01&to=2026-12-31&bucket=week`,
    auth(data.officer),
  );
  check(t, { 'trends 200': (r) => r.status === 200, 'trends has points': (r) => r.json('points').length > 0 });

  const c = http.get(`${BASE}/api/collection-centres`, auth(data.officer));
  check(c, { 'centres 200': (r) => r.status === 200 });
  sleep(0.2);
}

export function login() {
  const res = http.post(`${BASE}/api/auth/login`, JSON.stringify({ email: 'buyer@agriconnect.lk', password: 'password' }), { headers: JSON_HEADERS });
  check(res, { 'login 200': (r) => r.status === 200, 'login returns token': (r) => !!r.json('token') });
  sleep(0.2);
}

export function orderRace(data) {
  const res = http.post(
    `${BASE}/api/orders`,
    JSON.stringify({ listingId: data.listingId, quantity: ORDER_QTY, deliveryPreference: 'Pickup' }),
    auth(data.buyer),
  );
  if (res.status === 201) {
    ordersCreated.add(1);
  } else if (res.status === 409 || res.status === 400) {
    ordersRejected.add(1); // sold out / insufficient stock is the correct answer once stock runs out
  } else {
    ordersUnexpected.add(1);
  }
  check(res, { 'order answered 201/409/400, never 5xx': (r) => [201, 400, 409].includes(r.status) });
}

export function teardown(data) {
  // Orders created by this run = the buyer's orders that did not exist in setup(); check stock arithmetic, then cancel.
  const mine = allOrders(data.buyer)
    .filter((o) => !data.existing.includes(o.id) && o.listingId === data.listingId && o.status === 'Pending');

  const listing = http.get(`${BASE}/api/listings/${data.listingId}`, auth(data.buyer)).json();
  const reserved = mine.length * ORDER_QTY;
  const maxPossible = Math.floor(data.availableBefore / ORDER_QTY) * ORDER_QTY;

  console.log(`FR9 check: available before=${data.availableBefore}, reserved by this run=${reserved}, available after=${listing.availableQuantity}`);
  check(null, {
    'FR9 reserved quantity never exceeds available stock': () => reserved <= data.availableBefore,
    'FR9 exactly the orders that fit were accepted': () => reserved === maxPossible,
    'FR9 available stock never negative': () => listing.availableQuantity >= 0,
    'FR9 available stock reduced by exactly the reserved amount': () => listing.availableQuantity === data.availableBefore - reserved,
  }, { phase: 'fr9' });

  for (const o of mine) {
    http.post(`${BASE}/api/orders/${o.id}/cancel`, JSON.stringify({ reason: 'k6 performance test cleanup' }), auth(data.buyer));
  }
}
