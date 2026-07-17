# Api.IntegrationTests

PostgreSQL-backed API integration tests using Testcontainers, `WebApplicationFactory`, and Respawn.

## Prerequisites

- Docker Desktop (or another Docker engine) must be running
- First run downloads the `postgres:16-alpine` image

## Run

```bash
dotnet test Backend/BandFounder/Test/Api.IntegrationTests
```

Unit tests in Domain/Services/Infrastructure do not require Docker.
