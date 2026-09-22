$ErrorActionPreference = 'Stop'
$packages = [ordered]@{
    'PingLight.Test' = 'test'
    'PingLight.GatherApi.Lambda' = 'gather-api'
    'PingLight.AgregateChanges.Lambda' = 'aggregate-changes'
    'PingLight.DailyStats.Lambda' = 'daily-stats'
    'PingLight.WeeklyStats.Lambda' = 'weekly-stats'
    'PingLight.MonthlyStats.Lambda' = 'monthly-stats'
    'PingLight.TurnOffNotifications.Lambda' = 'turn-off-notifications'
}
Push-Location $PSScriptRoot
try {
    dotnet tool restore
    if ($LASTEXITCODE -ne 0) { throw 'Lambda packaging tool restore failed.' }
    foreach ($project in $packages.Keys) {
        Push-Location $project
        try {
            $artifact = "bin/Release/net10.0/deploy-package-$($packages[$project]).zip"
            dotnet lambda package --configuration Release --framework net10.0 --output-package $artifact
            if ($LASTEXITCODE -ne 0) { throw "Packaging $project failed." }
        } finally { Pop-Location }
    }
} finally { Pop-Location }
Write-Output 'All Lambda packages are ready.'
