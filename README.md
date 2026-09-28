# Flowzy API (.NET 8)

ASP.NET Core reimplementation of the F-Spark backend. The solution keeps three physical layers:

- `Flowzy.Api` — HTTP contract, authentication, middleware and local hosting.
- `Flowzy.Service` — application rules and API contracts.
- `Flowzy.Repository` — EF Core/Npgsql persistence and schema migration runner.

## Run locally with Docker

Requirements: Docker Desktop with Compose.

```powershell
docker compose up --build
```

The first startup creates PostgreSQL 16, executes the original SQL migrations V1–V33 followed by Flowzy's V34 (persistent token revocations), and seeds the local admin account. Default local addresses:

- API: `http://localhost:8080`
- health: `http://localhost:8080/actuator/health`
- OpenAPI: `http://localhost:8080/v3/api-docs`
- Swagger UI: `http://localhost:8080/swagger-ui.html`

Default development login is `admin@fspark.com` / `AdminPassword123`. The account deliberately starts with `mustChangePassword=true`, matching the Java backend. Change it through `PATCH /api/profile/me/password` before accessing other features.

To override local values, copy `.env.example` to `.env`. Real secrets and Java `.env`, `.git`, `target` content are intentionally excluded.

## Connect with DBeaver

Use the PostgreSQL driver with host `127.0.0.1`, port `5433`, database `flowzy`, username `flowzy`, and the default local password `flowzy_local_password` (or your overridden Compose credentials).

Docker publishes PostgreSQL on host port 5433 to avoid conflicting with a Windows PostgreSQL service on port 5432. The API container still connects internally to `postgres:5432`; `dotnet run` connects to `localhost:5433`. The existing named database volume is preserved when containers are recreated.

## Debug with `dotnet run`

Start only PostgreSQL, then run the API:

```powershell
docker compose up -d postgres
dotnet run --project src/Flowzy.Api
```

The development launch profile listens on port 8080 and uses `appsettings.Development.json`.

## Frontend

No frontend source change is needed. In `D:/f-spark-FE/F-spark-frontend`, set only:

```text
NEXT_PUBLIC_API_BASE_URL=http://localhost:8080
```

## Verification

```powershell
dotnet build Flowzy.sln
dotnet test Flowzy.sln
docker compose ps
```

The Java OpenAPI oracle is stored at `contracts/fspark-openapi.json`. It contains 153 paths and 240 schemas. The served `/v3/api-docs` preserves its operations and schemas while applying Flowzy branding and a same-origin server URL; generated C# DTOs live in `Flowzy.Service/Contracts/Generated`.

`dotnet test` also starts isolated PostgreSQL 16 Testcontainers, applies V1–V34 from an empty database, seeds an admin and verifies the login contract. Docker must therefore be running for the integration suite.

## Production deployment

Use [the production deployment guide](docs/PRODUCTION_DEPLOYMENT.md), `compose.production.yml` and `.env.production.example`. The production stack uses a single non-root API instance behind Caddy HTTPS, with an external PostgreSQL 16 database requiring certificate verification. It does not reuse the local Compose database or demonstration credentials. See the guide for the opt-in Docker-image test covering real TLS, XLSX exports, persistent logout and backup/restore.
