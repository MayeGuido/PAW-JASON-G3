using System.Text;
using Jason.API.Services;
using Jason.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jason.API.Controllers;

// Descargar e importar archivos JSON del formato común.
// Comparte la ruta api/items con el ItemsController (guardar y listar items).
[ApiController]
[Route("api/items")]
[Authorize]
public class ExchangeController : ControllerBase
{
    private readonly IExchangeService _exchangeService;

    public ExchangeController(IExchangeService exchangeService)
    {
        _exchangeService = exchangeService;
    }

    // Devuelve el item como archivo .json (UTF-8 sin BOM) listo para descargar
    [HttpGet("{id:int}/export")]
    public async Task<IActionResult> Export(int id)
    {
        var file = await _exchangeService.ExportItemAsync(id);
        return File(new UTF8Encoding(false).GetBytes(file.Json), "application/json; charset=utf-8", file.FileName);
    }

    // Recibe el archivo como multipart/form-data en el campo "file"
    [HttpPost("import")]
    public async Task<ActionResult<ImportResultDto>> Import(IFormFile? file)
        => Ok(await _exchangeService.ImportAsync(file));
}
