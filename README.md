# PingLight

App for pinging some host and posting info to Telegram channel. Can be useful for tracking host or electricity availability.

## Structure

- PingLight.Core - assembly with core logic of pinging and posting
- PingLight.Lambda - shell for making posts from AWS Lambda (not finished)

## Building and publishing

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
