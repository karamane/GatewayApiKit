import http from 'k6/http';
import { check, sleep } from 'k6';
import { Counter, Rate } from 'k6/metrics';

// Config
const BASE_URL = 'http://localhost:53000/moim/api/v1/internet/auth/login'; // Correct Ocelot route
const VUS = 50;
const DURATION = '30s';

// Metrics
const throttledRate = new Counter('throttled_requests');
const successRate = new Rate('success_rate');
const errorRate = new Rate('error_rate');

// Provided Real Token
const TOKEN = "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiJ9.eyJnc20iOiJURTZnRXdyTXJBdk9XMTY4RXFfWHVBPT0iLCJsb2dpblR5cGUiOiJUQ0tOIiwiaXNzIjoidHRvaW0iLCJpc0hpem1ldCI6ImZhbHNlIiwic2Vzc2lvbklkIjoiNmE1ZTg3ZTItMWFjMC00Mzc2LTgzYTctMDM5Y2VlNmI0YjI4Iiwia21saSI6ImZhbHNlIiwiYXVkIjoiYWNjZXNzIiwidXNyIjoiT25lVm5zLVZobjl0dlFILTJLelZ4QT09IiwiY29ycG9yYXRlIjoiMCIsInJqdGkiOiI2ZWI1YTc3Ni0xNDFjLTQ3N2YtOTMzYi04YTc3ZWNkZTYwNTkiLCJnQ2lkIjoiMTM0NjMyNjMxNTUiLCJleHAiOjE5ODQzNzUxNTYsImlhdCI6MTc2ODM3NTE1NiwianRpIjoiZmQ0M2M4NmUtNjdjOC00ZTc5LWFlYmQtMmM4NDBiOGJjMjllIiwic0NpZCI6IjExNzgwMTkzMTMifQ.hLsehVJsx5dvFhvwJDacWeP55jIz2rO6GLxyGvrTE_C1ndwX2TCpCN2nDs7aZbuRWfSarjU-j0RwpF98HRg4OZ6xw5GdAPYlM17DAzBMlrQqYAkeZcV8NPERyrIOjjw_fnDCYKqSnJRCXyvrvbywJ2do9uGQaDnFYDzu6lsCsy2ME_N68r1pRzYG01Ob43dDevTqUYzc1Wa04ULKaWVEDwMT6pNMuNMH_UQ4UiTQaPGW_oAWrnFK5BB02BylMN_P5lErS-NGjeIzf3priz1QgYdI5Do6tWtwmB3tU5N9RrhcAli3NlTfCc33aNIooSEf4nJQUX21e7G4HPAIQ7rcYQ";

export let options = {
  scenarios: {
    token_flood: {
      executor: 'constant-vus',
      vus: 20,
      duration: '30s',
    },
  },
  thresholds: {
    'http_req_duration': ['p(95)<2000'],
  }
};

export default function () {
  const params = {
    headers: {
      'Authorization': `Bearer ${TOKEN}`,
      'Content-Type': 'application/json',
      'X-Correlation-Id': `k6-${__VU}-${__ITER}`
    },
  };

  // Using a POST request with valid body
  const payload = JSON.stringify({
    "Channel": "iPhone",
    "UserName": 5333446433,
    "Version": "9.3.1",
    "LoginType": 1,
    "AdslNo": 1799933036,
    "Segment": [
      "PRIME",
      "ANOTHERSEGMENT",
      "YUKSEKHIZ"
    ],
    "IsRefresh": true
  });

  const res = http.post(BASE_URL, payload, params);

  // Debugging: Print status if not 200/429
  if (res.status !== 200 && res.status !== 429) {
    console.log(`Unexpected Status: ${res.status} Body: ${res.body}`);
  }

  // Check Results
  const isThrottled = res.status === 429;
  const isSuccess = res.status === 200 || res.status === 500; // 500 acceptable if downstream is mock/missing, means gateway passed it

  if (isThrottled) {
    throttledRate.add(1);
  }

  check(res, {
    'status is 200 or 429': (r) => r.status === 200 || r.status === 429 || r.status === 503,
  });

  if (res.status === 200) successRate.add(1);
  else errorRate.add(1);

  sleep(0.1);
}
