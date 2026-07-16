// App.xaml.cs
using DbTransistorsApp.Services;

namespace DbTransistorsApp;

public partial class App : Application
{
    private readonly IServiceProvider _serviceProvider;

    public App(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _serviceProvider = serviceProvider;

        // Establecer la página principal
        MainPage = _serviceProvider.GetRequiredService<AppShell>();
    }

    protected override async void OnStart()
    {
        base.OnStart();

        // Inicializar servicios si es necesario
        try
        {
            var databaseService = _serviceProvider.GetRequiredService<DatabaseService>();
            // Verificar conexión a la base de datos
            await Task.Run(() => databaseService.TestConnection());
        }
        catch (Exception ex)
        {
            await MainPage.DisplayAlert("Error", $"Error al conectar con la base de datos: {ex.Message}", "OK");
        }
    }

    protected override void OnSleep()
    {
        base.OnSleep();
        // Guardar estado si es necesario
    }

    protected override void OnResume()
    {
        base.OnResume();
        // Reanudar estado si es necesario
    }
}