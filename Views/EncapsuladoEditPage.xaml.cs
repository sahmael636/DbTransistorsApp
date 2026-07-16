// Views/EncapsuladoEditPage.xaml.cs
using DbTransistorsApp.ViewModels;

namespace DbTransistorsApp.Views;

public partial class EncapsuladoEditPage : ContentPage
{
    private readonly EncapsuladoEditViewModel _viewModel;

    public EncapsuladoEditPage(EncapsuladoEditViewModel viewModel)
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

        if (BindingContext is EncapsuladoEditViewModel vm)
        {
            // Obtener parámetros de navegación
            var parameters = Shell.Current.CurrentPage?.BindingContext as EncapsuladoEditViewModel;
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