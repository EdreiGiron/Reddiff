using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RedDiff.Aplicacion.Abstracciones.Persistencia;
using RedDiff.Aplicacion.Abstracciones.Red;
using RedDiff.Aplicacion.Abstracciones.Seguridad;
using RedDiff.Infraestructura.Persistencia;
using RedDiff.Infraestructura.Persistencia.Repositorios;
using RedDiff.Infraestructura.Red.Netconf;
using RedDiff.Infraestructura.Red.Ssh;
using RedDiff.Infraestructura.Red.Syslog;
using RedDiff.Infraestructura.Seguridad;

namespace RedDiff.Infraestructura;

public static class ConfiguracionServicios
{
    public static IServiceCollection AgregarInfraestructura(
        this IServiceCollection servicios,
        IConfiguration configuracion,
        IHostEnvironment ambiente)
    {
        ArgumentNullException.ThrowIfNull(servicios);
        ArgumentNullException.ThrowIfNull(configuracion);
        ArgumentNullException.ThrowIfNull(ambiente);

        IDataProtectionBuilder proteccionDatos = servicios
            .AddDataProtection()
            .SetApplicationName("RedDiff");

        string? directorioClaves = configuracion["Seguridad:DirectorioClavesProteccion"];
        if (!string.IsNullOrWhiteSpace(directorioClaves))
        {
            proteccionDatos.PersistKeysToFileSystem(new DirectoryInfo(directorioClaves));
        }
        else if (!ambiente.IsDevelopment())
        {
            throw new InvalidOperationException(
                "Fuera de desarrollo debe configurar Seguridad:DirectorioClavesProteccion.");
        }

        servicios
            .AddOptions<OpcionesBaseDatos>()
            .Bind(configuracion.GetSection(OpcionesBaseDatos.NombreSeccion))
            .Validate(
                opciones => !string.IsNullOrWhiteSpace(opciones.Servidor),
                "Debe configurar BaseDatos:Servidor.")
            .Validate(
                opciones => opciones.Puerto is > 0 and <= 65535,
                "BaseDatos:Puerto debe estar entre 1 y 65535.")
            .Validate(
                opciones => opciones.VersionMayorServidor is >= 15 and <= 18,
                "BaseDatos:VersionMayorServidor debe estar entre 15 y 18.")
            .Validate(
                opciones => !string.IsNullOrWhiteSpace(opciones.Nombre),
                "Debe configurar BaseDatos:Nombre.")
            .Validate(
                opciones => !string.IsNullOrWhiteSpace(opciones.Usuario),
                "Debe configurar BaseDatos:Usuario.")
            .Validate(
                opciones => opciones.TieneContrasenaDisponible(),
                "Debe suministrar BaseDatos:Contrasena o BaseDatos:ArchivoContrasena mediante una fuente segura.")
            .Validate(
                opciones => opciones.TiempoEsperaSegundos is > 0 and <= 60,
                "BaseDatos:TiempoEsperaSegundos debe estar entre 1 y 60.")
            .Validate(
                opciones => opciones.TiempoComandoSegundos is > 0 and <= 300,
                "BaseDatos:TiempoComandoSegundos debe estar entre 1 y 300.")
            .Validate(
                opciones => ambiente.IsDevelopment()
                    || opciones.TieneTransporteSeguroParaProduccion(),
                "Fuera de desarrollo debe usar VerifyFull o habilitar explícitamente una conexión local sin TLS hacia localhost.")
            .Validate(
                opciones => opciones.ModoSsl != Npgsql.SslMode.VerifyFull
                    || (!string.IsNullOrWhiteSpace(opciones.CertificadoRaiz)
                        && File.Exists(opciones.CertificadoRaiz)),
                "Con BaseDatos:ModoSsl=VerifyFull debe configurar un BaseDatos:CertificadoRaiz existente.")
            .ValidateOnStart();

        servicios.AddDbContext<ContextoRedDiff>((proveedorServicios, opcionesContexto) =>
        {
            OpcionesBaseDatos opcionesBaseDatos = proveedorServicios
                .GetRequiredService<IOptions<OpcionesBaseDatos>>()
                .Value;

            opcionesContexto.UseNpgsql(
                opcionesBaseDatos.ConstruirCadenaConexion(),
                opcionesPostgresql =>
                {
                    opcionesPostgresql.SetPostgresVersion(
                        opcionesBaseDatos.VersionMayorServidor,
                        0);
                    opcionesPostgresql.MigrationsAssembly(
                        typeof(MarcadorEnsambladoInfraestructura).Assembly.GetName().Name!);
                    opcionesPostgresql.MigrationsHistoryTable(
                        "__historial_migraciones",
                        "reddiff");
                });

            if (ambiente.IsDevelopment())
            {
                opcionesContexto.EnableDetailedErrors();
            }
        });

        servicios.AddScoped<IUnidadDeTrabajo>(proveedorServicios =>
            proveedorServicios.GetRequiredService<ContextoRedDiff>());
        servicios.AddScoped<IRepositorioBaselines, RepositorioBaselines>();
        servicios.AddScoped<IRepositorioCapturas, RepositorioCapturas>();
        servicios.AddScoped<IRepositorioComparaciones, RepositorioComparaciones>();
        servicios.AddScoped<IRepositorioDispositivos, RepositorioDispositivos>();
        servicios.AddScoped<IRepositorioEventosCambio, RepositorioEventosCambio>();
        servicios.AddScoped<IRepositorioVerificaciones, RepositorioVerificaciones>();
        servicios.AddScoped<IRepositorioVersionesConfiguracion, RepositorioVersionesConfiguracion>();
        servicios.AddScoped<IRepositorioUsuarios, RepositorioUsuarios>();
        servicios.AddScoped<IRepositorioRoles, RepositorioRoles>();
        servicios.AddScoped<IRepositorioAuditorias, RepositorioAuditorias>();
        servicios.AddSingleton<IProtectorContrasena, ProtectorContrasenaPbkdf2>();
        servicios.AddSingleton<
            IProtectorSecretoDispositivo,
            ProtectorSecretoDispositivoDataProtection>();
        servicios.AddSingleton<IConectorCapturaRemota, ConectorCapturaSsh>();
        servicios.AddSingleton<IConectorCapturaRemota, ConectorCapturaNetconf>();

        servicios
            .AddOptions<OpcionesReceptorSyslog>()
            .Bind(configuracion.GetSection(OpcionesReceptorSyslog.NombreSeccion))
            .Validate(
                opciones => System.Net.IPAddress.TryParse(opciones.DireccionEscucha, out _),
                "Eventos:Syslog:DireccionEscucha debe ser una dirección IP válida.")
            .Validate(
                opciones => opciones.Puerto is > 0 and <= 65535,
                "Eventos:Syslog:Puerto debe estar entre 1 y 65535.")
            .ValidateOnStart();
        servicios.AddHostedService<ReceptorSyslogUdp>();

        return servicios;
    }
}
