const apiBaseUrl =
  process.env.E2E_API_BASE_URL || 'http://localhost:5000';

async function request(path, { token, method = 'GET', body } = {}) {
  const response = await fetch(`${apiBaseUrl}${path}`, {
    method,
    headers: {
      Accept: 'application/json',
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
    ...(body === undefined ? {} : { body: JSON.stringify(body) }),
  });
  const text = await response.text();
  let data;
  try {
    data = text ? JSON.parse(text) : null;
  } catch {
    throw new Error(`API ${method} ${path} returned non-JSON (${response.status}).`);
  }
  if (!response.ok) {
    throw new Error(
      `API ${method} ${path} failed (${response.status}): ${
        data?.detail || data?.error || text
      }`,
    );
  }
  return data;
}

async function login(email, password) {
  if (!email || !password) {
    throw new Error('Set the test-account email and TEST_ACCOUNT_PASSWORD.');
  }
  const user = await request('/api/auth/login', {
    method: 'POST',
    body: { email, password },
  });
  if (!user.token) throw new Error('Login response did not include a JWT.');
  return user;
}

export { apiBaseUrl, login, request };
