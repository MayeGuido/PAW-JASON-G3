using System.Security.Claims;
using Jason.API.Services;
using Jason.Models.Constants;
using Jason.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jason.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = Roles.Admin)]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetAll()
        => Ok(await _userService.GetAllAsync());

    [HttpPut("{id}/role")]
    public async Task<ActionResult<UserDto>> ChangeRole(string id, ChangeRoleDto dto)
        => Ok(await _userService.ChangeRoleAsync(id, dto.Role, User.FindFirstValue(ClaimTypes.NameIdentifier)!));
}
