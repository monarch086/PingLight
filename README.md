# PingLight

App for pinging some host and posting info to Telegram channel. Can be useful for tracking host or electricity availability.

## Structure

- PingLight.Core - assembly with core logic of pinging and posting
- PingLight.Lambda - shell for making posts from AWS Lambda (not finished)

## Building and publishing

The project targets .NET 10 LTS (including all Lambdas and tests) and Angular 21
LTS. Use a .NET 10 SDK and Node.js 24.15+ from the Node 24 LTS line. Lambda runtime
settings, Docker images and artifact paths are updated alongside the frameworks.
The Lambda build script restores its pinned Amazon.Lambda.Tools 7.0.0 locally.

For the Cognito sign-in and assigned-device management dashboard, see
[the management API setup](src/PingLight.WebApi/README.md). The frontend retains
its existing Angular SSR / Lambda hosting. The management API deploys as a
separate Lambda service and reads the existing device configuration table.

```ps1
cd .\src\Lambdas\
.\build.ps1
serverless deploy --stage [dev/prod] [--force]

# deploy particular function
serverless deploy function --function test --stage dev --force
```

## Install serverless dependencies

```powershell
serverless plugin install -n serverless-api-gateway-throttling
```

## Author

Any ideas of improvements please send to <sbarsuk88@gmail.com>
