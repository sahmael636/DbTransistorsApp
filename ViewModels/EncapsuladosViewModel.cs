using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DbTransistorsApp.Models.Base;
using DbTransistorsApp.Services;
using DbTransistorsApp.ViewModels.Base;
using DbTransistorsApp.Views;
using System.Collections.ObjectModel;

namespace DbTransistorsApp.ViewModels;

public partial class EncapsuladosViewModel : BaseViewModel
{
    private readonly DatabaseService _databaseService;
    private readonly NavigationService _navigationService;
    private readonly DialogService _dialogService;
    private readonly ImageStorageService _imageStorageService;

    [ObservableProperty] private ObservableCollection<Encapsulado> _encapsulados = new();

    public EncapsuladosViewModel(
        DatabaseService databaseService,
        NavigationService navigationService,
        DialogService dialogService,
        ImageStorageService imageStorageService)
    {
        _databaseService = databaseService;
        _navigationService = navigationService;
        _dialogService = dialogService;
        _imageStorageService = imageStorageService;
        Title = "Encapsulados";
    }

    public override Task OnAppearingAsync() => LoadAsync();

    [RelayCommand]
    private Task Refresh() => LoadAsync();

    private async Task LoadAsync()
    {
        try
        {
            IsBusy = true;
            Encapsulados.Clear();
            foreach (var item in await _databaseService.GetAllEncapsuladosAsync())
            {
                item.ImagenPreview = _imageStorageService.GetImagePath(item.Imagen);
                Encapsulados.Add(item);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private Task NewEncapsulado()
        => _navigationService.NavigateToAsync(nameof(EncapsuladoEditPage), new Dictionary<string, object>
        {
            ["Mode"] = "New"
        });

    [RelayCommand]
    private Task EditEncapsulado(Encapsulado encapsulado)
        => _navigationService.NavigateToAsync(nameof(EncapsuladoEditPage), new Dictionary<string, object>
        {
            ["Mode"] = "Edit",
            ["Id"] = encapsulado.Id
        });

    [RelayCommand]
    private async Task DeleteEncapsulado(Encapsulado encapsulado)
    {
        bool confirmed = await _dialogService.ShowConfirmationAsync(
            "Eliminar encapsulado",
            $"¿Eliminar ID {encapsulado.Id}, '{encapsulado.Nombre}'? Sus asociaciones con transistores también se eliminarán.");
        if (!confirmed)
            return;

        try
        {
            await _databaseService.DeleteEncapsuladoAsync(encapsulado.Id);
            _imageStorageService.DeleteImage(encapsulado.Imagen);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowAlertAsync("No se pudo eliminar", ex.Message, "OK");
        }
    }
}
