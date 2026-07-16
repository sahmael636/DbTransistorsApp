// MauiProgram.cs
using DbTransistorsApp.Services;
using DbTransistorsApp.ViewModels;
using DbTransistorsApp.Views;
using Microsoft.Extensions.Logging;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Maui;

namespace DbTransistorsApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // Registrar servicios
        builder.Services.AddSingleton<DatabaseService>();
        builder.Services.AddSingleton<NavigationService>();
        builder.Services.AddSingleton<DialogService>();
        builder.Services.AddSingleton<PdfExportService>();
        builder.Services.AddSingleton<ExcelExportService>();
        builder.Services.AddSingleton<ExcelImportService>();

        // Registrar ViewModels
        builder.Services.AddTransient<MainViewModel>();
        builder.Services.AddTransient<SearchViewModel>();
        builder.Services.AddTransient<TransistorListViewModel>();
        builder.Services.AddTransient<TransistorDetailViewModel>();
        builder.Services.AddTransient<EncapsuladosViewModel>();
        builder.Services.AddTransient<EncapsuladoEditViewModel>();
        builder.Services.AddTransient<EstructurasViewModel>();
        builder.Services.AddTransient<TransistorEditViewModel>();

        // Registrar páginas
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<SearchPage>();
        builder.Services.AddTransient<TransistorListPage>();
        builder.Services.AddTransient<TransistorDetailPage>();
        builder.Services.AddTransient<EncapsuladosPage>();
        builder.Services.AddTransient<EncapsuladoEditPage>();
        builder.Services.AddTransient<EstructurasPage>();
        builder.Services.AddTransient<TransistorEditPage>();
        builder.Services.AddTransient<ImagePopup>();
        builder.Services.AddTransient<AppShell>();

        var app = builder.Build();

        // Configurar el servicio de inyección de dependencias para CommunityToolkit
        Ioc.Default.ConfigureServices(app.Services);

        return app;
    }
}