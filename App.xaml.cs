// App.xaml.cs
using DbTransistorsApp.Services;

namespace DbTransistorsApp;

public partial class App : Application
{
    private readonly IServiceProvider _serviceProvider;
    private readonly DatabaseService _databaseService;
    private bool _startupCompleted;

    public App(IServiceProvider serviceProvider, DatabaseService databaseService)
    {
        InitializeComponent();
        _serviceProvider = serviceProvider;
        _databaseService = databaseService;

        // Se muestra una página ligera inmediatamente. La copia y migración de la
        // base de datos se realizan después, sin bloquear la creación de la ventana.
        MainPage = CreateStartupPage();
    }

    protected override async void OnStart()
    {
        base.OnStart();

        if (_startupCompleted)
            return;

        _startupCompleted = true;

        try
        {
            // OnStart se ejecuta en el hilo de interfaz. Incluso operaciones con API
            // asíncrona pueden hacer trabajo síncrono interno en Android, por eso la
            // copia/migración completa se fuerza al ThreadPool después de permitir que
            // la pantalla inicial dibuje su primer fotograma.
            await Task.Delay(120);
            System.Diagnostics.Debug.WriteLine("Startup: iniciando SQLite en segundo plano.");
            await Task.Run(async () => await _databaseService.InitializeAsync().ConfigureAwait(false));
            System.Diagnostics.Debug.WriteLine("Startup: SQLite listo; creando AppShell.");

            // Resolver AppShell solo después de inicializar SQLite. Esta asignación
            // vuelve al hilo de interfaz automáticamente tras el await.
            MainPage = _serviceProvider.GetRequiredService<AppShell>();
            System.Diagnostics.Debug.WriteLine("Startup: AppShell visible.");

            // Los índices que falten se crean más tarde y nunca bloquean el arranque.
            _databaseService.StartBackgroundMaintenance();
        }
        catch (Exception ex)
        {
            await MainPage!.DisplayAlert(
                "Error de inicio",
                $"No se pudo inicializar la base de datos: {ex.Message}",
                "OK");
        }
    }

    private static Page CreateStartupPage()
    {
        var indicator = new ActivityIndicator
        {
            IsRunning = true,
            Color = Colors.White,
            WidthRequest = 48,
            HeightRequest = 48,
            HorizontalOptions = LayoutOptions.Center
        };

        var label = new Label
        {
            Text = "Preparando la base de datos...",
            TextColor = Colors.White,
            FontSize = 16,
            HorizontalTextAlignment = TextAlignment.Center
        };

        return new ContentPage
        {
            BackgroundColor = Color.FromArgb("#512BD4"),
            Content = new VerticalStackLayout
            {
                Spacing = 18,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                Children = { indicator, label }
            }
        };
    }
}
