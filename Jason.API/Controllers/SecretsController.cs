using Jason.API.Services;
using Jason.Models.Constants;
using Jason.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jason.API.Controllers;

// Los secrets (API keys de las fuentes) solo los administra el Admin y nunca se devuelven completos
[ApiController]
[Route("api/settings/secrets")]
[Authorize(Roles = Roles.Admin)]
public class SecretsController : ControllerBase
{
    private readonly ISecretService _secretService;

    public SecretsController(ISecretService secretService)
    {
        _secretService = secretService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SecretDto>>> GetAll()
        => Ok(await _secretService.GetAllAsync());

    // Crea el secret o, si ya existe uno con ese nombre, reemplaza su valor
    [HttpPut]
    public async Task<ActionResult<SecretDto>> Save(SaveSecretDto dto)
        => Ok(await _secretService.SaveAsync(dto));

    [HttpDelete("{key}")]
    public async Task<IActionResult> Delete(string key)
    {
        await _secretService.DeleteAsync(key);
        return NoContent();
    }
}
