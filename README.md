# Cattle Management

The cattle management system provides an HTTP API to organize herds, identify animals by ear tag, and track their current health status. Built with .NET 10 and C# 14, it separates business rules from storage and HTTP concerns through Onion Architecture and returns centralized RFC 7807 Problem Details responses for errors.

## Product at a glance

People responsible for a cattle operation can register herds and animals, look up an animal's herd and health status, and update that status as its condition changes. Case-insensitive ear-tag checks prevent duplicate registrations, while validation rejects missing registration data and future birth dates.

The current release stores records in memory; restarting the server clears them. The Blazor home page provides an API overview. A complete management interface, durable storage, authentication, and reports are not implemented.

Read the [product and technical guide](docs/product-and-technical-guide.md) for the operational workflow, API contract, architecture, and verification strategy. The [engineering decision log](docs/engineering-decisions.md) explains each design choice, its purpose, evidence, and limitations.

## Requirements

- .NET 10 SDK for local builds, or Docker Desktop for container builds
- Postman or Newman to run the included API collection

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

`Herd` and `Animal` inherit from `BaseEntity`, which assigns a GUID and a UTC creation timestamp. Each animal references its herd and starts with a healthy status. The current in-memory store resets when the process restarts.

In `Presentation.API/Program.cs`, the stateless ear-tag normalizer is a singleton, the registration validator is transient, and the catalog service and repository are scoped to an HTTP request. The repository depends on a synchronized singleton in-memory store so a created animal can be read in a later request. This store has no scoped dependencies. A database context is not implemented; a future persistence adapter should register its `DbContext` as scoped.

## Architectural foundations

**Onion Architecture and dependency inversion.** Business rules belong at the center, so `Core.Domain` contains `Herd`, `Animal`, and `BaseEntity` without references to ASP.NET Core or persistence packages. `Core.Application` depends on the domain and defines `ICattleRepository`, the contract its use case needs. `Infrastructure` implements that contract, while `Presentation.API` connects the implementation and HTTP controllers in `Program.cs`. The HTTP call flows from controller to use case to repository, but the compiled project references point toward the core. A later database implementation can replace the in-memory repository without changing the domain model. This follows the dependency direction described in [Palermo's Onion Architecture](https://jeffreypalermo.com/2008/07/the-onion-architecture-part-1/).

**Dependency injection and lifetimes.** ASP.NET Core creates a scope for each request. The catalog service and repository are `Scoped` because they coordinate one request; the validator is `Transient` because it has no shared state and can be created for each resolution; the tag normalizer is `Singleton` because it is stateless and safe to reuse. `InMemoryCattleStore` is also a singleton so separate requests see the same cattle records. It synchronizes dictionary access and depends on no scoped service, avoiding a captive dependency. Its contents are not durable. The integration test resolves services in two scopes to verify these lifetimes. See [Microsoft's service lifetime guidance](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection/service-lifetimes).

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
docker build -t cattle-management .
docker run --rm -p 18080:8080 -e ASPNETCORE_ENVIRONMENT=Development -e DisableHttpsRedirection=true cattle-management
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

## Quality checks

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

## API verification with Postman

Import [the cattle management API collection](postman/Cattle-Management.postman_collection.json), set `baseUrl` for your run mode, and run the requests in order. The collection creates a herd and an animal, retrieves the animal, changes its health status, and checks business and sample errors. The demo endpoint is available only when `ASPNETCORE_ENVIRONMENT=Development`.

To run the same collection from a terminal while the application is running on port 18080:

```bash
npx --yes newman run postman/Cattle-Management.postman_collection.json
```

The **Unexpected exception returns safe 500 Problem Details** request checks the response status, `Content-Type: application/problem+json`, standard fields, and absence of internal exception details. These assertions make the error contract reproducible for API consumers and maintainers.
