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
            await _databaseService.InitializeAsync();

            // Resolver AppShell solo después de inicializar SQLite. Al resolver el
            // Shell también se crean MainPage/MainViewModel, que dependen de la BBDD.
            MainPage = _serviceProvider.GetRequiredService<AppShell>();
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
