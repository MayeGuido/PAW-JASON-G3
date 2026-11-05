namespace Jason.API.Options;

// Ajustes tipados (patrón Options). Se leen de la sección "App" de appsettings.json
public class AppOptions
{
    public const string SectionName = "App";

    // Minutos que se guarda en cache la respuesta de cada fuente, para no gastar la cuota de las APIs
    public int FeedCacheMinutes { get; set; } = 5;
}
