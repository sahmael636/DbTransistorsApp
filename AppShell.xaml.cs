// AppShell.xaml.cs
using DbTransistorsApp.ViewModels;
using DbTransistorsApp.Views;

namespace DbTransistorsApp;

public partial class AppShell : Shell
{
    private readonly IServiceProvider _serviceProvider;

    public AppShell(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _serviceProvider = serviceProvider;

        // Registrar rutas con navegación con parámetros
        Routing.RegisterRoute(nameof(TransistorListPage), typeof(TransistorListPage));
        Routing.RegisterRoute(nameof(TransistorDetailPage), typeof(TransistorDetailPage));
        Routing.RegisterRoute(nameof(SearchPage), typeof(SearchPage));
        Routing.RegisterRoute(nameof(EncapsuladosPage), typeof(EncapsuladosPage));
        Routing.RegisterRoute(nameof(EncapsuladoEditPage), typeof(EncapsuladoEditPage));
        Routing.RegisterRoute(nameof(EstructurasPage), typeof(EstructurasPage));
        Routing.RegisterRoute(nameof(TransistorEditPage), typeof(TransistorEditPage));
        Routing.RegisterRoute(nameof(ImagePopup), typeof(ImagePopup));

        // Configurar la página principal
        CurrentItem = (ShellItem)Items.FirstOrDefault();
    }

    protected override async void OnNavigated(ShellNavigatedEventArgs args)
    {
        base.OnNavigated(args);

        // Manejar navegación con parámetros
        if (args.Current.Location.OriginalString.Contains("TransistorListPage"))
        {
            if (CurrentPage is TransistorListPage page && page.BindingContext is TransistorListViewModel vm)
            {
                var parameters = Shell.Current.CurrentPage?.BindingContext as TransistorListViewModel;
                if (parameters != null)
                {
                    // Los parámetros ya deberían estar en el ViewModel
                    await vm.OnAppearingAsync();
                }
            }
        }
        else if (args.Current.Location.OriginalString.Contains("TransistorDetailPage"))
        {
            if (CurrentPage is TransistorDetailPage page && page.BindingContext is TransistorDetailViewModel vm)
            {
                // Los parámetros ya deberían estar en el ViewModel
                await vm.OnAppearingAsync();
            }
        }
    }
}