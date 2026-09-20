# PingLight frontend

Angular 21.2 LTS with the Angular application builder and @angular/ssr. The app
continues to run through Express, Lambda and API Gateway at its existing domain.
The Lambda runtime is Node.js 24; local development requires Node 24.15 or newer
within the Node 24 LTS line (.nvmrc selects 24).

The dashboard supports Cognito sign-in and editing assigned device descriptions,
notification delays and report preferences. System administrators see all devices
and users and can grant or revoke device access. General users see only granted
devices. Device registration and Telegram destination changes are outside this version.

The device list is at `/devices` (All devices for administrators, My devices for
general users). Administrators manage user device access at `/users`. Both URLs
support direct visits, refresh, and browser back/forward navigation. Sign-in returns
to the selected page; refreshing may require signing in again because tokens are
kept in memory. General users visiting `/users` return to `/devices`.

`AppComponent` owns the shared layout and account initialization. Its router outlet
renders `DevicesPageComponent`, `UsersPageComponent`, or `AuthCallbackComponent`.
Each workspace page owns its requests, messages, and editing state; the shell and
pages share only the current account and pending-write count through
`WorkspaceSession`. The signed-out screen lives in `WelcomeComponent`.

## Setup and development

~~~powershell
npm ci
./configure-api.ps1 -Stage dev
npm start
~~~

The development server is at http://localhost:4201. See the
[management API setup](../PingLight.WebApi/README.md) for backend configuration.
The checked-in src/assets/app-config.json is empty; configure it before using
account access. For a local API, set its apiUrl to http://localhost:5063.
For dev, the configuration script writes `https://dev.api.pinglight.xyz`, shared
with the existing `/pings`, `/changes`, and `/test` endpoints.

## Build and verify

~~~powershell
npm run build:ssr
npm run lint
npm test -- --watch=false --browsers=ChromeHeadless
npm run test:ssr
npm run serve:ssr
~~~

`npm run lint` checks TypeScript and Angular templates with the recommended
Angular ESLint rules, including template accessibility, and checks SCSS with the
standard SCSS Stylelint config. Run `npm run lint:fix` to apply safe automatic
fixes before resolving any remaining findings manually.

One build emits browser files into dist/pinglight/browser and ESM server bundles
into dist/pinglight/server. The Lambda adapter dynamically imports server.mjs and
reuses the request handler on warm invocations. The smoke test calls this adapter
with API Gateway events for the home page, callback, config and a binary image.
The standalone production preview listens at http://localhost:4000.

## Deploy

~~~powershell
npx serverless package --stage dev
npx serverless deploy --stage dev
~~~

Generate app-config.json for the target stage before building. Angular's SSR
hostname allowlist is populated with dev.pinglight.xyz for dev and the stack's
API Gateway hostname. For another stage/custom domain, pass
--param="frontendHost=your-domain.example" (hostname only). For local testing with
a different hostname, set SSR_ALLOWED_HOSTS to a comma-separated list.

The new builder replaces the retired @nguniversal packages; hosting, service
name, API routes and domain mappings remain the same. Node 24 may trigger a
runtime-enum warning in Serverless v3; AWS supports nodejs24.x. The remaining npm
audit findings are in Serverless development/deployment tooling. Upgrading that
tooling to v4 requires a separate compatibility/login review. The stylesheet
exceeds the existing 2 KB warning budget but is below its 4 KB failure budget.

Angular LTS policy: https://angular.dev/reference/releases
