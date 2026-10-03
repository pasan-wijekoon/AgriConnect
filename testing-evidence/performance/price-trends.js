// k6 performance test for Component D: GET /api/analytics/price-trends (FR15).
// Run:  docker run --rm -i --add-host=host.docker.internal:host-gateway -e BASE_URL=http://host.docker.internal:5000 \
//         -v "$PWD/testing-evidence/performance:/out" grafana/k6 run --summary-export=/out/k6-summary.json - < testing-evidence/performance/price-trends.js
import http from 'k6/http';
import { check, sleep } from 'k6';

const BASE = __ENV.BASE_URL || 'http://localhost:5000';
const HEADERS = { headers: { 'X-Dev-Role': 'Farmer' } };

export const options = {
  scenarios: {
    smoke: { executor: 'constant-vus', vus: 1, duration: '10s', exec: 'trends', startTime: '0s' },
    load: { executor: 'constant-vus', vus: 20, duration: '30s', exec: 'trends', startTime: '12s' },
    stress: {
      executor: 'ramping-vus', exec: 'trends', startTime: '45s',
      stages: [{ duration: '15s', target: 100 }, { duration: '15s', target: 200 }, { duration: '10s', target: 0 }],
    },
  },
  thresholds: {
    'http_req_duration{scenario:load}': ['p(95)<500'], // SRS target under normal load
    'http_req_failed{scenario:load}': ['rate<0.01'],
    checks: ['rate>0.99'],
  },
};

export function setup() {
  const filters = http.get(`${BASE}/api/analytics/filters`, HEADERS).json();
  const crop = filters.crops.find((c) => c.hasPriceHistory);
  return { cropId: crop.id };
}

export function trends(data) {
  const res = http.get(
    `${BASE}/api/analytics/price-trends?cropId=${data.cropId}&from=2026-06-01&to=2026-12-31&bucket=week`,
    HEADERS,
  );
  check(res, {
    'status is 200': (r) => r.status === 200,
    'has price points': (r) => r.json('points').length > 0,
  });
  sleep(0.2);
}
