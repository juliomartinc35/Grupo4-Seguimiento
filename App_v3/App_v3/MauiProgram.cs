using Microsoft.Extensions.Logging;
using App_v3.Seguimiento;
using App_v3.Services;
using App_v3.ViewModels;
using App_v3.Views;

namespace App_v3;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // Servicios
        builder.Services.AddSingleton<IDatabaseService>(_ =>
            new DatabaseService(Path.Combine(FileSystem.AppDataDirectory, "Asistencias.db3")));
        builder.Services.AddSingleton<IDialogService, ShellDialogService>();

        // Lógica del seguimiento (H1 a H13), definida en App_v3.Core
        builder.Services.AddSingleton<EstudianteService>();
        builder.Services.AddSingleton<CalendarioService>();
        builder.Services.AddSingleton<AsistenciaService>();
        builder.Services.AddSingleton<EntregaService>();
        builder.Services.AddSingleton<RiesgoService>();
        builder.Services.AddSingleton<ContactoService>();
        builder.Services.AddSingleton<FichaService>();

        // Pantallas del seguimiento
        builder.Services.AddTransient<PanelSeguimientoViewModel>();
        builder.Services.AddTransient<PanelPage>();
        builder.Services.AddTransient<EstudiantesViewModel>();
        builder.Services.AddTransient<EstudiantesPage>();
        builder.Services.AddTransient<AsistenciaViewModel>();
        builder.Services.AddTransient<AsistenciaPage>();
        builder.Services.AddTransient<EntregasViewModel>();
        builder.Services.AddTransient<EntregasPage>();
        builder.Services.AddTransient<CalendarioViewModel>();
        builder.Services.AddTransient<CalendarioPage>();
        builder.Services.AddTransient<ConfiguracionRiesgoViewModel>();
        builder.Services.AddTransient<ConfiguracionRiesgoPage>();
        builder.Services.AddTransient<FichaViewModel>(); // la ficha se arma desde Navegacion.AbrirFichaAsync

        // Módulos anteriores (se conservan registrados; ya no están en las pestañas)
        builder.Services.AddTransient<GestionPersonasViewModel>();
        builder.Services.AddTransient<GestionPersonasPage>();
        builder.Services.AddTransient<GestionClasesViewModel>();
        builder.Services.AddTransient<GestionClasesPage>();
        builder.Services.AddTransient<EstudianteListViewModel>();
        builder.Services.AddTransient<EstudianteListPage>();

        return builder.Build();
    }
}
