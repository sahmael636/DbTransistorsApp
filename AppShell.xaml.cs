using DbTransistorsApp.Views;

namespace DbTransistorsApp;

public partial class AppShell : Shell
{
    public AppShell(IServiceProvider serviceProvider)
    {
        InitializeComponent();

        Routing.RegisterRoute(nameof(TransistorListPage), typeof(TransistorListPage));
        Routing.RegisterRoute(nameof(TransistorDetailPage), typeof(TransistorDetailPage));
        Routing.RegisterRoute(nameof(SearchPage), typeof(SearchPage));
        Routing.RegisterRoute(nameof(EncapsuladosPage), typeof(EncapsuladosPage));
        Routing.RegisterRoute(nameof(EncapsuladoEditPage), typeof(EncapsuladoEditPage));
        Routing.RegisterRoute(nameof(EstructurasPage), typeof(EstructurasPage));
        Routing.RegisterRoute(nameof(TransistorEditPage), typeof(TransistorEditPage));

        // Shell selecciona automáticamente el primer ShellContent. No se fuerza
        // CurrentItem para evitar una conversión durante el arranque.
    }
}
