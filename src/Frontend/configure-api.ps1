param(
    [ValidateSet('dev', 'prod')][string]$Stage = 'dev',
    [string]$Region = 'eu-central-1'
)
$ErrorActionPreference = 'Stop'
$stackJson = aws cloudformation describe-stacks --stack-name "pinglight-management-$Stage" --region $Region --output json
if ($LASTEXITCODE -ne 0) { throw 'Unable to read the management stack.' }
$stack = $stackJson | ConvertFrom-Json
$outputs = @{}
foreach ($output in $stack.Stacks[0].Outputs) { $outputs[$output.OutputKey] = $output.OutputValue }
foreach ($name in @('ApiUrl', 'CognitoClientId', 'CognitoUserPoolId')) {
    if (!$outputs[$name]) { throw "Missing stack output: $name" }
}
@{
    apiUrl = $outputs.ApiUrl
    clientId = $outputs.CognitoClientId
    userPoolId = $outputs.CognitoUserPoolId
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'src/assets/app-config.json') -Encoding utf8
Write-Output "Configured frontend for management API stage $Stage. Rebuild before deploying."
