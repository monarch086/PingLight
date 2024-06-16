Write-Output "Packaging services...";

Set-Location ./PingLight.Test
dotnet restore
dotnet lambda package --configuration Release --framework net6.0 --output-package bin/Release/net6.0/deploy-package.zip
Write-Output ">>> Finished packaging PingLight.Test";
Set-Location ..

Set-Location ./PingLight.GatherApi.Lambda
dotnet restore
dotnet lambda package --configuration Release --framework net6.0 --output-package bin/Release/net6.0/deploy-package.zip
Write-Output ">>> Finished packaging PingLight.GatherApi.Lambda";
Set-Location ..

Write-Output ">>> >>> >>> Finished all services.";