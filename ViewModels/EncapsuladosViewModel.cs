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
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private List<Encapsulado> _allEncapsulados = new();

    [ObservableProperty]
    private ObservableCollection<Encapsulado> _encapsulados = new();

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private int _visibleCount;

    public string ResultsSummary => string.IsNullOrWhiteSpace(SearchText)
        ? $"{VisibleCount:N0} encapsulados"
        : $"{VisibleCount:N0} coincidencias";

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    partial void OnVisibleCountChanged(int value)
        => OnPropertyChanged(nameof(ResultsSummary));

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
        if (!await _loadLock.WaitAsync(0))
            return;

        try
        {
            IsBusy = true;
            var loaded = await _databaseService.GetAllEncapsuladosAsync();
            _allEncapsulados = loaded
                .GroupBy(x => x.Id)
                .Select(x => x.First())
                .OrderBy(x => x.Nombre, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            foreach (var item in _allEncapsulados)
                item.ImagenPreview = _imageStorageService.GetImagePath(item.Imagen);

            ApplyFilter();
        }
        finally
        {
            IsBusy = false;
            _loadLock.Release();
        }
    }

    private void ApplyFilter()
    {
        string term = SearchText?.Trim() ?? string.Empty;
        IEnumerable<Encapsulado> filtered = _allEncapsulados;

        if (term.Length > 0)
        {
            filtered = filtered.Where(x =>
                x.Nombre.Contains(term, StringComparison.CurrentCultureIgnoreCase)
                || x.Id.ToString().Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        Encapsulados = new ObservableCollection<Encapsulado>(filtered);
        VisibleCount = Encapsulados.Count;
        OnPropertyChanged(nameof(ResultsSummary));
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
