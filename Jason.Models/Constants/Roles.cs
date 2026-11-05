namespace Jason.Models.Constants;

// Roles de la aplicación. Se usan en [Authorize(Roles = ...)] y en la Web.
public static class Roles
{
    public const string Admin = "Admin";
    public const string User = "User";

    public static readonly string[] All = { Admin, User };
}
