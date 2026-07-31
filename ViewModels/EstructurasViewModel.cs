using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DbTransistorsApp.Models.Base;
using DbTransistorsApp.Services;
using DbTransistorsApp.ViewModels.Base;
using System.Collections.ObjectModel;

namespace DbTransistorsApp.ViewModels;

public partial class EstructurasViewModel : BaseViewModel
{
    private readonly DatabaseService _databaseService;
    private readonly DialogService _dialogService;
    private readonly SemaphoreSlim _loadLock = new(1, 1);

    [ObservableProperty] private ObservableCollection<Estructura> _estructuras = new();

    public EstructurasViewModel(DatabaseService databaseService, DialogService dialogService)
    {
        _databaseService = databaseService;
        _dialogService = dialogService;
        Title = "Estructuras";
    }

    public override Task OnAppearingAsync() => LoadAsync();

    [RelayCommand]
    private Task Refresh() => LoadAsync();

    private async Task LoadAsync()
    {
        if (!await _loadLock.WaitAsync(0))
            return;

        try
        {
            IsBusy = true;
            var loaded = await _databaseService.GetAllEstructurasAsync();
            Estructuras = new ObservableCollection<Estructura>(
                loaded
                    .GroupBy(x => x.Id)
                    .Select(x => x.First())
                    .OrderBy(x => x.Id));
        }
        finally
        {
            IsBusy = false;
            _loadLock.Release();
        }
    }

    [RelayCommand]
    private async Task NewEstructura()
    {
        string? name = await _dialogService.ShowPromptAsync(
            "Nueva estructura",
            "Escriba el nombre de la estructura:",
            "Ej.: NPN");
        if (string.IsNullOrWhiteSpace(name))
            return;

        try
        {
            await _databaseService.InsertEstructuraAsync(new Estructura { Nombre = name });
            await LoadAsync();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowAlertAsync("No se pudo crear", ex.Message, "OK");
        }
    }

    [RelayCommand]
    private async Task EditEstructura(Estructura estructura)
    {
        string? name = await _dialogService.ShowPromptAsync(
            "Editar estructura",
            $"Nuevo nombre para ID {estructura.Id}:",
            estructura.Nombre);
        if (string.IsNullOrWhiteSpace(name))
            return;

        try
        {
            estructura.Nombre = name;
            await _databaseService.UpdateEstructuraAsync(estructura);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowAlertAsync("No se pudo actualizar", ex.Message, "OK");
        }
    }

    [RelayCommand]
    private async Task DeleteEstructura(Estructura estructura)
    {
        bool confirmed = await _dialogService.ShowConfirmationAsync(
            "Eliminar estructura",
            $"¿Eliminar ID {estructura.Id}, '{estructura.Nombre}'? Solo será posible si no está en uso.");
        if (!confirmed)
            return;

        try
        {
            await _databaseService.DeleteEstructuraAsync(estructura.Id);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowAlertAsync("No se pudo eliminar", ex.Message, "OK");
        }
    }
}
