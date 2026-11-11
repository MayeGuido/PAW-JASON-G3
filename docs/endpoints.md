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
| DELETE | `/api/sources/{id}` | Admin | Elimina una fuente | hecho |
| GET | `/api/users` | Admin | Lista los usuarios con su rol | hecho |
| PUT | `/api/users/{id}/role` | Admin | Cambia el rol (`Admin` o `User`) | hecho |
| GET | `/api/settings/secrets` | Admin | Lista los secrets, siempre enmascarados | hecho |
| PUT | `/api/settings/secrets` | Admin | Crea un secret o reemplaza su valor | hecho |
| DELETE | `/api/settings/secrets/{key}` | Admin | Elimina un secret | hecho |
| GET | `/api/feed` | Público | Trae y parsea el contenido de las fuentes, sin guardar. Usa cache | hecho |
| GET | `/api/items` | Público | Lista los items guardados | hecho |
| POST | `/api/items` | Con sesión | Guarda un item elegido | hecho |
| GET | `/api/items/{id}/export` | Con sesión | Descarga el item como archivo JSON (formato común). Quita claves de la URL y valida contra el schema | hecho |
| GET | `/api/items/export` | Con sesión | Descarga un solo archivo JSON con todas las fichas guardadas, agrupadas por fuente | hecho |
| POST | `/api/items/import` | Con sesión | Sube un JSON (`multipart/form-data`, campo `file`), lo valida y lo guarda. Devuelve `ImportResultDto` con nuevas, reemplazadas y advertencias | hecho |
| POST | `/api/items/import?preview=true` | Con sesión | Igual que el anterior, pero solo revisa el archivo y dice qué pasaría, sin guardar nada (`isPreview: true`) | hecho |

## Pruebas automáticas

El proyecto `Jason.Tests` prueba el servicio de exportar e importar con repositorios en memoria (no necesita base de datos). Se corre con `dotnet test`.

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

## Secrets usados por el feed

En Configuración se asocia el secret a su fuente. El feed admite estas convenciones en el nombre:

- `query:nombre`: envía el valor como parámetro de query. Ejemplo: `query:api_key` para NASA APOD.
- `header:nombre`: envía el valor en un encabezado. Ejemplo: `header:X-Api-Key`.
- Cualquier otro nombre asociado a una fuente se envía como `api_key` para mantener compatibilidad con nombres como `NASA_API_KEY`.

Si falta el secret o una fuente no responde, se registra el error y el resto del feed se devuelve normalmente. El valor del secret nunca se incluye en la respuesta del feed.
