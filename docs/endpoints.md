# Endpoints de la API

Contrato para que la Web y la API se puedan construir en paralelo. Si algo cambia, se actualiza aquí en el mismo Pull Request.

Estado: **hecho** = ya existe en el código, **pendiente** = falta construirlo.

Las rutas marcadas con acceso distinto de "Público" necesitan el token JWT en el encabezado `Authorization: Bearer <token>`. Se obtiene en `POST /api/auth/login`.

| Método | Ruta | Acceso | Qué hace | Estado |
|---|---|---|---|---|
| POST | `/api/auth/register` | Público | Crea una cuenta con rol User y devuelve el token | hecho |
| POST | `/api/auth/login` | Público | Devuelve el token JWT, cuándo vence y los datos del usuario | hecho |
| GET | `/api/auth/me` | Con sesión | Datos de quien tiene la sesión | hecho |
| GET | `/api/sources` | Público | Lista las fuentes | hecho |
| POST | `/api/sources` | Admin | Agrega una fuente | hecho |
| DELETE | `/api/sources/{id}` | Admin | Elimina una fuente | pendiente |
| GET | `/api/users` | Admin | Lista los usuarios con su rol | hecho |
| PUT | `/api/users/{id}/role` | Admin | Cambia el rol (`Admin` o `User`) | hecho |
| GET | `/api/settings/secrets` | Admin | Lista los secrets, siempre enmascarados | hecho |
| PUT | `/api/settings/secrets` | Admin | Crea un secret o reemplaza su valor | hecho |
| DELETE | `/api/settings/secrets/{key}` | Admin | Elimina un secret | hecho |
| GET | `/api/feed` | Público | Trae y parsea el contenido de las fuentes, sin guardar. Usa cache | pendiente |
| GET | `/api/items` | Público | Lista los items guardados | pendiente |
| POST | `/api/items` | Con sesión | Guarda un item elegido | pendiente |
| GET | `/api/items/{id}/export` | Con sesión | Descarga el item como archivo JSON (formato común). Quita claves de la URL y valida contra el schema | hecho |
| POST | `/api/items/import` | Con sesión | Sube un JSON (`multipart/form-data`, campo `file`), lo valida y lo guarda. Devuelve `ImportResultDto` con nuevas, reemplazadas y advertencias | hecho |

## Formato de error

Todas las respuestas de error tienen la misma forma, incluidos los errores de validación y los de login:

```json
{ "statusCode": 409, "title": "Conflicto", "message": "Ya existe una fuente con esa URL." }
```

| Código | Cuándo |
|---|---|
| 400 | Datos inválidos (el mensaje dice qué campo falla) |
| 401 | No hay sesión, el token venció o la contraseña es incorrecta |
| 403 | Hay sesión, pero el rol no permite la acción |
| 404 | No existe lo que se pidió |
| 409 | Ya existe (correo repetido, fuente con la misma URL) |
| 423 | Cuenta bloqueada 5 minutos tras 5 intentos fallidos de login |
| 500 | Error no previsto (el detalle queda solo en el log de la API) |

Para lanzar un error controlado desde un servicio: `throw ApiException.NotFound("...")`, `BadRequest(...)`, `Conflict(...)`, `Unauthorized(...)` o `Locked(...)`.

## Reglas

- La API nunca devuelve entidades de EF, solo DTOs (carpeta `Jason.Models/Dtos`).
- Los controllers solo llaman al servicio. La lógica va en `Services/`.
- Los nombres de clases, tablas y rutas van en inglés. Los textos de la interfaz, en español.
- Para proteger un endpoint: `[Authorize]` (cualquier usuario con sesión) o `[Authorize(Roles = Roles.Admin)]`. Los nombres de los roles están en `Jason.Models/Constants/Roles.cs`.
- Para usar un secret desde un servicio (por ejemplo al llamar a una fuente que pide API key): inyectar `ISecretService` y llamar a `GetValueAsync("NOMBRE")`. Devuelve el valor en claro solo dentro de la API; ningún endpoint lo expone.
