using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DbTransistorsApp.Models.Base;
using DbTransistorsApp.Services;
using DbTransistorsApp.ViewModels.Base;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Reflection;

namespace DbTransistorsApp.ViewModels;

public partial class TransistorEditViewModel : BaseViewModel
{
    private readonly DatabaseService _databaseService;
    private readonly NavigationService _navigationService;
    private readonly DialogService _dialogService;
    private readonly EncapsuladoSelectionService _encapsuladoSelectionService;
    private string _tableType = string.Empty;
    private int _id;
    private string _mode = "New";
    private Type _modelType = null!;
    private ITransistor _transistor = null!;
    private List<Encapsulado> _allEncapsulados = new();
    private HashSet<int> _selectedEncapsuladoIds = new();

    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _transistorType = string.Empty;
    [ObservableProperty] private ObservableCollection<ParameterField> _parameters = new();
    [ObservableProperty] private ObservableCollection<Estructura> _estructuras = new();
    [ObservableProperty] private Estructura? _selectedEstructura;
    [ObservableProperty] private int _selectedEncapsuladosCount;
    [ObservableProperty] private string _selectedEncapsuladosSummary = "Ninguno seleccionado";

    public string EncapsuladosButtonText => SelectedEncapsuladosCount == 0
        ? "Seleccionar encapsulados"
        : $"Cambiar selección ({SelectedEncapsuladosCount:N0})";

    partial void OnSelectedEncapsuladosCountChanged(int value)
        => OnPropertyChanged(nameof(EncapsuladosButtonText));

    public TransistorEditViewModel(
        DatabaseService databaseService,
        NavigationService navigationService,
        DialogService dialogService,
        EncapsuladoSelectionService encapsuladoSelectionService)
    {
        _databaseService = databaseService;
        _navigationService = navigationService;
        _dialogService = dialogService;
        _encapsuladoSelectionService = encapsuladoSelectionService;
    }

    public async Task InitializeAsync(string type, int id, string mode)
    {
        _tableType = TransistorMetadata.NormalizeTableName(type);
        _id = id;
        _mode = string.Equals(mode, "Edit", StringComparison.OrdinalIgnoreCase) ? "Edit" : "New";
        _modelType = TransistorMetadata.GetModelType(_tableType);
        TransistorType = TransistorMetadata.GetDisplayName(_tableType);
        Title = _mode == "New" ? $"Nuevo {TransistorType}" : $"Editar {TransistorType}";

        await LoadEstructurasAsync();
        await LoadEncapsuladosAsync();

        if (_mode == "New")
        {
            _transistor = (ITransistor)(Activator.CreateInstance(_modelType)
                ?? throw new InvalidOperationException("No se pudo crear el modelo."));
            Name = string.Empty;
            SelectedEstructura = Estructuras.FirstOrDefault();
            BuildParameters(null);
            _selectedEncapsuladoIds.Clear();
            UpdateEncapsuladosSummary();
            UpdateSelectionStyles();
            return;
        }

        _transistor = await _databaseService.GetTransistorByTypeAndIdAsync(_tableType, _id)
            ?? throw new InvalidOperationException("El transistor no existe.");
        Name = _transistor.Name;
        SelectedEstructura = Estructuras.FirstOrDefault(x => x.Id == _transistor.StructId)
            ?? Estructuras.FirstOrDefault();
        BuildParameters(_transistor);
        _selectedEncapsuladoIds = _transistor.CapsIds.ToHashSet();
        UpdateEncapsuladosSummary();
        UpdateSelectionStyles();
    }

    private async Task LoadEstructurasAsync()
    {
        var available = await _databaseService.GetAvailableStructuresForTableAsync(_tableType);
        if (available.Count == 0)
        {
            var allowedIds = await _databaseService.GetAllowedStructureIdsForTableAsync(_tableType);
            available = (await _databaseService.GetAllEstructurasAsync())
                .Where(x => allowedIds.Contains(x.Id))
                .ToList();
        }

        Estructuras.Clear();
        foreach (var item in available)
            Estructuras.Add(item);
    }

    private async Task LoadEncapsuladosAsync()
    {
        _allEncapsulados = await _databaseService.GetAllEncapsuladosAsync();
        UpdateEncapsuladosSummary();
    }

    private void BuildParameters(object? source)
    {
        Parameters.Clear();
        foreach (PropertyInfo property in TransistorMetadata.GetEditableProperties(_tableType))
        {
            object? value = source == null ? null : property.GetValue(source);
            Type underlying = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            Parameters.Add(new ParameterField
            {
                Name = property.Name,
                DisplayName = TransistorMetadata.GetDisplayNameForProperty(property.Name),
                Unit = TransistorMetadata.GetUnitForProperty(property.Name),
                IsNumeric = underlying != typeof(string),
                Value = value == null ? string.Empty : Convert.ToString(value, CultureInfo.CurrentCulture) ?? string.Empty
            });
        }
    }

    partial void OnSelectedEstructuraChanged(Estructura? value) => UpdateSelectionStyles();

    [RelayCommand]
    private void SelectEstructura(Estructura estructura)
    {
        SelectedEstructura = estructura;
    }

    [RelayCommand]
    private async Task SelectEncapsulados()
    {
        IReadOnlyList<int>? selected = await _encapsuladoSelectionService.SelectAsync(
            _allEncapsulados,
            _selectedEncapsuladoIds);

        if (selected == null)
            return;

        _selectedEncapsuladoIds = selected.ToHashSet();
        UpdateEncapsuladosSummary();
    }

    private void UpdateEncapsuladosSummary()
    {
        var selected = _allEncapsulados
            .Where(x => _selectedEncapsuladoIds.Contains(x.Id))
            .OrderBy(x => x.Nombre, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        SelectedEncapsuladosCount = selected.Count;
        if (selected.Count == 0)
        {
            SelectedEncapsuladosSummary = "Ninguno seleccionado";
            return;
        }

        string names = string.Join(", ", selected.Take(4).Select(x => x.Nombre));
        int remaining = selected.Count - 4;
        SelectedEncapsuladosSummary = remaining > 0
            ? $"{names} y {remaining:N0} más"
            : names;
    }

    private void UpdateSelectionStyles()
    {
        foreach (var item in Estructuras)
        {
            item.IsSelected = item.Id == SelectedEstructura?.Id;
        }

    }

    [RelayCommand]
    private async Task Save()
    {
        string normalizedName = Name.Trim();
        if (normalizedName.Length == 0)
        {
            await _dialogService.ShowAlertAsync("Datos requeridos", "El nombre es obligatorio.", "OK");
            return;
        }

        if (SelectedEstructura == null)
        {
            await _dialogService.ShowAlertAsync("Datos requeridos", "Seleccione una estructura.", "OK");
            return;
        }

        bool nameChanged = _mode == "New" ||
            !string.Equals(normalizedName, _transistor.Name?.Trim(), StringComparison.OrdinalIgnoreCase);
        if (nameChanged && await _databaseService.TransistorNameExistsAsync(
                normalizedName,
                _mode == "Edit" ? _tableType : null,
                _mode == "Edit" ? _id : 0))
        {
            await _dialogService.ShowAlertAsync(
                "Nombre duplicado",
                "Ya existe un transistor con ese nombre, sin importar mayúsculas o minúsculas.",
                "OK");
            return;
        }

        try
        {
            IsBusy = true;
            _transistor.Name = normalizedName;
            _transistor.StructId = SelectedEstructura.Id;
            _transistor.CapsIds = _selectedEncapsuladoIds.OrderBy(x => x).ToList();

            foreach (var parameter in Parameters)
            {
                PropertyInfo? property = _modelType.GetProperty(parameter.Name);
                if (property == null || !property.CanWrite)
                    continue;

                Type underlying = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                if (underlying == typeof(string))
                {
                    property.SetValue(_transistor,
                        string.IsNullOrWhiteSpace(parameter.Value) ? null : parameter.Value.Trim());
                    continue;
                }

                if (string.IsNullOrWhiteSpace(parameter.Value))
                {
                    property.SetValue(_transistor, null);
                    continue;
                }

                if (!TryParseDouble(parameter.Value, out double number))
                {
                    await _dialogService.ShowAlertAsync(
                        "Valor inválido",
                        $"El campo {parameter.DisplayName} debe contener un número válido.",
                        "OK");
                    return;
                }
                property.SetValue(_transistor, number);
            }

            if (_mode == "New")
                await _databaseService.InsertTransistorAsync(_tableType, _transistor);
            else
                await _databaseService.UpdateTransistorAsync(_tableType, _transistor);

            await _dialogService.ShowToastAsync(
                _mode == "New" ? "Transistor creado." : "Transistor actualizado.");
            await _navigationService.NavigateBackAsync();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowAlertAsync("Error", $"No se pudo guardar: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private Task Cancel() => _navigationService.NavigateBackAsync();

    private static bool TryParseDouble(string value, out double result)
        => double.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out result)
           || double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
}

public partial class ParameterField : ObservableObject
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public bool IsNumeric { get; set; }
    public Keyboard InputKeyboard => IsNumeric ? Keyboard.Numeric : Keyboard.Text;
    [ObservableProperty] private string _value = string.Empty;
}
