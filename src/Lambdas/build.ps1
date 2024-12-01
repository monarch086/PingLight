Write-Output "Packaging services...";

Set-Location ./PingLight.Test
dotnet restore
dotnet lambda package --configuration Release --framework net8.0 --output-package bin/Release/net8.0/deploy-package-test.zip
Write-Output ">>> Finished packaging PingLight.Test";
Set-Location ..

Set-Location ./PingLight.GatherApi.Lambda
dotnet restore
dotnet lambda package --configuration Release --framework net8.0 --output-package bin/Release/net8.0/deploy-package-gather-api.zip
Write-Output ">>> Finished packaging PingLight.GatherApi.Lambda";
Set-Location ..

Set-Location ./PingLight.AgregateChanges.Lambda
dotnet restore
dotnet lambda package --configuration Release --framework net8.0 --output-package bin/Release/net8.0/deploy-package-aggregate-changes.zip
Write-Output ">>> Finished packaging PingLight.AggregateChanges.Lambda";
Set-Location ..

Set-Location ./PingLight.DailyStats.Lambda
dotnet restore
dotnet lambda package --configuration Release --framework net8.0 --output-package bin/Release/net8.0/deploy-package-daily-stats.zip
Write-Output ">>> Finished packaging PingLight.DailyStats.Lambda";
Set-Location ..

Set-Location ./PingLight.WeeklyStats.Lambda
dotnet restore
dotnet lambda package --configuration Release --framework net8.0 --output-package bin/Release/net8.0/deploy-package-weekly-stats.zip
Write-Output ">>> Finished packaging PingLight.WeeklyStats.Lambda";
Set-Location ..

Set-Location ./PingLight.MonthlyStats.Lambda
dotnet restore
dotnet lambda package --configuration Release --framework net8.0 --output-package bin/Release/net8.0/deploy-package-monthly-stats.zip
Write-Output ">>> Finished packaging PingLight.MonthlyStats.Lambda";
Set-Location ..

Set-Location ./PingLight.TurnOffNotifications.Lambda
dotnet restore
dotnet lambda package --configuration Release --framework net8.0 --output-package bin/Release/net8.0/deploy-package-turn-off-notifications.zip
Write-Output ">>> Finished packaging PingLight.TurnOffNotifications.Lambda";
Set-Location ..

Write-Output ">>> >>> >>> Finished all services.";