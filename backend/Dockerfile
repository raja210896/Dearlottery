# Production image for Render. Local dev is unaffected — this is only used by the Render build.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY LotteryAnalytics.Api.csproj ./
RUN dotnet restore "LotteryAnalytics.Api.csproj"
COPY . .
RUN dotnet publish "LotteryAnalytics.Api.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_ENVIRONMENT=Production
# Render injects PORT at runtime; Program.cs binds to it via the PORT env var.
ENTRYPOINT ["dotnet", "LotteryAnalytics.Api.dll"]
