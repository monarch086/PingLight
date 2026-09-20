import 'zone.js/node';

import { AngularNodeAppEngine, createNodeRequestHandler, isMainModule, writeResponseToNodeResponse } from '@angular/ssr/node';
import express from 'express';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

export function app(): express.Express {
  const server = express();
  const serverFolder = dirname(fileURLToPath(import.meta.url));
  const browserFolder = resolve(serverFolder, '../browser');
  const allowedHosts = (process.env['SSR_ALLOWED_HOSTS'] ?? 'localhost,127.0.0.1,dev.pinglight.xyz')
    .split(',').map(host => host.trim()).filter(Boolean);
  const engine = new AngularNodeAppEngine({ allowedHosts });

  server.get('*.*', express.static(browserFolder, { maxAge: '1y' }));
  server.use((req, res, next) => {
    engine.handle(req).then(response => {
      if (response) writeResponseToNodeResponse(response, res);
      else next();
    }).catch(next);
  });
  return server;
}

const server = app();
export const reqHandler = createNodeRequestHandler(server);

if (isMainModule(import.meta.url)) {
  const port = process.env['PORT'] || 4000;
  server.listen(port, () => console.log('PingLight SSR listening on http://localhost:' + port));
}
