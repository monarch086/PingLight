# PingLight

PingLight tracks device and electricity availability, records turn-off periods, and
sends Telegram notifications. The Angular dashboard lets users manage their assigned
devices; the ASP.NET Core management API validates Cognito tokens and enforces access.

## Services

- `src/Lambdas` contains the ingestion, aggregation, statistics, and notification
  Lambdas. Its `pinglight-<stage>` stack owns the shared REST API Gateway and the
  existing direct Lambda Function URLs used by some devices.
- `src/PingLight.WebApi` contains the management API. Its
  `pinglight-management-<stage>` stack attaches `/devices` and `/users` routes to
  that shared API and owns the Cognito resources and Users table.
- `src/Frontend` contains the Angular SSR dashboard. Its
  `pinglight-front-<stage>` stack serves `dev.pinglight.xyz` or `pinglight.xyz`.

The services target .NET 10 and Node.js 24. See the
[management API](src/PingLight.WebApi/README.md) and
[frontend](src/Frontend/README.md) guides for setup and local development.

## Deploy

Use PowerShell (`pwsh` on Linux or macOS), the .NET 10 SDK, Node.js 24.15 or newer,
AWS credentials for the PingLight account in `eu-central-1`, and Serverless
Framework 4 for the .NET services. Install it with `npm install -g serverless@4`
and authenticate with `serverless login` or a Serverless access/license key. The
frontend uses its own locally installed Serverless Framework 3 via `npx`.

Deploy changes to `dev` first. Set `$Stage = 'prod'` for production after checking
the dev deployment. Run these commands from the repository root in this order when
all three services have changed:

```powershell
$Stage = 'dev' # or 'prod'

Push-Location src/Lambdas
./build.ps1
serverless deploy --stage $Stage
Pop-Location

Push-Location src/PingLight.WebApi
./build.ps1
serverless deploy --stage $Stage
Pop-Location

Push-Location src/Frontend
npm ci
./configure-api.ps1 -Stage $Stage
npm run build:ssr
npm run lint
npm run test:ssr
npx serverless deploy --stage $Stage
Pop-Location
```

Deploy only the services affected by a change. Keep the order above when a later
service depends on an earlier one: the management stack imports the core stack's
REST API identifiers, and `configure-api.ps1` reads the management stack outputs.
A dashboard-only change needs only the frontend block. Generate
`src/Frontend/src/assets/app-config.json` for the target stage **before every
frontend build**; it contains public API and Cognito IDs and should not be committed
with environment-specific values. A production build made with dev config will
otherwise call the dev API.

For a new stage, deploy the core stack before management and set the stage's
frontend origin and host parameters described in the service guides. On initial
setup, [bootstrap the first system administrator](src/PingLight.WebApi/README.md#bootstrap-the-first-system-administrator)
after the management and frontend stacks are live.

Production devices may call the core Lambda Function URLs directly. Before changing
`src/Lambdas/serverless.yml` or deploying the core stack to production, run
`serverless package --stage prod` from `src/Lambdas` and compare the generated
CloudFormation template with the deployed stack for Lambda or Function URL
replacement. Record the
URLs for `pinglight-prod-test`, `pinglight-prod-gather-api`, and
`pinglight-prod-gather-changes-api` with `aws lambda get-function-url-config`, then
verify the same URLs after deployment. Do not accept replacement of those resources.

After deployment, check the CloudFormation stacks are complete, open the dashboard
at `https://dev.pinglight.xyz` or `https://pinglight.xyz`, and exercise the affected
flow. The dev management API is at `https://dev.api.pinglight.xyz`; production uses
`https://api.pinglight.xyz`.

## Author

Ideas for improvements: <sbarsuk88@gmail.com>
