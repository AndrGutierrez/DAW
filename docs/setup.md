# Guía de setup

Guía corta para levantar el sistema desde cero.

## Requisitos
- **.NET 10 SDK** (o Docker Desktop).
- **Docker** (para PostgreSQL y Nginx).
- Opcional: **Postman/Newman** para las pruebas de API.

## 1. Secretos (`.env`)
```bash
cp .env.example .env
```
Edita `.env` y define valores reales:
- `POSTGRES_PASSWORD`
- `JWT_KEY` (mínimo 32 caracteres; `openssl rand -base64 48`)
- `SEED_ADMIN_PASSWORD`

`.env` no se versiona (está en `.gitignore` y `.dockerignore`). La app **no arranca** si `Jwt:Key` falta o es el placeholder.

## 2. Base de datos
```bash
docker compose up -d db
```

Si vas a correr la app **sin Docker** (`dotnet run`), define los secretos con `dotnet user-secrets` (o expórtalos como variables de entorno):
```bash
dotnet user-secrets --project src/Presentation.API set "Jwt:Key" "<clave>"
dotnet user-secrets --project src/Presentation.API set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=daw;Username=daw;Password=<postgres-password>"
dotnet user-secrets --project src/Presentation.API set "Seed:AdminPassword" "<admin-password>"
```

## 3. Migraciones y seed
```bash
dotnet tool restore
dotnet ef database update --project src/Infrastructure --startup-project src/Presentation.API
dotnet run --project src/Presentation.API -- --seed
```
El seed es **idempotente**: crea roles, permisos, especies/razas, el usuario admin (sincroniza su contraseña con `SEED_ADMIN_PASSWORD`) y **datos demo** (finca, potreros, lotes y animales con pesajes).

## 4. Ejecutar
- **Local**: `dotnet run --project src/Presentation.API --launch-profile http` → `http://localhost:5269`
- **Docker (todo)**: `docker compose up --build` → `http://localhost:18080`

## 5. URLs útiles
- API: `/` · Swagger: `/swagger` · ERD: `/erd/`
- Credenciales del seed: `admin` / valor de `SEED_ADMIN_PASSWORD`

## 6. Pruebas
```bash
dotnet test DAW.slnx
npx --yes newman run postman/Cattle-Management.postman_collection.json \
  -e postman/Daw.postman_environment.json
```

## 7. Notas
- **Reiniciar datos**: `docker compose down -v` (borra el volumen de PostgreSQL) y vuelve a migrar/sembrar.
- **Rotar la contraseña del admin**: edita `SEED_ADMIN_PASSWORD` y ejecuta el seed otra vez.
- **Ver SQL de EF Core**: sube `Microsoft.EntityFrameworkCore.Database.Command` a `Information` en los `appsettings`.

## 8. CI (GitHub Actions)
Cada **pull request** ejecuta `.github/workflows/ci.yml`:
- **Unit/integration tests**: `dotnet test DAW.slnx`.
- **Postman (Newman)**: levanta PostgreSQL efímero, aplica migraciones, siembra datos y corre la colección con el entorno.

El workflow genera los secretos en tiempo de ejecución (Postgres, JWT y admin), así que no hace falta configurar secrets en el repositorio.

