namespace Jason.API.Options;

// Ajustes del token JWT (sección "Jwt" de appsettings).
// Key es un secreto: en desarrollo viene en appsettings.Development.json; en cualquier otro ambiente
// debe ponerse en appsettings.Local.json o en una variable de entorno, nunca en Git.
public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "Jason.API";
    public string Audience { get; set; } = "Jason.Web";
    public string Key { get; set; } = string.Empty;
    public int ExpireMinutes { get; set; } = 120;
}
