using Jason.API.Services;
using Jason.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jason.API.Controllers;

[ApiController]
[Route("api/items")]
public class ItemsController : ControllerBase
{
    private readonly IItemService _itemService;

    public ItemsController(IItemService itemService)
    {
        _itemService = itemService;
    }

    // Público: cualquiera ve los items guardados
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SourceItemDto>>> GetAll()
        => Ok(await _itemService.GetAllAsync());

    // Con sesión: guarda un item elegido del feed
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<SourceItemDto>> Save(SaveItemDto dto)
    {
        var created = await _itemService.SaveAsync(dto);
        return CreatedAtAction(nameof(GetAll), new { id = created.Id }, created);
    }
}
