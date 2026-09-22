const assert = require('node:assert/strict');
const { universal } = require('../lambda');

function request(path, queryStringParameters = null) {
  return universal(
    {
      resource: '/{proxy+}',
      path,
      httpMethod: 'GET',
      headers: { Host: 'dev.pinglight.xyz', 'X-Forwarded-Proto': 'https' },
      queryStringParameters,
      body: null,
      isBase64Encoded: false,
      requestContext: { stage: 'dev', path: '/dev' + path },
    },
    {
      functionName: 'pinglight-front-smoke',
      getRemainingTimeInMillis: () => 15000,
    },
  );
}
function body(response) {
  return response.isBase64Encoded
    ? Buffer.from(response.body, 'base64').toString('utf8')
    : response.body;
}

setTimeout(() => {
  console.error('Lambda SSR smoke test timed out.');
  process.exit(1);
}, 20000).unref();
(async () => {
  const home = await request('/');
  assert.equal(home.statusCode, 302);
  assert.equal(
    new URL(
      home.headers.location || home.headers.Location,
      'https://dev.pinglight.xyz',
    ).pathname,
    '/devices',
  );
  for (const path of ['/devices', '/users']) {
    const response = await request(path);
    assert.equal(response.statusCode, 200, path + ': ' + body(response));
    assert.match(
      body(response),
      /Відстежуйте наявність світла у Вашому домі\./,
    );
  }
  const config = await request('/assets/app-config.json');
  assert.equal(config.statusCode, 200);
  assert.ok(Object.hasOwn(JSON.parse(body(config)), 'clientId'));
  const logo = await request('/assets/pinglight_logo_clear.png');
  assert.equal(logo.statusCode, 200);
  assert.equal(logo.isBase64Encoded, true);
  assert.equal(
    Buffer.from(logo.body, 'base64').subarray(1, 4).toString(),
    'PNG',
  );
  console.log(
    'Passed: Lambda SSR home, devices, users, configuration and binary asset responses.',
  );
  process.exit(0);
})().catch((error) => {
  console.error(error);
  process.exit(1);
});
