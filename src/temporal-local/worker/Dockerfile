FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

COPY RiskWorker.csproj .
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./
USER $APP_UID
ENTRYPOINT ["dotnet", "RiskWorker.dll"]
