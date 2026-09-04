using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.RateLimiting;
using RedDiff.Aplicacion.Seguridad;

namespace RedDiff.Api.Seguridad;

public static class ConfiguracionSeguridad
{
    public static IServiceCollection AgregarSeguridad(
        this IServiceCollection servicios,
        IConfiguration configuracion,
        IHostEnvironment ambiente)
    {
        ArgumentNullException.ThrowIfNull(servicios);
        ArgumentNullException.ThrowIfNull(configuracion);
        ArgumentNullException.ThrowIfNull(ambiente);

        OpcionesSeguridad valores = configuracion
            .GetSection(OpcionesSeguridad.NombreSeccion)
            .Get<OpcionesSeguridad>() ?? new OpcionesSeguridad();

        servicios
            .AddOptions<OpcionesSeguridad>()
            .Bind(configuracion.GetSection(OpcionesSeguridad.NombreSeccion))
            .Validate(
                opciones => opciones.DuracionSesionMinutos is >= 5 and <= 120,
                "Seguridad:DuracionSesionMinutos debe estar entre 5 y 120.")
            .Validate(
                opciones => EsOrigenValido(opciones.OrigenCliente),
                "Seguridad:OrigenCliente debe ser un origen HTTP o HTTPS sin ruta.")
            .ValidateOnStart();

        CookieSecurePolicy politicaSegura = ambiente.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;

        servicios.AddScoped<EventosCookieAutenticacion>();
        servicios
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(opciones =>
            {
                opciones.Cookie.Name = "RedDiff.Sesion";
                opciones.Cookie.HttpOnly = true;
                opciones.Cookie.IsEssential = true;
                opciones.Cookie.SameSite = SameSiteMode.Strict;
                opciones.Cookie.SecurePolicy = politicaSegura;
                opciones.ExpireTimeSpan = TimeSpan.FromMinutes(valores.DuracionSesionMinutos);
                opciones.SlidingExpiration = false;
                opciones.EventsType = typeof(EventosCookieAutenticacion);
            });

        servicios.AddAuthorization(opciones =>
        {
            opciones.AddPolicy(
                PoliticasSeguridad.Administrador,
                politica => politica.RequireRole(RolesSistema.Administrador));
        });

        servicios.AddAntiforgery(opciones =>
        {
            opciones.HeaderName = PoliticasSeguridad.EncabezadoAntifalsificacion;
            opciones.Cookie.Name = "RedDiff.Antifalsificacion";
            opciones.Cookie.HttpOnly = true;
            opciones.Cookie.IsEssential = true;
            opciones.Cookie.SameSite = SameSiteMode.Strict;
            opciones.Cookie.SecurePolicy = politicaSegura;
        });

        servicios.AddCors(opciones =>
        {
            opciones.AddPolicy(PoliticasSeguridad.CorsCliente, politica =>
            {
                if (!string.IsNullOrWhiteSpace(valores.OrigenCliente))
                {
                    politica
                        .WithOrigins(valores.OrigenCliente.TrimEnd('/'))
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                }
                else
                {
                    politica.SetIsOriginAllowed(_ => false);
                }
            });
        });

        servicios.AddRateLimiter(opciones =>
        {
            opciones.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            opciones.AddPolicy(
                PoliticasSeguridad.LimiteAutenticacion,
                contexto => RateLimitPartition.GetFixedWindowLimiter(
                    contexto.Connection.RemoteIpAddress?.ToString() ?? "desconocida",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        AutoReplenishment = true
                    }));
        });

        return servicios;
    }

    private static bool EsOrigenValido(string? origen)
    {
        if (string.IsNullOrWhiteSpace(origen))
        {
            return true;
        }

        return Uri.TryCreate(origen, UriKind.Absolute, out Uri? uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            && string.IsNullOrEmpty(uri.PathAndQuery.Trim('/'))
            && string.IsNullOrEmpty(uri.Fragment)
            && string.IsNullOrEmpty(uri.UserInfo);
    }
}
