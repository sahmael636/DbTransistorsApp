// Views/TransistorListPage.xaml.cs
using DbTransistorsApp.ViewModels;

namespace DbTransistorsApp.Views;

public partial class TransistorListPage : ContentPage
{
    private readonly TransistorListViewModel _viewModel;

    public TransistorListPage(TransistorListViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    private void InitializeComponent()
    {
        throw new NotImplementedException();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Obtener parámetros de navegación
        if (BindingContext is TransistorListViewModel vm)
        {
            if (Shell.Current.CurrentPage?.BindingContext is TransistorListViewModel)
            {
                // El ViewModel ya debería estar inicializado
                await vm.OnAppearingAsync();
            }
        }
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        await _viewModel.OnDisappearingAsync();
    }
}