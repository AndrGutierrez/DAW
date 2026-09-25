# IT Asset Inventory

This repository contains the Phase 1 foundation for an IT equipment inventory system in .NET 10 and C# 14. It models equipment and categories, exposes a small HTTP API, and returns centralized RFC 7807 Problem Details responses for errors.

## Requirements

- .NET 10 SDK for local builds, or Docker Desktop for container builds
- Postman to run the included collection and capture the required response screenshot

## Clone, build, and test

```bash
git clone https://github.com/AndrGutierrez/DAW.git
cd DAW
dotnet build DAW.slnx
dotnet test DAW.slnx
```

The solution contains four production projects and one test project:

| Project | Responsibility | Project references |
| --- | --- | --- |
| `Core.Domain` | Pure entities and business state | None |
| `Core.Application` | Use cases and repository interface | `Core.Domain` |
| `Infrastructure` | Thread-safe in-memory repository | `Core.Application`, `Core.Domain` |
| `Presentation.API` | HTTP controllers, middleware, and the existing Blazor host | `Core.Application`, `Infrastructure` |
| `Core.Tests` | Unit tests | Production projects under test |

`AssetCategory` and `ITAsset` inherit from `BaseEntity`, which assigns a GUID and a UTC creation timestamp. Each asset references its category. The in-memory repository is sufficient for this phase; data is reset when the process restarts.

In `Presentation.API/Program.cs`, the stateless asset-tag normalizer is transient, the catalog use case is scoped to a request, and the thread-safe in-memory repository is a singleton so data survives across requests. No CI workflow is defined here.

## Run the API

With the .NET 10 SDK:

```bash
dotnet run --project src/Presentation.API --launch-profile http
```

The local URL is `http://localhost:5269`. Change the Postman collection's `baseUrl` variable to this URL.

On Windows with Git Bash and Docker Desktop, start Docker and confirm that the engine is ready:

```bash
docker desktop start
docker version
```

`docker version` must show both a client and a server version. If it only shows the client, wait for Docker Desktop to finish starting and run `docker version` again. Then run the tests:

```bash
MSYS_NO_PATHCONV=1 docker run --rm -v "$(pwd -W):/src" -w /src mcr.microsoft.com/dotnet/sdk:10.0 dotnet test DAW.slnx -c Release
```

Then build and run the application:

```bash
docker build -t daw-phase1 .
docker run --rm -p 18080:8080 -e ASPNETCORE_ENVIRONMENT=Development -e DisableHttpsRedirection=true daw-phase1
```

The Docker URL is `http://localhost:18080`, which matches the included Postman collection. Stop the foreground container with Ctrl+C.

## API and error handling

| Method | Path | Purpose |
| --- | --- | --- |
| `POST` | `/api/categories` | Create an asset category |
| `GET` | `/api/categories` | List categories |
| `GET` | `/api/categories/{id}` | Get one category |
| `POST` | `/api/assets` | Register equipment linked to a category |
| `GET` | `/api/assets` | List equipment |
| `GET` | `/api/assets/{id}` | Get one asset |
| `GET` | `/api/demo/errors/{kind}` | Trigger a sample error in Development only |

`ExceptionMiddleware` maps `KeyNotFoundException` to 404, `InvalidOperationException` and invalid arguments to 400, and unexpected exceptions to 500. Responses have the `application/problem+json` content type and include `type`, `title`, `status`, `detail`, and `instance`. HTTP 500 responses hide exception messages and stack traces.

For a quick cURL check:

```bash
curl -i http://localhost:18080/api/demo/errors/not-found
curl -i http://localhost:18080/api/demo/errors/invalid-operation
curl -i http://localhost:18080/api/demo/errors/unexpected
```

The expected status codes are 404, 400, and 500. The last response contains only a generic `detail`.

## Postman evidence

Import [the Phase 1 Postman collection](postman/DAW-Phase1.postman_collection.json), set `baseUrl` for your run mode, and run the requests in order. The collection creates a category and an asset, then checks business and sample errors. The demo endpoint is available only when `ASPNETCORE_ENVIRONMENT=Development`.

For the assignment screenshot, open the **Unexpected exception returns safe 500 Problem Details** request in Postman after running it. Capture the status, `Content-Type: application/problem+json`, and JSON body in the same image. Attach the collection file and repository URL to the course submission.
