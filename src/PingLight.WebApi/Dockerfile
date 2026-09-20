FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build-env
WORKDIR /app

COPY . .

RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:10.0
# ENV ASPNETCORE_URLS=http://*:8080
WORKDIR /app
COPY --from=build-env /app/out .

EXPOSE 8080

# "--launch-profile Docker"
ENTRYPOINT ["dotnet", "PingLight.WebApi.dll"]