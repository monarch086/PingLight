# PingLight management API

ASP.NET Core runs as one Lambda behind the same API Gateway REST API used by the
ingestion Lambdas. Cognito handles sign-up,
verified email, login, and password recovery. PostgreSQL and ASP.NET Identity are
not part of this application's runtime.

## Access model

- Every authenticated user has a row in `PingLight.<stage>.Users`, keyed by the
  Cognito `sub`. The row is created on the user's first API request.
- `IsSystemAdmin` is a DynamoDB boolean. System administrators see and manage all
  devices and can grant or revoke device access for general users.
- General users see and manage only the composite `DeviceId` + `ChatId` entries
  listed in their `DeviceGrants` string set.
- `GET /users/me` returns the current role. `GET /users` and the grant/revoke
  endpoints are restricted to system administrators.
- `GET /devices` returns all devices for an administrator and only granted devices
  for a general user.
- `PUT /devices/{deviceId}/destinations/{chatId}/settings` updates description,
  notification delay, and daily/weekly/monthly report flags.

Missing and unassigned devices return 404 on update. ChatId, IsActive, roles, and
provisioning credentials cannot be edited through the device settings endpoint.
There is no public registration or claiming of devices.

The existing device table is scanned in internal pages of 100 records and filtered
against the authenticated user's grants inside the API. Pagination keys stay on
the server. Add a dedicated access index when the fleet grows. DeviceConfig rows
are updated in place without replacing attributes consumed by the workers.

## Deploy to dev

Use AWS credentials for the account containing `PingLight.dev.DeviceConfigs`,
.NET 10 SDK, PowerShell, and Serverless Framework 4. Install the deployment CLI
with `npm install -g serverless@4` and authenticate with `serverless login`
(or configure a Serverless access/license key). Use the global `serverless`
command for this API; the frontend's local CLI still uses v3.

First deploy the existing Lambda service once so its CloudFormation stack exports
the shared REST API ID and root resource ID:

~~~powershell
cd ../Lambdas
./build.ps1
serverless deploy --stage dev
~~~

Then, from `src/PingLight.WebApi`:

~~~powershell
./build.ps1
serverless package --stage dev
# Review the generated CloudFormation before deploying.
serverless deploy --stage dev
~~~

This creates the separate `pinglight-management-dev` stack, but attaches its
`/devices` and `/users` resources to the REST API owned by `pinglight-dev`. It does
not create another API Gateway. The existing custom-domain mapping therefore makes
the routes available below `https://dev.api.pinglight.xyz`. The management stack
also creates a Cognito authorizer, user pool, public PKCE client, hosted login
domain, and retained `PingLight.dev.Users` DynamoDB table. It references the
existing DeviceConfigs table without replacing it.

Dev login redirects allow `https://dev.pinglight.xyz` and
`http://localhost:4201`. For another stage, explicitly pass
`--param="frontendOrigin=https://your-domain"`. The user pool and Users table are
retained on stack deletion or replacement.

After deployment, from `src/Frontend`:

~~~powershell
./configure-api.ps1 -Stage dev
npm ci
npm run build:ssr
npx serverless deploy --stage dev
~~~

This preserves the existing Angular SSR, Express, Lambda, API Gateway hosting and
domain. `configure-api.ps1` reads the management stack's `ApiUrl` output, which is
`https://dev.api.pinglight.xyz` for dev. `app-config.json` contains only public
endpoints and client IDs. Generate it before building.

Sign-in uses authorization code + PKCE through `oidc-client-ts`. Tokens stay in
memory; session storage holds only the temporary login transaction. Existing
access tokens can remain valid for up to 15 minutes after sign-out.

## Bootstrap the first system administrator

Sign up, verify the email address, sign in, and allow the dashboard to load once.
This creates the user's record in `PingLight.<stage>.Users`. In the DynamoDB console,
edit that record and set `IsSystemAdmin` to the DynamoDB **Boolean** value `true`.
Reload the dashboard.

The API preserves the flag on later requests because registration initializes it
only if the attribute does not exist. There is deliberately no endpoint for
changing `IsSystemAdmin`; edit the flag directly in the Users table for now.

The administrator can then grant devices from the dashboard. The user list comes
from Cognito, so verified accounts appear even before their first dashboard visit.
`DeviceGrants` contains encoded composite keys and should normally be managed
through the dashboard.

## Local development

Configure user secrets or environment variables without committing secrets:

~~~powershell
$env:Cognito__Authority = 'https://cognito-idp.eu-central-1.amazonaws.com/USER_POOL_ID'
$env:Cognito__ClientId = 'PUBLIC_APP_CLIENT_ID'
$env:Devices__TableName = 'PingLight.dev.DeviceConfigs'
$env:Users__TableName = 'PingLight.dev.Users'
$env:AllowedOrigins__0 = 'http://localhost:4201'
dotnet run --no-launch-profile --urls http://localhost:5063
~~~

Generate the frontend configuration, change only `apiUrl` to
`http://localhost:5063`, and run `npm start`. There is no authentication bypass;
local API access requires AWS credentials and a real Cognito access token.

## Verification

~~~powershell
dotnet test ../Tests/PingLight.WebApi.Tests/PingLight.WebApi.Tests.csproj
cd ../Frontend
npm run build:ssr
npm run test:ssr
npm test -- --watch=false --browsers=ChromeHeadless
~~~
