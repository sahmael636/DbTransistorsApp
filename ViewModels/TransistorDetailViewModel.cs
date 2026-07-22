// ViewModels/TransistorDetailViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DbTransistorsApp.Models.Base;
using DbTransistorsApp.Services;
using DbTransistorsApp.ViewModels.Base;
using DbTransistorsApp.Views;
//using IntelliJ.Lang.Annotations;
using System.Collections.ObjectModel;
using System.Reflection;

namespace DbTransistorsApp.ViewModels
{
    public partial class TransistorDetailViewModel : BaseViewModel
    {
        private readonly DatabaseService _databaseService;
        private readonly NavigationService _navigationService;
        private readonly DialogService _dialogService;
        private ITransistor _originalTransistor;
        private ITransistor _currentTransistor;
        private string _tableType;
        private int _id;
        private Type _modelType;

        [ObservableProperty]
        private string _idString;

        public int Id
        {
            get => _id;
            set
            {
                _id = value;
                // ✅ Cargar datos cuando se recibe el ID
                if (_id > 0 && !string.IsNullOrEmpty(Type))
                {
                    Task.Run(async () => await LoadTransistorData());
                }
            }
        }

        public string Type
        {
            get => _tableType;
            set
            {
                _tableType = value;
                // ✅ Cargar datos cuando se recibe el tipo
                if (_id > 0 && !string.IsNullOrEmpty(_tableType))
                {
                    Task.Run(async () => await LoadTransistorData());
                }
            }
        }

        [ObservableProperty]
        private string _transistorName;

        [ObservableProperty]
        private string _transistorType;

        [ObservableProperty]
        private string _transistorStructure;

        [ObservableProperty]
        private Encapsulado _encapsulado;

        public string EncapsuladoNombre => _encapsulado?.Nombre ?? "Desconocido";

        // Called by the source generator when Encapsulado changes
        partial void OnEncapsuladoChanged(Encapsulado value)
        {
            OnPropertyChanged(nameof(EncapsuladoNombre));
        }

        [ObservableProperty]
        private bool _hasEncapsuladoImage;

        [ObservableProperty]
        private ObservableCollection<TransistorParameter> _parameters = new();

        [ObservableProperty]
        private ObservableCollection<object> _replacements = new();

        // Encabezados dinámicos para la tabla de reemplazos
        public ObservableCollection<string> ReplacementHeaders { get; } = new();

        [ObservableProperty]
        private double _columnWidth;

        public string ReplacementHeaderLine => string.Join(" | ", ReplacementHeaders);

        [ObservableProperty]
        private int _replacementCount;

        public TransistorDetailViewModel(DatabaseService databaseService, NavigationService navigationService, DialogService dialogService)
        {
            _databaseService = databaseService;
            _navigationService = navigationService;
            _dialogService = dialogService;
            ColumnWidth = 80;
        }

        // ✅ Método para cargar datos
        private async Task LoadTransistorData()
        {
            try
            {
                IsBusy = true;

                // Obtener el transistor
                _originalTransistor = await _databaseService.GetTransistorByTypeAndIdAsync(_tableType, _id);
                if (_originalTransistor == null)
                {
                    await _dialogService.ShowAlertAsync("Error", "Transistor no encontrado", "OK");
                    await _navigationService.NavigateBackAsync();
                    return;
                }

                TransistorName = _originalTransistor.Name;
            System.Diagnostics.Debug.WriteLine($"Transistor loaded: Name={TransistorName}");

                // Crear una copia editable
                _currentTransistor = CloneTransistor(_originalTransistor);

                // Obtener la estructura
                var estructura = await _databaseService.GetEstructuraByIdAsync(_originalTransistor.StructId);
                TransistorStructure = estructura?.Nombre ?? "Desconocida";

                // Obtener tipo de transistor
                TransistorType = GetTransistorDisplayType(_tableType);

                // Obtener encapsulado
                var caps = await _databaseService.GetEncapsuladosByTransistorIdAsync(_tableType, _id);
                Encapsulado = caps.FirstOrDefault();
                HasEncapsuladoImage = Encapsulado != null && !string.IsNullOrEmpty(Encapsulado.Imagen);

                // Configurar parámetros
                ConfigureParameters(_currentTransistor);
            System.Diagnostics.Debug.WriteLine($"Parameters count after configure: {Parameters?.Count}");

                // Cargar reemplazos
                await LoadReplacementsAsync();
            System.Diagnostics.Debug.WriteLine($"ReplacementCount after load: {ReplacementCount}");
            }
            catch (Exception ex)
            {
                await _dialogService.ShowAlertAsync("Error", $"Error al cargar: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task InitializeAsync(string type, int id)
        {
            System.Diagnostics.Debug.WriteLine($"InitializeAsync start: type={type}, id={id}");
            var normalizedType = type?.ToLowerInvariant();
            _tableType = normalizedType;
            _id = id;
            _modelType = GetModelType(normalizedType);

            // Cargar el transistor
            _originalTransistor = await _databaseService.GetTransistorByTypeAndIdAsync(type, id);
            if (_originalTransistor == null)
            {
                await _dialogService.ShowAlertAsync("Error", "Transistor no encontrado", "OK");
                await _navigationService.NavigateBackAsync();
                return;
            }

            TransistorName = _originalTransistor.Name;

            // Crear una copia editable
            _currentTransistor = CloneTransistor(_originalTransistor);

            // Obtener la estructura
            var estructura = await _databaseService.GetEstructuraByIdAsync(_originalTransistor.StructId);
            TransistorStructure = estructura?.Nombre ?? "Desconocida";

            // Obtener tipo de transistor
            TransistorType = GetTransistorDisplayType(type);

            // Obtener encapsulado
            var caps = await _databaseService.GetEncapsuladosByTransistorIdAsync(type, id);
            Encapsulado = caps.FirstOrDefault();
            HasEncapsuladoImage = Encapsulado != null && !string.IsNullOrEmpty(Encapsulado.Imagen);

            // Configurar parámetros
            ConfigureParameters(_currentTransistor);

            // Cargar reemplazos
            await LoadReplacementsAsync();
        }

        private Type GetModelType(string type)
        {
            var t = type?.ToLowerInvariant();
            return t switch
            {
                "bjtge" => typeof(BjtGe),
                "bjtsi" => typeof(BjtSi),
                "bjtprebias" => typeof(BjtPrebias),
                "bjtprebiasdual" => typeof(BjtPrebiasDual),
                "bjtsidual" => typeof(BjtSiDual),
                "jfet" => typeof(Jfet),
                "mosfet" => typeof(Mosfet),
                "mosfetdual" => typeof(MosfetDual),
                "igbt" => typeof(Igbt),
                "igbtdual" => typeof(IgbtDual),
                _ => throw new ArgumentException("Tipo de tabla no válido")
            };
        }

        private ITransistor CloneTransistor(ITransistor original)
        {
            var type = original.GetType();
            var clone = (ITransistor)Activator.CreateInstance(type);

            foreach (var prop in type.GetProperties())
            {
                if (prop.CanWrite && prop.Name != "CapsIds")
                {
                    prop.SetValue(clone, prop.GetValue(original));
                }
            }

            // Clonar CapsIds
            if (original.CapsIds != null)
            {
                clone.CapsIds = new List<int>(original.CapsIds);
            }

            return clone;
        }

        private string GetTransistorDisplayType(string type)
        {
            return type switch
            {
                "bjtge" => "Transistor Bipolar de Germanio",
                "bjtsi" => "Transistor Bipolar de Silicio",
                "bjtprebias" => "Transistor Bipolar Pre-polarizado",
                "bjtprebiasdual" => "Transistor Bipolar Dual Pre-polarizado",
                "bjtsidual" => "Transistor Bipolar Dual de Silicio",
                "jfet" => "Transistor JFET",
                "mosfet" => "Transistor MOSFET",
                "mosfetdual" => "Transistor MOSFET Dual",
                "igbt" => "Transistor IGBT",
                "igbtdual" => "Transistor IGBT Dual",
                _ => "Transistor"
            };
        }

        private void ConfigureParameters(ITransistor transistor)
        {
            Parameters.Clear();

            // Obtener propiedades que son parámetros (excluyendo Id, Name, StructId, CapsIds)
            var props = transistor.GetType().GetProperties()
                .Where(p => p.Name != "Id" && p.Name != "Name" && p.Name != "StructId" &&
                           p.Name != "CapsIds" && p.Name != "R1" && p.Name != "R2")
                .ToList();

            // Configurar encabezados para la tabla de reemplazos (sin 'Nombre', que se muestra aparte)
            ReplacementHeaders.Clear();
            foreach (var prop in props)
            {
                ReplacementHeaders.Add(GetParameterDisplayName(prop.Name));
            }
            System.Diagnostics.Debug.WriteLine($"ReplacementHeaders: {string.Join(",", ReplacementHeaders)}");
            OnPropertyChanged(nameof(ReplacementHeaders));

            foreach (var prop in props)
            {
                var value = prop.GetValue(transistor);
                var param = new TransistorParameter
                {
                    Name = prop.Name,
                    DisplayName = GetParameterDisplayName(prop.Name),
                    Unit = GetParameterUnit(prop.Name),
                    DefaultValue = value?.ToString() ?? "0"
                };

                param.Value = param.DefaultValue;
                Parameters.Add(param);
            }
        }

        private string GetParameterDisplayName(string fieldName)
        {
            return fieldName switch
            {
                "Pc" or "Pd" => "Disipación de potencia",
                "Vcb" => "Voltaje Colector-Base",
                "Vce" => "Voltaje Colector-Emisor",
                "Veb" => "Voltaje Emisor-Base",
                "Vds" => "Voltaje Drenador-Fuente",
                "Vgs" => "Voltaje Compuerta-Fuente",
                "Vgsth" => "Voltaje Umbral",
                "Vcesat" => "VCE Saturación",
                "Veg" => "VEG",
                "Ic" or "Id" => "Corriente",
                "Tj" => "Temperatura de unión",
                "Ft" => "Frecuencia de transición",
                "Cc" => "Capacitancia",
                "Hfe" => "Ganancia",
                "Qg" => "Carga de compuerta",
                "Tr" => "Tiempo de subida",
                "Cd" => "Capacitancia de salida",
                "Rds" => "Resistencia",
                _ => fieldName
            };
        }

        private string GetParameterUnit(string fieldName)
        {
            return fieldName switch
            {
                "Pc" or "Pd" => "W",
                "Vcb" or "Vce" or "Veb" or "Vds" or "Vgs" or "Vgsth" or "Vcesat" or "Veg" => "V",
                "Ic" or "Id" => "A",
                "Tj" => "°C",
                "Ft" => "MHz",
                "Cc" or "Cd" => "pF",
                "Qg" => "nC",
                "Tr" => "ns",
                "Rds" => "Ω",
                _ => ""
            };
        }

        [RelayCommand]
        private async Task ResetToDefault()
        {
            // Restablecer todos los parámetros a los valores originales
            var props = _originalTransistor.GetType().GetProperties()
                .Where(p => p.Name != "Id" && p.Name != "Name" && p.Name != "StructId" &&
                           p.Name != "CapsIds" && p.Name != "R1" && p.Name != "R2");

            foreach (var prop in props)
            {
                var value = prop.GetValue(_originalTransistor);
                var param = Parameters.FirstOrDefault(p => p.Name == prop.Name);
                if (param != null)
                {
                    param.Value = value?.ToString() ?? "0";
                }
            }

            // Recargar reemplazos
            await LoadReplacementsAsync();

            await _dialogService.ShowToastAsync("Valores restablecidos a los valores originales");
        }

        [RelayCommand]
        private async Task ParameterChanged(string parameterName)
        {
            // Actualizar el valor en el transistor actual
            var param = Parameters.FirstOrDefault(p => p.Name == parameterName);
            if (param != null)
            {
                var prop = _currentTransistor.GetType().GetProperty(parameterName);
                if (prop != null)
                {
                    if (double.TryParse(param.Value, out double value))
                    {
                        prop.SetValue(_currentTransistor, value);
                    }
                    else if (string.IsNullOrEmpty(param.Value))
                    {
                        prop.SetValue(_currentTransistor, null);
                    }
                }
            }

            // Recargar reemplazos (con un pequeño delay para evitar muchas llamadas)
            await Task.Delay(500);
            await LoadReplacementsAsync();
        }

        [RelayCommand]
        private async Task ShowEncapsuladoImage()
        {
            if (Encapsulado != null && !string.IsNullOrEmpty(Encapsulado.Imagen))
            {
                try
                {
                    // Buscar la imagen en la carpeta de recursos
                    string imagePath = Path.Combine(FileSystem.AppDataDirectory, "Images", "Packages", Encapsulado.Imagen);

                    // Si no existe, intentar en la carpeta de recursos
                    if (!File.Exists(imagePath))
                    {
                        // Intentar cargar desde recursos embebidos
                        using var stream = await FileSystem.OpenAppPackageFileAsync($"Images/Packages/{Encapsulado.Imagen}");
                        if (stream != null)
                        {
                            // Guardar temporalmente
                            string tempPath = Path.Combine(Path.GetTempPath(), Encapsulado.Imagen);
                            using var fileStream = File.Create(tempPath);
                            await stream.CopyToAsync(fileStream);
                            imagePath = tempPath;
                        }
                    }

                    if (File.Exists(imagePath))
                    {
                        await _dialogService.ShowImagePopupAsync(imagePath, Encapsulado.Nombre);
                    }
                    else
                    {
                        await _dialogService.ShowAlertAsync("Imagen no encontrada",
                            $"No se encontró la imagen '{Encapsulado.Imagen}' para el encapsulado '{Encapsulado.Nombre}'", "OK");
                    }
                }
                catch (Exception ex)
                {
                    await _dialogService.ShowAlertAsync("Error", $"Error al cargar la imagen: {ex.Message}", "OK");
                }
            }
        }

        [RelayCommand]
        private async Task SelectReplacement(object replacement)
        {
            var prop = replacement.GetType().GetProperty("Id");
            if (prop != null)
            {
                int id = (int)prop.GetValue(replacement);
                await _navigationService.NavigateToAsync(nameof(TransistorDetailPage),
                    new Dictionary<string, object>
                    {
                        { "Type", _tableType },
                        { "Id", id }
                    });
            }
        }

        private async Task LoadReplacementsAsync()
        {
            try
            {
                IsBusy = true;

                // Obtener parámetros actuales
                var parameters = new Dictionary<string, object>();
                var props = _currentTransistor.GetType().GetProperties()
                    .Where(p => p.Name != "Id" && p.Name != "Name" && p.Name != "StructId" &&
                               p.Name != "CapsIds" && p.Name != "R1" && p.Name != "R2");

                foreach (var prop in props)
                {
                    var value = prop.GetValue(_currentTransistor);
                    if (value != null && value is double doubleValue)
                    {
                        parameters[prop.Name] = doubleValue;
                    }
                }

                // Añadir el ID para excluir el transistor actual
                parameters["_id"] = _id;
                System.Diagnostics.Debug.WriteLine($"LoadReplacementsAsync: table={_tableType}, id={_id}, structId={_currentTransistor?.StructId}, capsCount={_currentTransistor?.CapsIds?.Count}");
                System.Diagnostics.Debug.WriteLine($"LoadReplacementsAsync parameters: {string.Join(",", parameters.Select(kv => kv.Key + "=" + kv.Value))}");

                // Obtener reemplazos
                var replacements = await _databaseService.GetReplacementsAsync(
                    _tableType,
                    parameters,
                    _currentTransistor?.StructId ?? 0,
                    _currentTransistor?.CapsIds
                );
                System.Diagnostics.Debug.WriteLine($"LoadReplacementsAsync: replacements returned count={replacements?.Count}");
                Replacements.Clear();

                // Convertir a objetos dinámicos para mostrar en la tabla
                if (replacements != null)
                {
                    foreach (var item in replacements)
                    {
                        // Convertir cada item a ReplacementRow si es necesario
                        var row = new ReplacementRow();
                        var propId = item.GetType().GetProperty("Id");
                        if (propId != null)
                            row.Id = (int)propId.GetValue(item);
                        var propName = item.GetType().GetProperty("Name");
                        row.Name = propName?.GetValue(item)?.ToString() ?? string.Empty;


                        // Obtener propiedades dinámicas y rellenar hasta el máximo global
                        var props2 = item.GetType().GetProperties()
                            .Where(p => p.Name != "Id" && p.Name != "Name" && p.Name != "StructId" && p.Name != "CapsIds" && p.Name != "R1" && p.Name != "R2")
                            .ToList();

                        int maxParams = ColumnLayoutHelper.MaxParameterCount;
                        for (int i = 0; i < maxParams; i++)
                        {
                            if (i < props2.Count)
                            {
                                var v = props2[i].GetValue(item);
                                row.Values.Add(v?.ToString() ?? string.Empty);
                            }
                            else
                            {
                                row.Values.Add(string.Empty);
                            }
                        }

                        row.Original = item;
                        Replacements.Add(row);
                    }
                }
                ReplacementCount = Replacements.Count;
                System.Diagnostics.Debug.WriteLine($"LoadReplacementsAsync: ReplacementCount set to {ReplacementCount}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task ExportToPdf()
        {
            try
            {
                IsBusy = true;

                var replacements = Replacements.ToList();
                if (!replacements.Any())
                {
                    await _dialogService.ShowAlertAsync("Sin datos", "No hay reemplazos para exportar", "OK");
                    return;
                }

                // Generar PDF
                string fileName = $"Reemplazos_{TransistorName}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                string filePath = Path.Combine(FileSystem.CacheDirectory, fileName);

                // Implementar generación de PDF
                await GeneratePdfAsync(filePath, replacements);

                // Compartir el archivo
                await Share.Default.RequestAsync(new ShareFileRequest
                {
                    Title = "Compartir PDF de reemplazos",
                    File = new ShareFile(filePath)
                });
            }
            catch (Exception ex)
            {
                await _dialogService.ShowAlertAsync("Error", $"Error al exportar a PDF: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task ExportToExcel()
        {
            try
            {
                IsBusy = true;

                var replacements = Replacements.ToList();
                if (!replacements.Any())
                {
                    await _dialogService.ShowAlertAsync("Sin datos", "No hay reemplazos para exportar", "OK");
                    return;
                }

                // Generar Excel
                string fileName = $"Reemplazos_{TransistorName}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                string filePath = Path.Combine(FileSystem.CacheDirectory, fileName);

                // Implementar generación de Excel
                await GenerateExcelAsync(filePath, replacements);

                // Compartir el archivo
                await Share.Default.RequestAsync(new ShareFileRequest
                {
                    Title = "Compartir Excel de reemplazos",
                    File = new ShareFile(filePath)
                });
            }
            catch (Exception ex)
            {
                await _dialogService.ShowAlertAsync("Error", $"Error al exportar a Excel: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task Import()
        {
            try
            {
                // Seleccionar archivo Excel
                var customFileType = new FilePickerFileType(
                    new Dictionary<DevicePlatform, IEnumerable<string>>
                    {
                        { DevicePlatform.WinUI, new[] { ".xlsx", ".xls" } },
                        { DevicePlatform.macOS, new[] { "xlsx", "xls" } },
                        { DevicePlatform.Android, new[] { "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "application/vnd.ms-excel" } },
                        { DevicePlatform.iOS, new[] { "com.microsoft.excel.xlsx", "com.microsoft.excel.xls" } },
                    });

                var options = new PickOptions
                {
                    PickerTitle = "Seleccionar archivo Excel",
                    FileTypes = customFileType,
                };

                var file = await FilePicker.PickAsync(options);
                if (file != null)
                {
                    // Procesar el archivo Excel
                    await ProcessExcelImportAsync(file.FullPath);
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowAlertAsync("Error", $"Error al importar: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private async Task New()
        {
            // Navegar a la página de creación
            await _navigationService.NavigateToAsync(nameof(TransistorEditPage),
                new Dictionary<string, object>
                {
                    { "Type", _tableType },
                    { "Mode", "New" }
                });
        }

        [RelayCommand]
        private async Task Edit()
        {
            // Navegar a la página de edición
            await _navigationService.NavigateToAsync(nameof(TransistorEditPage),
                new Dictionary<string, object>
                {
                    { "Type", _tableType },
                    { "Id", _id },
                    { "Mode", "Edit" }
                });
        }

        [RelayCommand]
        private async Task Delete()
        {
            var result = await _dialogService.ShowConfirmationAsync(
                "Confirmar eliminación",
                $"¿Está seguro de eliminar el transistor '{TransistorName}'? Esta acción eliminará también las relaciones en cascada.");

            if (result)
            {
                try
                {
                    IsBusy = true;
                    await _databaseService.DeleteTransistorAsync(_tableType, _id);
                    await _dialogService.ShowToastAsync("Transistor eliminado correctamente");
                    await _navigationService.NavigateBackAsync();
                }
                catch (Exception ex)
                {
                    await _dialogService.ShowAlertAsync("Error", $"Error al eliminar: {ex.Message}", "OK");
                }
                finally
                {
                    IsBusy = false;
                }
            }
        }

        private async Task GeneratePdfAsync(string filePath, List<object> replacements)
        {
            // Implementación con iTextSharp o QuestPDF
            // Crear un PDF con tabla de reemplazos
        }

        private async Task GenerateExcelAsync(string filePath, List<object> replacements)
        {
            // Implementación con EPPlus o ClosedXML
            // Crear un Excel con tabla de reemplazos
        }

        private async Task ProcessExcelImportAsync(string filePath)
        {
            // Implementar importación de Excel
            // Validar campos, no nulos, no repetidos
        }
    }
}