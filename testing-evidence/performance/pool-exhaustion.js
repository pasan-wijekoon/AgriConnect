// DEF-P-01 retest: what does the API answer when its database cannot serve requests?
// (In run A the Supabase pooler refused clients beyond 15 and the API answered 500; this script makes the
// same class of failure deterministic by pointing the API at a database port nothing listens on.)
// Counts responses by status: before the fix every request failed with 500, after the fix with 503 + Retry-After.
//
// The token is obtained beforehand (login needs the database), e.g.
//   TOKEN=$(curl -s -X POST localhost:5000/api/auth/login -H 'Content-Type: application/json' //     -d '{"email":"officer@agriconnect.lk","password":"password"}' | python -c "import sys,json;print(json.load(sys.stdin)['token'])")
//   docker run --rm -i --add-host=host.docker.internal:host-gateway -e BASE_URL=http://host.docker.internal:5000 -e TOKEN=$TOKEN //     -v "$PWD/testing-evidence/run-2026-10-07:/out" grafana/k6 run --summary-export=/out/<name>.json - < testing-evidence/performance/pool-exhaustion.js
import http from 'k6/http';
import { Counter } from 'k6/metrics';

const BASE = __ENV.BASE_URL || 'http://localhost:5000';
const CROP_ID = 'a1000000-0000-0000-0000-000000000005'; // Carrots, has price history in the seed
const ok = new Counter('status_2xx');
const unavailable = new Counter('status_503');
const serverError = new Counter('status_500');
const other = new Counter('status_other');
const retryAfter = new Counter('responses_503_with_retry_after');

export const options = { vus: 50, duration: '15s' };

export default function () {
  const res = http.get(`${BASE}/api/analytics/price-trends?cropId=${CROP_ID}&from=2026-06-01&to=2026-12-31&bucket=week`,
    { headers: { Authorization: `Bearer ${__ENV.TOKEN}` } });
  if (res.status === 200) ok.add(1);
  else if (res.status === 503) { unavailable.add(1); if (res.headers['Retry-After']) retryAfter.add(1); }
  else if (res.status === 500) serverError.add(1);
  else other.add(1);
}
