# PingLight.WebApi Commands

```sh
# Build image:
docker build -t pinglight-webapi .

# Start database
docker compose up -d

# Add & apply migrations
dotnet ef migrations add "initial"
dotnet ef database update

# Start app
dotnet run
```
