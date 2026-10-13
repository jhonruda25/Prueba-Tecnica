# Fundación delamujer — API de Catálogo de Productos y Stock
### Prueba Técnica: Analista Desarrollador de Aplicaciones

[![CI Pipeline](https://github.com/jhonruda25/Prueba-Tecnica/actions/workflows/ci.yml/badge.svg)](https://github.com/jhonruda25/Prueba-Tecnica/actions/workflows/ci.yml)
[![.NET 10 LTS](https://img.shields.io/badge/.NET-10.0%20LTS-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Docker Alpine](https://img.shields.io/badge/Docker-Alpine%20Linux-2496ED?logo=docker)](https://hub.docker.com/_/alpine)
[![PostgreSQL](https://img.shields.io/badge/Database-PostgreSQL%2016-4169E1?logo=postgresql)](https://www.postgresql.org/)

> API REST backend de grado de producción diseñada como núcleo transaccional de alta concurrencia para la gestión de catálogo de productos e inventario, implementada con **Clean Architecture** en **.NET 10 (C# 14)** y persistencia relacional en **PostgreSQL**.

> 🌐 **Repositorio en GitHub:** [https://github.com/jhonruda25/Prueba-Tecnica](https://github.com/jhonruda25/Prueba-Tecnica)  
> 🚀 **Swagger UI en Producción (URL Pública Funcional):** [https://prueba-tecnica-ahee.onrender.com/](https://prueba-tecnica-ahee.onrender.com/)  
> 🩺 **Health Check en Producción:** [https://prueba-tecnica-ahee.onrender.com/health](https://prueba-tecnica-ahee.onrender.com/health)

---

## 1. Justificación y Decisiones de Arquitectura

### 1.1. Selección Tecnológica (.NET 10 LTS en C# 14)
* **Requisito de la prueba:** *"Lenguaje y framework: .NET (C#) reciente."*
* **Decisión:** Se adoptó **.NET 10**, la versión **LTS (Long Term Support)** vigente con ciclo de vida activo hasta finales de 2028.
* **Justificación de ingeniería:** Las versiones anteriores (.NET 8 y .NET 9) alcanzaron el fin de soporte en noviembre de 2026. Levantar una solución de producción para una entidad microfinanciera sobre un runtime próximo a expirar representaría un riesgo operativo de obsolescencia. .NET 10 garantiza parches corporativos a largo plazo, mejoras de rendimiento en el runtime y compatibilidad completa con C# 14.

### 1.2. Arquitectura Limpia (Clean Architecture)
El proyecto se desacopló en capas con aislamiento estricto de dependencias hacia el centro (Regla de Dependencia de Uncle Bob):

```
└── src/
    ├── Delamujer.ProductCatalog.Domain/          # Núcleo: Entidad Product, reglas invariantes, excepciones y contratos
    ├── Delamujer.ProductCatalog.Application/     # Casos de uso: DTOs, validación FluentValidation, servicios de orquestación
    ├── Delamujer.ProductCatalog.Infrastructure/  # Adaptadores: EF Core 10, DbContext, mapeos PostgreSQL, migraciones
    └── Delamujer.ProductCatalog.Api/             # Entrada: Controllers RESTful, Swagger OpenAPI, Middleware RFC 7807
└── tests/
    ├── Delamujer.ProductCatalog.UnitTests/       # Pruebas unitarias de dominio y aplicación (xUnit + FluentAssertions)
    └── Delamujer.ProductCatalog.IntegrationTests/# Pruebas de integración E2E y pruebas de estrés de concurrencia
```

---

## 2. El Desafío Transaccional y Manejo de Concurrencia

### El Problema
La consigna establece: *"Esta API será el núcleo transaccional consumido por múltiples plataformas concurrentes. Debe permitir administrar productos, controlar su disponibilidad de forma segura..."*

En un modelo ingenuo (leer producto en memoria, restar en C# y guardar cambios), dos solicitudes concurrentes que ingresan al mismo tiempo con `Stock = 5` e intentan restar 4 unidades provocarían una **condición de carrera (Lost Update)**, dejando el stock en 1 o en valores negativos.

### La Solución Implementada
Se implementó una estrategia de triple barrera:
1. **Restricción a nivel de motor de base de datos (Check Constraint):**
   * Se configuró en la tabla `products` la regla `CHECK ("Stock" >= 0)` y `CHECK ("Price" > 0)`, haciendo que el motor de base de datos rechace físicamente cualquier intento de balance negativo.
2. **Actualización Atómica Directa (`ExecuteUpdateAsync`):**
   * En lugar de cargar la entidad en memoria, el ajuste de stock ejecuta una sentencia SQL atómica parametrizada:
     ```sql
     UPDATE "products"
     SET "Stock" = "Stock" + @delta,
         "ConcurrencyToken" = @token,
         "UpdatedAtUtc" = @now
     WHERE "Id" = @id AND ("Stock" + @delta) >= 0;
     ```
   * Si las filas afectadas son `0`, el repositorio evalúa si el producto no existe (devolviendo `null` para un `404 Not Found`) o si la deducción habría dejado el stock negativo (lanzando `InsufficientStockException` para devolver un `400 Bad Request` semántico con `ProblemDetails`).
3. **Limitación de Tasa Nativa (Rate Limiting en .NET 10):**
   * Se incorporó `AddRateLimiter` con partición por cliente (120 req/minuto) que devuelve `429 Too Many Requests` formateado en RFC 7807 ante ráfagas abusivas, mitigando denegación de servicio transaccional.
   * **Prueba comprobada:** Se incluyeron pruebas de integración multihilo donde **20 hilos simultáneos** descuentan unidades en el mismo milisegundo sin perder una sola actualización.

---

## 3. Seguridad Defensiva y Arquitectura Perimetral con Cloudflare

Siguiendo el principio de **Defensa en Profundidad (Defense-in-Depth)** y mejores prácticas del sector financiero:

### 3.1. Seguridad a Nivel de Aplicación (.NET 10)
* **Cabeceras de Blindaje HTTP:**
  * `X-Content-Type-Options: nosniff` (previene ataques de confusión MIME).
  * `X-Frame-Options: DENY` (inmune a clickjacking).
  * `Referrer-Policy: strict-origin-when-cross-origin`.
  * `Permissions-Policy` restrictiva para APIs REST.
* **Manejo Centralizado de Excepciones:** Intercepción global sin filtración de stack traces técnicos en respuestas.
* **Health Checks con Sonda de Base de Datos:** `/health` monitorea activamente la conectividad con EF Core / PostgreSQL.

### 3.2. Estrategia de Borde con Cloudflare (Producción Corporativa)
Para un despliegue a escala de Fundación delamujer, la API está lista para integrarse con:
* **Cloudflare API Shield:** Validación estricta del contrato OpenAPI (`swagger.json`) en el Edge, descartando llamadas malformadas o payloads fuera de esquema antes de que lleguen a la infraestructura backend.
* **Cloudflare Rate Limiting Rules:** Reglas perimetrales específicas para `/api/products/*/stock` frenando intentos de agotamiento masivo de inventario.
* **Cloudflare Tunnel (`cloudflared`):** Permite exponer el contenedor Docker de forma saliente cifrada sin abrir puertos públicos entrantes en los servidores de la fundación.


---

## 3. Especificación de Endpoints y Semántica HTTP

La API expone respuestas HTTP semánticas y sigue el estándar **RFC 7807 (ProblemDetails)** para manejo uniforme de errores:

| Método | Endpoint | Descripción | Códigos HTTP |
| :--- | :--- | :--- | :--- |
| `POST` | `/api/products` | Crea un producto en el catálogo | `201 Created` (con cabecera `Location`), `400 Bad Request` |
| `GET` | `/api/products/{id}` | Obtiene el detalle de un producto | `200 OK`, `404 Not Found` |
| `GET` | `/api/products` | Listado paginado con filtros de búsqueda | `200 OK` |
| `PUT` | `/api/products/{id}` | Actualiza nombre, descripción y precio | `200 OK`, `400 Bad Request`, `404 Not Found` |
| `PATCH/PUT` | `/api/products/{id}/stock` | Suma o descuenta inventario atómicamente | `200 OK`, `400 Bad Request`, `404 Not Found` |
| `DELETE` | `/api/products/{id}` | Desactiva (soft-delete) el producto | `204 No Content`, `404 Not Found` |
| `GET` | `/health` | Health check del servicio | `200 OK` (`Healthy`) |

### Ejemplos de Peticiones

#### Crear Producto (`POST /api/products`)
```json
{
  "name": "Microcrédito AgroMujer",
  "description": "Financiamiento productivo para mujeres rurales",
  "price": 2500000.00,
  "initialStock": 50
}
```

#### Ajustar Stock (`PATCH /api/products/{id}/stock`)
```json
{
  "quantityDelta": -10
}
```
* Para sumar stock: usar valores positivos (ej. `15`).
* Para restar stock: usar valores negativos (ej. `-10`).

#### Respuesta de Error Semántica (`ProblemDetails` RFC 7807)
Si un descuento excede el inventario disponible:
```json
{
  "type": "https://fundaciondelamujer.com/errors/insufficient-stock",
  "title": "Insufficient Stock for Adjustment",
  "status": 400,
  "detail": "Cannot adjust stock for product '...'. Current stock is 5, requested change is -10. Resulting stock cannot be negative.",
  "instance": "/api/products/.../stock",
  "productId": "9b05d261-3b53-41c3-bb99-76e36f413e6e",
  "currentStock": 5,
  "requestedDelta": -10,
  "traceId": "0HNP9CEKHMVH6:00000001"
}
```

---

## 4. Ejecución de Pruebas Automatizadas

El proyecto cuenta con **47 pruebas automatizadas** (34 unitarias y 13 de integración):

```bash
dotnet test Delamujer.ProductCatalog.slnx
```

### Cobertura de Pruebas:
* **Pruebas de Dominio:** Validación de invariantes de negocio (precio mayor a cero, nombre obligatorio y longitud máxima, stock no negativo, transiciones de estado).
* **Pruebas de Aplicación:** Pruebas de validadores FluentValidation y del orquestador `ProductService` con dobles de prueba (`NSubstitute`).
* **Pruebas de Integración:** Pruebas completas E2E del ciclo de vida REST usando `WebApplicationFactory` sobre SQLite en memoria.
* **Pruebas de Estrés de Concurrencia:** Verificación multihilo paralela con `Task.WhenAll` garantizando que no existan carreras ni sobregiros de stock.

---

## 5. Instrucciones de Ejecución Local

### Opción A: Con Docker Compose (PostgreSQL listo para producción)
Requiere tener instalado Docker Desktop:

```bash
# 1. Clonar el repositorio
git clone <URL_DEL_REPOSITORIO>
cd Prueba-Tecnica

# 2. Levantar la API y la base de datos PostgreSQL
docker-compose up --build
```
* La API se iniciará en `http://localhost:8080`.
* Al ingresar a `http://localhost:8080` en el navegador, serás redirigido automáticamente a la interfaz interactiva de **Swagger UI**.
* La base de datos aplicará las migraciones y sembrará 5 productos de prueba automáticamente al arrancar.

---

### Opción B: Con .NET CLI directo (Desarrollo local con SQLite)
Si no tenés Docker instalado, la aplicación cuenta con un fallback local automático a SQLite en modo desarrollo:

```bash
# Ejecutar en entorno Development
dotnet run --project src/Delamujer.ProductCatalog.Api/Delamujer.ProductCatalog.Api.csproj --launch-profile "http"
```
* La API estará disponible en `http://localhost:5086` con Swagger en la raíz (`http://localhost:5086/`).

---

## 6. Despliegue en la Nube (URL Pública Funcional)

Para cumplir con el requerimiento de entrega con URL pública funcional, se recomienda el despliegue en **Railway** o **Render** utilizando el `Dockerfile` provisto:

### Opción A: Despliegue en Railway (Recomendado - 3 Minutos):
1. Ingresa a [railway.app](https://railway.app) e inicia sesión con tu cuenta de GitHub (**jhonruda25**).
2. Haz clic en **New Project** $\rightarrow$ **Provision PostgreSQL**.
   * Railway creará una base de datos PostgreSQL administrada al instante.
3. En el mismo proyecto, haz clic en **+ New** $\rightarrow$ **GitHub Repo** $\rightarrow$ selecciona **`jhonruda25/Prueba-Tecnica`**.
   * Railway detectará el `Dockerfile` multi-stage automáticamente.
4. En el servicio web (`Prueba-Tecnica`), ve a la pestaña **Variables** y agrega:
   * `DatabaseProvider` = `PostgreSQL`
   * `DATABASE_URL` = `${{Postgres.DATABASE_URL}}` *(Railway vincula la base de datos automáticamente)*
   * `ASPNETCORE_ENVIRONMENT` = `Production`
5. En la pestaña **Settings** $\rightarrow$ **Networking**, haz clic en **Generate Domain**.
   * Te generará una URL pública automática (ej: `https://prueba-tecnica-production-xxxx.up.railway.app`).
6. ¡Listo! El contenedor compilará, correrá las migraciones en PostgreSQL, sembrará los 5 productos iniciales y al abrir la URL pública en tu navegador verás directamente la interfaz interactiva de **Swagger UI**.

---

### Opción B: Despliegue en Render:
1. Sube este repositorio a tu cuenta de **GitHub** o **GitLab**.
2. Ingresa a [Render.com](https://render.com) e inicia sesión.
3. **Paso 1 (Base de Datos PostgreSQL):**
   * Click en **New +** > **PostgreSQL**.
   * Nombre: `delamujer-db`.
   * Base de datos: `delamujer_catalog`.
   * Plan: **Free**.
   * Copia la **Internal Database URL** (ej: `postgres://...`).
4. **Paso 2 (Web Service con Docker):**
   * Click en **New +** > **Web Service**.
   * Conecta tu repositorio de GitHub.
   * Runtime: **Docker**.
   * En **Environment Variables**, agrega:
     * `DatabaseProvider` = `PostgreSQL`
     * `DATABASE_URL` = `<URL_DE_TU_POSTGRES_COPIADA_EN_EL_PASO_3>`
     * `ASPNETCORE_ENVIRONMENT` = `Production`
5. Click en **Create Web Service**.
6. Render compilará el `Dockerfile` multi-stage, conectará la base de datos, correrá las migraciones y sembrará los datos automáticamente.
7. Tu API quedará disponible en una URL pública (ej: `https://delamujer-api.onrender.com/`), mostrando directamente Swagger UI en la raíz.

---

## 7. Estructura de Commits

Siguiendo las recomendaciones de la prueba técnica, el desarrollo se versionó de forma progresiva y descriptiva bajo la convención de *Conventional Commits*:

1. `chore: initialize solution structure with Clean Architecture in .NET 10`
2. `feat(domain): add Product entity, repository interfaces and business invariants with unit tests`
3. `feat(application): implement DTOs, ProductService, FluentValidation and unit tests`
4. `feat(infrastructure): implement EF Core DbContext, PostgreSQL mappings, atomic stock update and migrations`
5. `feat(api): expose REST endpoints, ProblemDetails middleware, Swagger UI and database seeding`
6. `test: add integration tests and stock concurrency validation under parallel stress`
7. `ci/cd: add multi-stage dockerfile, compose and deployment documentation`
8. `feat(security): add native rate limiting, security headers, database health checks and Cloudflare documentation`
9. `chore(api): add swagger route alias redirect to root documentation`
10. `ci/cd: migrate docker runtime to alpine linux with non-root user and zero build secrets`
11. `test: configure WebApplicationFactory isolated database provider and rate limiting bypass in testing`
12. `docs: add railway deployment guide to readme`
