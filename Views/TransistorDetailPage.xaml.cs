// Views/TransistorDetailPage.xaml.cs
using DbTransistorsApp.ViewModels;
using Microsoft.Maui.ApplicationModel;
using System.Diagnostics;

namespace DbTransistorsApp.Views;

public partial class TransistorDetailPage : ContentPage, IQueryAttributable
{
    private readonly TransistorDetailViewModel _viewModel;

    public TransistorDetailPage(TransistorDetailViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    // Recibir parámetros de navegación cuando se usa Shell GoToAsync con parámetros
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        try
        {
            if (BindingContext is TransistorDetailViewModel vm)
            {
                object typeObj = null;
                object idObj = null;

                if (!query.TryGetValue("type", out typeObj))
                    query.TryGetValue("Type", out typeObj);

                if (!query.TryGetValue("id", out idObj))
                    query.TryGetValue("Id", out idObj);

                if (typeObj != null && idObj != null && int.TryParse(idObj.ToString(), out int id))
                {
                    var type = typeObj.ToString();
                    Debug.WriteLine($"ApplyQueryAttributes: type={type}, id={id}");
                    // Ejecutar inicialización asíncrona en hilo de UI
                    _ = MainThread.InvokeOnMainThreadAsync(async () => await vm.InitializeAsync(type, id));
                }
            }
        }
        catch (Exception ex)
        {
            // evitar que errores de navegación rompan la experiencia; mostrar alerta si es necesario
            System.Diagnostics.Debug.WriteLine($"ApplyQueryAttributes error: {ex}");
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            // ✅ Obtener parámetros de navegación
            if (BindingContext is TransistorDetailViewModel vm)
            {
                // Verificar si el ViewModel ya tiene los datos
                if (string.IsNullOrEmpty(vm.TransistorName))
                {
                    // Intentar obtener parámetros desde Shell
                    if (Shell.Current.CurrentPage?.BindingContext is TransistorDetailViewModel context)
                    {
                        // Los parámetros ya deberían estar en el ViewModel
                        await vm.OnAppearingAsync();
                    }
                    else
                    {
                        // Obtener desde QueryProperty
                        var type = Shell.Current.CurrentPage?.BindingContext?.GetType();
                        // Si no hay datos, el ViewModel se inicializará con los parámetros
                    }
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al cargar: {ex.Message}", "OK");
        }
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        await _viewModel.OnDisappearingAsync();
    }
}