// Views/TransistorDetailPage.xaml.cs
using DbTransistorsApp.ViewModels;

namespace DbTransistorsApp.Views;

public partial class TransistorDetailPage : ContentPage
{
    private readonly TransistorDetailViewModel _viewModel;

    public TransistorDetailPage(TransistorDetailViewModel viewModel)
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

        // Inicializar con parámetros de navegación
        if (BindingContext is TransistorDetailViewModel vm)
        {
            var parameters = Shell.Current.CurrentPage?.BindingContext as TransistorDetailViewModel;
            if (parameters != null)
            {
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