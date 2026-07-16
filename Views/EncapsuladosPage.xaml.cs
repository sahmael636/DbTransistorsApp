// Views/EncapsuladosPage.xaml.cs
using DbTransistorsApp.ViewModels;

namespace DbTransistorsApp.Views;

public partial class EncapsuladosPage : ContentPage
{
    private readonly EncapsuladosViewModel _viewModel;

    public EncapsuladosPage(EncapsuladosViewModel viewModel)
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