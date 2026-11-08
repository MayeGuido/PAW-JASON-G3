using System.Security.Claims;
using System.Text;
using Jason.API.Middleware;
using Jason.API.Options;
using Jason.API.Parsers;
using Jason.API.Security;
using Jason.API.Seed;
using Jason.API.Services;
using Jason.DataAccess;
using Jason.DataAccess.Context;
using Jason.DataAccess.Identity;
using Jason.Models.Constants;
using Jason.Models.Dtos;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Cada persona puede sobreescribir ajustes (cadena de conexión, claves) en appsettings.Local.json.
// Ese archivo está en .gitignore y no se sube nunca. Las variables de entorno y la línea de comandos
// se vuelven a agregar después para que sigan teniendo prioridad sobre él.
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);
builder.Configuration.AddEnvironmentVariables();
builder.Configuration.AddCommandLine(args);

builder.Services.Configure<AppOptions>(builder.Configuration.GetSection(AppOptions.SectionName));

// Los errores de validación de los DTOs también salen en el formato único de error
builder.Services.AddControllers().ConfigureApiBehaviorOptions(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var message = string.Join(" ", context.ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage));
        return new BadRequestObjectResult(new ApiErrorDto
        {
            StatusCode = StatusCodes.Status400BadRequest,
            Title = "Datos inválidos",
            Message = message
        });
    };
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Pega el token que devuelve /api/auth/login (sin la palabra Bearer)."
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient("Feed", client =>
{
    client.Timeout = TimeSpan.FromSeconds(20);
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Falta la cadena de conexión DefaultConnection en appsettings.");
builder.Services.AddDataAccess(connectionString);

// Usuarios y roles (ASP.NET Identity)
builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddErrorDescriber<SpanishIdentityErrorDescriber>();

// Login con token JWT
var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
if (string.IsNullOrWhiteSpace(jwt.Key) || jwt.Key.Length < 32)
{
    throw new InvalidOperationException(
        "Falta Jwt:Key (mínimo 32 caracteres). En desarrollo viene en appsettings.Development.json; " +
        "en otro ambiente ponla en appsettings.Local.json o en una variable de entorno.");
}
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));

static Task WriteAuthErrorAsync(HttpContext context, int status, string title, string message)
{
    context.Response.StatusCode = status;
    return context.Response.WriteAsJsonAsync(new ApiErrorDto { StatusCode = status, Title = title, Message = message });
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        options.Events = new JwtBearerEvents
        {
            // El rol que cuenta es el de la base de datos, no el que quedó escrito en el token. Así un cambio de rol
            // (o una cuenta eliminada) surte efecto de inmediato y no hasta que el token venza.
            OnTokenValidated = async context =>
            {
                var principal = context.Principal;
                var userId = principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();

                var user = userId is null ? null : await userManager.FindByIdAsync(userId);
                if (principal?.Identity is not ClaimsIdentity identity || user is null)
                {
                    context.Fail("El usuario ya no existe.");
                    return;
                }

                var currentRole = (await userManager.GetRolesAsync(user)).Contains(Roles.Admin) ? Roles.Admin : Roles.User;
                foreach (var claim in identity.FindAll(ClaimTypes.Role).ToList())
                    identity.RemoveClaim(claim);
                identity.AddClaim(new Claim(ClaimTypes.Role, currentRole));
            },

            // Las respuestas 401 y 403 también salen en el formato único de error
            OnChallenge = context =>
            {
                context.HandleResponse();
                return WriteAuthErrorAsync(context.HttpContext, StatusCodes.Status401Unauthorized,
                    "No autenticado", "Debes iniciar sesión para hacer esto.");
            },
            OnForbidden = context => WriteAuthErrorAsync(context.HttpContext, StatusCodes.Status403Forbidden,
                "Sin permiso", "Tu rol no tiene permiso para hacer esto.")
        };
    });
builder.Services.AddAuthorization();

// Cifrado de los secrets guardados en la base de datos
builder.Services.AddDataProtection().SetApplicationName("Jason");

builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ISecretService, SecretService>();
builder.Services.AddScoped<ISourceService, SourceService>();
builder.Services.AddScoped<IFeedService, FeedService>();

// Parsers (Strategy + Factory)
builder.Services.AddSingleton<IContentParser, JsonContentParser>();
builder.Services.AddSingleton<IContentParser, XmlContentParser>();
builder.Services.AddSingleton<IContentParser, HtmlContentParser>();
builder.Services.AddSingleton<IContentParserFactory, ContentParserFactory>();

var app = builder.Build();

await IdentitySeeder.SeedAsync(app.Services, app.Configuration, app.Logger);

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
