FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore Flowzy.sln
RUN dotnet publish src/Flowzy.Api/Flowzy.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0-bookworm-slim AS final
WORKDIR /app
ADD --chmod=644 https://www.postgresql.org/media/keys/ACCC4CF8.asc /usr/share/keyrings/postgresql.asc
COPY docker/postgresql.sources /etc/apt/sources.list.d/postgresql.sources
RUN apt-get update \
    && apt-get install -y --no-install-recommends postgresql-client-16 \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .
RUN mkdir -p /app/backups /app/uploads
EXPOSE 8080
ENTRYPOINT ["dotnet", "Flowzy.Api.dll"]
