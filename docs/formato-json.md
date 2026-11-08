# Formato JSON común entre grupos

Fuente oficial: el documento "Formato JSON común v1.0" que preparó el grupo que coordina el formato. Este archivo resume cómo lo usamos nosotros. Mientras la versión no se congele, puede cambiar.

- `docs/formato-json/schema.json`: el schema de validación (copiado del Anexo A).
- `docs/formato-json/ejemplo.json`: el ejemplo completo del documento.
- Código: `Jason.Models/Exchange/` (clases del Anexo B, con los ajustes de abajo).

## Idea en una frase

Un archivo tiene fuentes (una por API), cada fuente tiene items, y cada item tiene unos pocos campos estándar (`id`, `title`, `mediaType`, `createdAt`) más la respuesta original de la API en `data`.

## Cómo encaja en las tablas del profesor

| Formato | Tabla |
|---|---|
| `sources[].name`, `url`, `description` | `Sources.Name`, `Url`, `Description` |
| `sources[].componentType`, `requiresSecret` (propuestos) | `Sources.ComponentType`, `RequiresSecret` |
| el item completo (`id`, `title`, `mediaType`, `createdAt`, `data`...) | `SourceItems.Json` |
| fecha en que se guardó | `SourceItems.CreatedAt` (no es lo mismo que `createdAt` del item) |

- `SourceItems` no tiene columna para el id del item. Para no duplicar al importar se busca con `JSON_VALUE([Json], '$.id')` dentro de la misma fuente (ver `SourceItemRepository.FindByExternalIdAsync`).
- La tabla `Sources` no guarda el formato de la respuesta (json, xml o html). Se deduce del `Content-Type` o del contenido (`ContentParserFactory.DetectFormat`).

## Ajustes que proponemos al grupo coordinador

Todos son compatibles: el formato ignora los campos que no conoce.

1. **Fechas en UTC con Z.** Una fecha que viene de SQL (`DATETIME`) sale sin la Z y el schema la rechaza. Resuelto en `UtcDateTimeConverter`.
2. **Sin fecha en la API:** usar la fecha de captura como `createdAt`.
3. **Campos opcionales en la fuente:** `description`, `componentType` (`widget`, `api` o `feed`), `requiresSecret` y `format` (`json`, `xml` o `html`). Ya están en `ExportSource`.
4. **Sin API keys en `url`** (por ejemplo `?api_key=...`), no solo en `data`.
5. **Categoría desconocida al importar:** guardarla como `otros` en vez de rechazar todo el archivo.
6. **`data` siempre objeto:** XML convertido a objeto, y HTML como `{ "html": "..." }`.

Para HTML, cada enlace útil se convierte en una ficha. `data` conserva el fragmento original del enlace y sus valores normalizados con la forma `{ "html": "<a ...>...</a>", "href": "...", "title": "..." }`. Esto evita guardar toda la página repetida dentro de cada ficha y mantiene la respuesta original relevante para esa ficha.

## Reglas que no se negocian

- Nunca incluir API keys, tokens ni contraseñas en el archivo.
- `data` es un objeto, nunca un texto con JSON escapado.
- UTF-8.
- Antes de compartir un archivo, validarlo contra `schema.json`.
