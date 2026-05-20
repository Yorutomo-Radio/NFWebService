FROM mcr.microsoft.com/dotnet/sdk:8.0 AS builder
WORKDIR /app

COPY NightFriendsBot/. ./
RUN dotnet restore NightFriendsBot.csproj
RUN dotnet publish -c Release -o /out

FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=builder /out .
ENTRYPOINT ["dotnet", "NightFriendsBot.dll"]