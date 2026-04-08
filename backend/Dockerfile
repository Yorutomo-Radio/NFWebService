# ビルドステージ
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS builder
WORKDIR /app
COPY Yorutomo/*.csproj ./
RUN dotnet restore
COPY Yorutomo/ ./
RUN dotnet publish -c Release -o /out

# 実行ステージ（軽量）
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=builder /out .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Yorutomo.dll"]