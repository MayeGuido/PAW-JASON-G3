using Jason.API.Exceptions;
using Jason.DataAccess.Identity;
using Jason.Models.Constants;
using Jason.Models.Dtos;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Jason.API.Services;

public interface IUserService
{
    Task<IEnumerable<UserDto>> GetAllAsync();
    Task<UserDto> ChangeRoleAsync(string userId, string role, string currentUserId);
}

public class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UserService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<IEnumerable<UserDto>> GetAllAsync()
    {
        var users = await _userManager.Users.OrderBy(u => u.Email).ToListAsync();

        var result = new List<UserDto>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            result.Add(AuthService.ToDto(user, roles.Contains(Roles.Admin) ? Roles.Admin : Roles.User));
        }
        return result;
    }

    public async Task<UserDto> ChangeRoleAsync(string userId, string role, string currentUserId)
    {
        if (!Roles.All.Contains(role))
            throw ApiException.BadRequest("El rol debe ser Admin o User.");

        var user = await _userManager.FindByIdAsync(userId)
            ?? throw ApiException.NotFound("No existe ese usuario.");

        var currentRoles = await _userManager.GetRolesAsync(user);
        var wasAdmin = currentRoles.Contains(Roles.Admin);

        if (wasAdmin && role != Roles.Admin)
        {
            if (user.Id == currentUserId)
                throw ApiException.BadRequest("No puedes quitarte el rol de Admin a ti mismo.");

            var admins = await _userManager.GetUsersInRoleAsync(Roles.Admin);
            if (admins.Count <= 1)
                throw ApiException.BadRequest("Debe quedar al menos un Admin.");
        }

        if (currentRoles.Count > 0)
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
        await _userManager.AddToRoleAsync(user, role);

        return AuthService.ToDto(user, role);
    }
}
