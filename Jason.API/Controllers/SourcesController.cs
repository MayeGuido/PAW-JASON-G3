using Jason.API.Services;
using Jason.Models.Constants;
using Jason.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jason.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SourcesController : ControllerBase
{
    private readonly ISourceService _sourceService;

    public SourcesController(ISourceService sourceService)
    {
        _sourceService = sourceService;
    }

    // Público: cualquiera ve la lista de fuentes
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SourceDto>>> GetAll()
        => Ok(await _sourceService.GetAllAsync());

    // Solo el Admin agrega fuentes
    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<SourceDto>> Create(CreateSourceDto dto)
    {
        var created = await _sourceService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetAll), new { id = created.Id }, created);
    }

    // Solo el Admin elimina fuentes. SourceItems se borran en cascada por la relación de la base.
    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Delete(int id)
    {
        await _sourceService.DeleteAsync(id);
        return NoContent();
    }
}
