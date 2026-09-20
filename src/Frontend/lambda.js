const awsServerlessExpress = require('aws-serverless-express');

let serverPromise;

// Angular's application builder emits ESM. Cache the server across warm invocations.
exports.universal = async (event, context) => {
  serverPromise ??= import('./dist/pinglight/server/server.mjs')
    .then(({ reqHandler }) => awsServerlessExpress.createServer(reqHandler, null, [
      'application/javascript', 'application/json', 'application/octet-stream',
      'text/css', 'text/html', 'image/png', 'image/jpeg', 'image/gif',
      'image/svg+xml', 'image/x-icon', 'font/woff', 'font/woff2'
    ]))
    .catch(error => { serverPromise = undefined; throw error; });
  return awsServerlessExpress.proxy(await serverPromise, event, context, 'PROMISE').promise;
};
