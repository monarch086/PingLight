$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot
try {
    dotnet publish PingLight.WebApi.csproj --configuration Release --runtime linux-x64 --self-contained false --output bin/Release/net10.0/lambda
    if ($LASTEXITCODE -ne 0) { throw 'API publish failed.' }
    Compress-Archive -Path bin/Release/net10.0/lambda/* -DestinationPath bin/Release/management-api.zip -Force
} finally {
    Pop-Location
}
