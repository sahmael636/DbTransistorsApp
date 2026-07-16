// Views/EstructurasPage.xaml.cs
using DbTransistorsApp.ViewModels;

namespace DbTransistorsApp.Views;

public partial class EstructurasPage : ContentPage
{
    private readonly EstructurasViewModel _viewModel;

    public EstructurasPage(EstructurasViewModel viewModel)
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
        await _viewModel.OnAppearingAsync();
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        await _viewModel.OnDisappearingAsync();
    }
}