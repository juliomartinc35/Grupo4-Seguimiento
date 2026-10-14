using Microsoft.Extensions.Logging;
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

        // Módulo Gestión de Personas
        builder.Services.AddTransient<GestionPersonasViewModel>();
        builder.Services.AddTransient<GestionPersonasPage>();

        // Módulo Gestión de Clases y Cátedras
        builder.Services.AddTransient<GestionClasesViewModel>();
        builder.Services.AddTransient<GestionClasesPage>();

        // Módulo Estudiantes / Riesgo Académico
        builder.Services.AddTransient<EstudianteListViewModel>();
        builder.Services.AddTransient<EstudianteListPage>();

        // Modulo Umbrales de Riesgo (H7)
        builder.Services.AddTransient<ConfiguracionRiesgoViewModel>();
        builder.Services.AddTransient<ConfiguracionRiesgoPage>();

        return builder.Build();
    }
}