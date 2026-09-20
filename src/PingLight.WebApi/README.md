# PingLight management API

ASP.NET Core runs as one Lambda behind an HTTP API. Cognito handles sign-up,
verified email, login and password recovery. PostgreSQL/ASP.NET Identity are no
longer part of this application's runtime; no existing database is modified.

## First version

- GET /devices returns only devices whose OwnerId matches the verified
  Cognito access token's sub. Only the assigned destination rows are returned.
- PUT /devices/{deviceId}/destinations/{chatId}/settings updates description (up to 200 characters),
  notification delay (0–86400 whole seconds), and daily/weekly/monthly report flags.
- Device ownership is checked atomically when writing. Missing, unassigned and
  other users' devices all return 404 on update.
- ChatId, IsActive, ownership, and provisioning credentials cannot be edited here.
- No public registration/claiming of devices, no telemetry changes, and no
  migration of the existing ingestion or scheduled Lambdas.

The existing table is scanned with an owner filter and internal pages of 100
evaluated records. Pagination keys stay on the server to avoid exposing identifiers
of other users. Add an OwnerId index when the fleet grows. DeviceConfig rows
are updated in place without replacing attributes consumed by the workers.

## Deploy to dev

Use AWS credentials for the account containing PingLight.dev.DeviceConfigs,
.NET 10 SDK, PowerShell and Serverless Framework 3.

From src/PingLight.WebApi:

Restore the existing deployment CLI first with npm ci in src/Frontend.
Serverless v3 emits an outdated runtime-schema warning for dotnet10 (as with the
existing Lambda service). The generated function must still use runtime dotnet10;
do not change it to dotnet6 to silence that warning.

~~~powershell
./build.ps1
../Frontend/node_modules/.bin/serverless.cmd package --stage dev
# Review the generated CloudFormation before deploying.
../Frontend/node_modules/.bin/serverless.cmd deploy --stage dev
~~~

This creates the separate pinglight-management-dev stack: one API Lambda, an
HTTP API with a Cognito JWT authorizer, user pool, public PKCE app client and hosted
login domain. It references the existing DeviceConfigs table; it does not create
or replace it. It requires a string DeviceId partition key and string ChatId sort
key (verified in dev).

Dev login redirects allow https://dev.pinglight.xyz and http://localhost:4201.
For another stage, explicitly pass --param="frontendOrigin=https://your-domain".
The user pool is retained on stack deletion/replacement. Do not remove an active
identity stack as a routine deployment step.

After deployment, from src/Frontend:

~~~powershell
./configure-api.ps1 -Stage dev
npm ci
npm run build:ssr
npx serverless deploy --stage dev
~~~

This preserves the existing Angular SSR / Express / Lambda / API Gateway frontend
hosting and domain. app-config.json contains only public endpoints/client IDs.
The checked-in configuration is deliberately empty; generate it for the target
environment before building. Never deploy a build configured for another stage.

Sign-in uses authorization code + PKCE via oidc-client-ts. Tokens are kept in
memory; the temporary login transaction uses sessionStorage. A full page reload
requires the sign-in redirect again (Cognito may reuse its login session).
Refresh tokens renew the session while the page remains open. Sign-out attempts
refresh-token revocation, clears the local session, and clears Cognito's browser
session. Existing access tokens can remain valid for up to 15 minutes.

## Assign existing devices

After the user signs up and verifies their email, an administrator obtains their
sub from this stack's Cognito user pool and adds a string OwnerId attribute with
that exact value to the appropriate existing DeviceConfigs item, identified by both DeviceId and ChatId. Do this through
a controlled admin workflow, never using an email or owner ID supplied by a
browser. Preserve all existing attributes. No rows are auto-assigned, and the
application has no permission to change ownership through its exposed endpoints.

## Local development

Configure via .NET user secrets or environment variables (do not commit secrets):

~~~powershell
$env:Cognito__Authority = 'https://cognito-idp.eu-central-1.amazonaws.com/USER_POOL_ID'
$env:Cognito__ClientId = 'PUBLIC_APP_CLIENT_ID'
$env:Devices__TableName = 'PingLight.dev.DeviceConfigs'
$env:AllowedOrigins__0 = 'http://localhost:4201'
dotnet run --no-launch-profile --urls http://localhost:5063
~~~

Generate the frontend configuration, then change only apiUrl to
http://localhost:5063 and run npm start. The deployed HTTP API allows only the
deployed frontend origin; local development calls the local API. There is no
authentication bypass. Local API access requires AWS credentials and real
Cognito access tokens.

The legacy Docker build workflow remains in place but is not a Lambda deployment
pipeline. A container requires the same Cognito/table/origin environment settings.
Retire existing database/container resources only after separately verifying their
live usage. The old Identity migrations are available in Git history.

## Verification

The frontend uses Angular 21 LTS and the API targets .NET 10 LTS with Microsoft
framework packages at 10.0.12. Remaining npm audit findings are in Serverless
deployment tooling. The dashboard stylesheet exceeds the existing 2 KB warning
threshold but remains below the 4 KB error threshold. The frontend retains its
hosting architecture with a Node 24 runtime and explicit SSR hostname allowlist.
A real Cognito sign-up/sign-in smoke test still requires deployment.

~~~powershell
dotnet test ../Tests/PingLight.WebApi.Tests/PingLight.WebApi.Tests.csproj
cd ../Frontend
npm run build:ssr
npm run test:ssr
npm test -- --watch=false --browsers=ChromeHeadless
~~~
