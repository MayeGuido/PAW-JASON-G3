using Microsoft.AspNetCore.Identity;

namespace Jason.DataAccess.Identity;

// Usuario de la aplicación. Extiende IdentityUser con el nombre que se muestra en la interfaz.
public class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }
}
