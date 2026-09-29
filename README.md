# Cattle Management

This repository contains the Phase 1 foundation for the cattle management system selected for Group 3. It uses .NET 10 and C# 14, models herds and animals, exposes a small HTTP API, and returns centralized RFC 7807 Problem Details responses for errors.

## Product at a glance

The product is intended to help a cattle operation keep an identifiable record of each animal, its herd, and its current health status. Phase 1 is a working API prototype: it can register herds and animals, reject duplicate ear tags, change an animal's health status, and return predictable error responses. Data currently lives in memory and disappears when the process restarts. The existing Blazor page is an overview, not a complete management interface.

For the product explanation, architecture rationale, exact API behavior, rubric traceability, and a defense walkthrough, read the [Phase 1 product and technical guide](docs/phase1-product-and-technical-guide.md). The [engineering decision log](docs/engineering-decisions.md) records what changed, why, what it enables, how it was verified, and its limits. Update both documents when subsequent phases change these claims.

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
| `Infrastructure` | Scoped repository backed by a thread-safe in-memory store | `Core.Application`, `Core.Domain` |
| `Presentation.API` | HTTP controllers, middleware, and the existing Blazor host | `Core.Application`, `Infrastructure` |
| `Core.Tests` | Unit and HTTP integration tests | Production projects under test |

`Herd` and `Animal` inherit from `BaseEntity`, which assigns a GUID and a UTC creation timestamp. Each animal references its herd and starts with a healthy status. The in-memory store is temporary for Phase 1; data is reset when the process restarts.

In `Presentation.API/Program.cs`, the stateless ear-tag normalizer is a singleton, the registration validator is transient, and the catalog service and repository are scoped to an HTTP request. The repository depends on a thread-safe singleton in-memory store so a created animal can be read in a later request. This temporary store has no scoped dependencies. Phase 1 has no `DbContext`; the Phase 2 persistence implementation should register it as scoped. No CI workflow is defined here.

## Phase 1 foundations

**Onion Architecture and dependency inversion.** Business rules belong at the center, so `Core.Domain` contains `Herd`, `Animal`, and `BaseEntity` without references to ASP.NET Core or persistence packages. `Core.Application` depends on the domain and defines `ICattleRepository`, the contract its use case needs. `Infrastructure` implements that contract, while `Presentation.API` connects the implementation and HTTP controllers in `Program.cs`. The HTTP call flows from controller to use case to repository, but the compiled project references point toward the core. A later database implementation can replace the in-memory repository without changing the domain model. This follows the dependency direction described in [Palermo's Onion Architecture](https://jeffreypalermo.com/2008/07/the-onion-architecture-part-1/).

**Dependency injection and lifetimes.** ASP.NET Core creates a scope for each request. The catalog service and repository are `Scoped` because they coordinate one request; the validator is `Transient` because it has no shared state and can be created for each resolution; the tag normalizer is `Singleton` because it is stateless and safe to reuse. The temporary `InMemoryCattleStore` is also a singleton so separate requests see the same demo data. It is synchronized and depends on no scoped service, avoiding a captive dependency. It is not persistent storage and will be replaced during Phase 2. The integration test resolves services in two scopes to verify these lifetimes. See [Microsoft's service lifetime guidance](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection/service-lifetimes).

**Centralized RFC 7807 errors.** `ExceptionMiddleware` runs before controller endpoints, catches their unhandled exceptions, and maps missing resources to 404, invalid operations to 400, and unexpected failures to 500. Every mapped response uses `application/problem+json` and the standard `type`, `title`, `status`, `detail`, and `instance` members. For 500, `detail` is generic so the response does not reveal the exception message or stack trace. The demo controller, HTTP integration tests, and Postman collection show this behavior. See [RFC 7807](https://www.rfc-editor.org/info/rfc7807/).

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
| `POST` | `/api/herds` | Create a herd |
| `GET` | `/api/herds` | List herds |
| `GET` | `/api/herds/{id}` | Get one herd |
| `POST` | `/api/animals` | Register an animal linked to a herd |
| `GET` | `/api/animals` | List animals |
| `GET` | `/api/animals/{id}` | Get one animal |
| `PATCH` | `/api/animals/{id}/health-status` | Change an animal's health status (`0` healthy, `1` under observation, `2` in treatment) |
| `GET` | `/api/demo/errors/{kind}` | Trigger a sample error in Development only |

`ExceptionMiddleware` is registered before the controllers in `Program.cs`. It maps `KeyNotFoundException` to 404, `InvalidOperationException` and invalid arguments to 400, and unexpected exceptions to 500. Responses have the `application/problem+json` content type and include `type`, `title`, `status`, `detail`, and `instance`. HTTP 500 responses hide exception messages and stack traces. `ApiIntegrationTests` starts the real ASP.NET Core application in memory and verifies all three responses through HTTP, as well as the DI lifetimes and cattle endpoints.

## Phase 1 rubric evidence

| Requirement | Implementation | Verification |
| --- | --- | --- |
| Four Onion layers and a pure domain | Four production projects in `DAW.slnx`; `Core.Domain` has no project references | `dotnet build DAW.slnx` |
| Related entities with GUID and UTC creation | `BaseEntity`, `Herd`, and `Animal` | `DomainAndApplicationTests` |
| Appropriate DI lifetimes | Registrations in `Presentation.API/Program.cs` | `DependencyInjectionUsesExpectedLifetimes` |
| Registered RFC 7807 middleware | `ExceptionMiddleware` before controllers in `Program.cs` | `RegisteredPipelineReturnsProblemDetails` and the Postman collection |

For a quick cURL check:

```bash
curl -i http://localhost:18080/api/demo/errors/not-found
curl -i http://localhost:18080/api/demo/errors/invalid-operation
curl -i http://localhost:18080/api/demo/errors/unexpected
```

The expected status codes are 404, 400, and 500. The last response contains only a generic `detail`.

## Postman evidence

Import [the Phase 1 Postman collection](postman/DAW-Phase1.postman_collection.json), set `baseUrl` for your run mode, and run the requests in order. The collection creates a herd and an animal, then checks business and sample errors. The demo endpoint is available only when `ASPNETCORE_ENVIRONMENT=Development`.

To run the same collection from a terminal while the application is running on port 18080:

```bash
npx --yes newman run postman/DAW-Phase1.postman_collection.json
```

For the assignment screenshot, open the **Unexpected exception returns safe 500 Problem Details** request in Postman after running it. Capture the status, `Content-Type: application/problem+json`, and JSON body in the same image. Attach the collection file and repository URL to the course submission.
