using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DbTransistorsApp.Models.Base;
using DbTransistorsApp.Services;
using DbTransistorsApp.ViewModels.Base;
using DbTransistorsApp.Views;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Reflection;

namespace DbTransistorsApp.ViewModels;

public partial class TransistorDetailViewModel : BaseViewModel
{
    private readonly DatabaseService _databaseService;
    private readonly NavigationService _navigationService;
    private readonly DialogService _dialogService;
    private readonly PdfExportService _pdfExportService;
    private readonly ExcelExportService _excelExportService;
    private readonly ImageStorageService _imageStorageService;

    private ITransistor _originalTransistor = null!;
    private ITransistor _currentTransistor = null!;
    private string _tableType = string.Empty;
    private int _id;
    private Type _modelType = null!;
    private IReadOnlyList<PropertyInfo> _displayProperties = Array.Empty<PropertyInfo>();
    private bool _initialized;
    private bool _reloadOnNextAppearance;

    [ObservableProperty] private string _transistorName = string.Empty;
    [ObservableProperty] private string _transistorType = string.Empty;
    [ObservableProperty] private string _transistorStructure = string.Empty;
    [ObservableProperty] private ObservableCollection<Encapsulado> _encapsulados = new();
    [ObservableProperty] private ObservableCollection<TransistorParameter> _parameters = new();
    [ObservableProperty] private ObservableCollection<ReplacementRow> _replacements = new();
    [ObservableProperty] private int _replacementCount;
    [ObservableProperty] private double _columnWidth = 90;
    [ObservableProperty] private double _replacementsTableWidth = 600;
    [ObservableProperty] private bool _areParametersExpanded;

    public ObservableCollection<string> ReplacementHeaders { get; } = new();
    public Dictionary<string, string> OriginalParameters { get; } = new();
    public string ParametersToggleText => AreParametersExpanded
        ? "▼ Parámetros del transistor"
        : "▶ Parámetros del transistor";

    partial void OnAreParametersExpandedChanged(bool value)
        => OnPropertyChanged(nameof(ParametersToggleText));

    public TransistorDetailViewModel(
        DatabaseService databaseService,
        NavigationService navigationService,
        DialogService dialogService,
        PdfExportService pdfExportService,
        ExcelExportService excelExportService,
        ImageStorageService imageStorageService)
    {
        _databaseService = databaseService;
        _navigationService = navigationService;
        _dialogService = dialogService;
        _pdfExportService = pdfExportService;
        _excelExportService = excelExportService;
        _imageStorageService = imageStorageService;
        AreParametersExpanded = DeviceInfo.Idiom != DeviceIdiom.Phone;
    }

    public async Task InitializeAsync(string type, int id)
    {
        _tableType = TransistorMetadata.NormalizeTableName(type);
        _id = id;
        _modelType = TransistorMetadata.GetModelType(_tableType);
        _displayProperties = TransistorMetadata.GetDisplayProperties(_tableType);
        ConfigureReplacementHeaders();
        await LoadTransistorDataAsync();
        _initialized = true;
        _reloadOnNextAppearance = false;
    }

    public override async Task OnAppearingAsync()
    {
        if (_initialized && _reloadOnNextAppearance)
        {
            _reloadOnNextAppearance = false;
            await LoadTransistorDataAsync();
        }
    }

    private async Task LoadTransistorDataAsync()
    {
        try
        {
            IsBusy = true;
            _originalTransistor = await _databaseService.GetTransistorByTypeAndIdAsync(_tableType, _id)
                ?? throw new InvalidOperationException("El transistor ya no existe.");
            _currentTransistor = CloneTransistor(_originalTransistor);

            TransistorName = _originalTransistor.Name;
            TransistorType = TransistorMetadata.GetDisplayName(_tableType);
            var structure = await _databaseService.GetEstructuraByIdAsync(_originalTransistor.StructId);
            TransistorStructure = structure?.Nombre ?? "Desconocida";

            Encapsulados.Clear();
            foreach (var cap in await _databaseService.GetEncapsuladosByTransistorIdAsync(_tableType, _id))
            {
                cap.ImagenPreview = _imageStorageService.GetImagePath(cap.Imagen);
                Encapsulados.Add(cap);
            }

            ConfigureParameters();
            await LoadReplacementsAsync();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowAlertAsync("Error", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ConfigureReplacementHeaders()
    {
        ReplacementHeaders.Clear();
        foreach (var property in _displayProperties)
            ReplacementHeaders.Add(TransistorMetadata.GetDisplayNameForProperty(property.Name));

        ColumnWidth = DeviceInfo.Idiom == DeviceIdiom.Phone ? 90 : 110;
        ReplacementsTableWidth = 150 + ReplacementHeaders.Count * ColumnWidth;
        OnPropertyChanged(nameof(ReplacementHeaders));
    }

    private void ConfigureParameters()
    {
        Parameters.Clear();
        OriginalParameters.Clear();
        foreach (var property in _displayProperties)
        {
            object? value = property.GetValue(_currentTransistor);
            string text = value == null ? string.Empty : Convert.ToString(value, CultureInfo.CurrentCulture) ?? string.Empty;
            Parameters.Add(new TransistorParameter(
                property.Name,
                TransistorMetadata.GetDisplayNameForProperty(property.Name),
                text,
                TransistorMetadata.GetUnitForProperty(property.Name),
                text));
            OriginalParameters[property.Name] = text;
        }
    }

    private static ITransistor CloneTransistor(ITransistor source)
    {
        var clone = (ITransistor)(Activator.CreateInstance(source.GetType())
            ?? throw new InvalidOperationException("No se pudo copiar el transistor."));
        foreach (var property in source.GetType().GetProperties().Where(x => x.CanRead && x.CanWrite))
        {
            object? value = property.GetValue(source);
            if (value is List<int> ids)
                value = new List<int>(ids);
            property.SetValue(clone, value);
        }
        return clone;
    }

    [RelayCommand]
    private void ToggleParameters() => AreParametersExpanded = !AreParametersExpanded;

    [RelayCommand]
    private async Task ParameterChanged(string parameterName)
    {
        var parameter = Parameters.FirstOrDefault(x => x.Name == parameterName);
        PropertyInfo? property = _modelType.GetProperty(parameterName);
        if (parameter == null || property == null)
            return;

        if (string.IsNullOrWhiteSpace(parameter.Value))
        {
            property.SetValue(_currentTransistor, null);
        }
        else if (TryParseDouble(parameter.Value, out double value))
        {
            property.SetValue(_currentTransistor, value);
        }
        else
        {
            await _dialogService.ShowAlertAsync(
                "Valor inválido",
                $"{parameter.DisplayName} no contiene un número válido.",
                "OK");
            return;
        }

        await LoadReplacementsAsync();
    }

    [RelayCommand]
    private async Task ResetToDefault()
    {
        _currentTransistor = CloneTransistor(_originalTransistor);
        ConfigureParameters();
        await LoadReplacementsAsync();
        await _dialogService.ShowToastAsync("Parámetros restablecidos.");
    }

    private async Task LoadReplacementsAsync()
    {
        var criteria = new Dictionary<string, object>();
        OriginalParameters.Clear();
        foreach (PropertyInfo property in _displayProperties)
        {
            object? value = property.GetValue(_currentTransistor);
            string text = value == null ? string.Empty : Convert.ToString(value, CultureInfo.CurrentCulture) ?? string.Empty;
            OriginalParameters[property.Name] = text;
            if (value is double number && number > 0)
                criteria[property.Name] = number;
        }
        criteria["_id"] = _id;

        var matches = await _databaseService.GetReplacementsAsync(
            _tableType,
            criteria,
            _currentTransistor.StructId,
            _currentTransistor.CapsIds);

        Replacements.Clear();
        foreach (object item in matches)
        {
            var row = new ReplacementRow
            {
                Id = Convert.ToInt32(item.GetType().GetProperty("Id")?.GetValue(item) ?? 0),
                Name = item.GetType().GetProperty("Name")?.GetValue(item)?.ToString() ?? string.Empty,
                Original = item
            };
            foreach (PropertyInfo property in _displayProperties)
                row.Values.Add(FormatValue(property.GetValue(item)));
            Replacements.Add(row);
        }
        ReplacementCount = Replacements.Count;
    }

    [RelayCommand]
    private async Task ShowEncapsuladoImage(Encapsulado encapsulado)
    {
        string? path = _imageStorageService.GetImagePath(encapsulado.Imagen);
        if (path == null)
        {
            await _dialogService.ShowAlertAsync(
                "Imagen no disponible",
                $"El encapsulado {encapsulado.Nombre} no tiene una imagen accesible.",
                "OK");
            return;
        }
        await _dialogService.ShowImagePopupAsync(path, encapsulado.Nombre);
    }

    [RelayCommand]
    private Task SelectReplacement(ReplacementRow replacement)
        => _navigationService.NavigateToAsync(nameof(TransistorDetailPage), new Dictionary<string, object>
        {
            ["Type"] = _tableType,
            ["Id"] = replacement.Id
        });

    [RelayCommand]
    private Task Edit()
    {
        _reloadOnNextAppearance = true;
        return _navigationService.NavigateToAsync(nameof(TransistorEditPage), new Dictionary<string, object>
        {
            ["Type"] = _tableType,
            ["Id"] = _id,
            ["Mode"] = "Edit"
        });
    }

    [RelayCommand]
    private async Task Delete()
    {
        bool confirmed = await _dialogService.ShowConfirmationAsync(
            "Eliminar transistor",
            $"¿Desea eliminar '{TransistorName}'? Esta acción no se puede deshacer.");
        if (!confirmed)
            return;

        try
        {
            IsBusy = true;
            await _databaseService.DeleteTransistorAsync(_tableType, _id);
            await _dialogService.ShowToastAsync("Transistor eliminado.");
            await _navigationService.NavigateBackAsync();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowAlertAsync("Error", $"No se pudo eliminar: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ExportToPdf()
    {
        if (Replacements.Count == 0)
        {
            await _dialogService.ShowAlertAsync("Sin datos", "No hay reemplazos para exportar.", "OK");
            return;
        }

        try
        {
            IsBusy = true;
            string path = Path.Combine(
                FileSystem.CacheDirectory,
                $"Reemplazos_{SanitizeFileName(TransistorName)}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            var rows = Replacements.Select(x => new PdfReplacementRow(
                x.Name,
                x.Values.Take(ReplacementHeaders.Count).ToList())).ToList();

            await _pdfExportService.ExportReplacementsToPdfAsync(
                path,
                TransistorName,
                TransistorType,
                TransistorStructure,
                OriginalParameters,
                ReplacementHeaders.ToList(),
                rows);

            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Compartir PDF de reemplazos",
                File = new ShareFile(path)
            });
        }
        catch (Exception ex)
        {
            await _dialogService.ShowAlertAsync("Error", $"No se pudo generar el PDF: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ExportToExcel()
    {
        if (Replacements.Count == 0)
        {
            await _dialogService.ShowAlertAsync("Sin datos", "No hay reemplazos para exportar.", "OK");
            return;
        }

        try
        {
            IsBusy = true;
            using MemoryStream workbook = await _excelExportService.CreateReplacementsWorkbookAsync(
                TransistorName,
                TransistorType,
                TransistorStructure,
                OriginalParameters,
                ReplacementHeaders.ToList(),
                Replacements.ToList());
            string path = Path.Combine(
                FileSystem.CacheDirectory,
                $"Reemplazos_{SanitizeFileName(TransistorName)}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
            await using (var output = File.Create(path))
                await workbook.CopyToAsync(output);

            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Compartir Excel de reemplazos",
                File = new ShareFile(path)
            });
        }
        catch (Exception ex)
        {
            await _dialogService.ShowAlertAsync("Error", $"No se pudo generar el Excel: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static string FormatValue(object? value)
        => value == null ? string.Empty : Convert.ToString(value, CultureInfo.CurrentCulture) ?? string.Empty;

    private static bool TryParseDouble(string value, out double result)
        => double.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out result)
           || double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result);

    private static string SanitizeFileName(string value)
    {
        string result = string.Concat(value.Where(x => !Path.GetInvalidFileNameChars().Contains(x)));
        return string.IsNullOrWhiteSpace(result) ? "Transistor" : result;
    }
}
