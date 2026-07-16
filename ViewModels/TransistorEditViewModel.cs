// ViewModels/TransistorEditViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DbTransistorsApp.Models.Base;
using DbTransistorsApp.Services;
using DbTransistorsApp.ViewModels.Base;
//using IntelliJ.Lang.Annotations;
using System.Collections.ObjectModel;
//using static Android.Icu.Text.CaseMap;
//using static Java.Util.Jar.Attributes;

namespace DbTransistorsApp.ViewModels
{
    public partial class TransistorEditViewModel : BaseViewModel
    {
        private readonly DatabaseService _databaseService;
        private readonly NavigationService _navigationService;
        private readonly DialogService _dialogService;
        private string _tableType;
        private int _id;
        private string _mode;
        private Type _modelType;
        private ITransistor _transistor;

        [ObservableProperty]
        private string _name;

        [ObservableProperty]
        private ObservableCollection<ParameterField> _parameters = new();

        [ObservableProperty]
        private ObservableCollection<Estructura> _estructuras = new();

        [ObservableProperty]
        private Estructura _selectedEstructura;

        [ObservableProperty]
        private ObservableCollection<Encapsulado> _allEncapsulados = new();

        [ObservableProperty]
        private ObservableCollection<Encapsulado> _selectedEncapsulados = new();

        [ObservableProperty]
        private string _title;

        public TransistorEditViewModel(DatabaseService databaseService, NavigationService navigationService, DialogService dialogService)
        {
            _databaseService = databaseService;
            _navigationService = navigationService;
            _dialogService = dialogService;
        }

        public async Task InitializeAsync(string type, int id, string mode)
        {
            _tableType = type;
            _id = id;
            _mode = mode;
            _modelType = GetModelType(type);

            // Cargar estructuras disponibles
            await LoadEstructurasAsync();

            // Cargar encapsulados disponibles
            await LoadEncapsuladosAsync();

            if (_mode == "New")
            {
                Title = $"Nuevo Transistor {GetTransistorDisplayType(type)}";
                _transistor = (ITransistor)Activator.CreateInstance(_modelType);
                InitializeNewTransistor();
            }
            else
            {
                Title = $"Editar Transistor";
                _transistor = await _databaseService.GetTransistorByTypeAndIdAsync(type, id);
                if (_transistor == null)
                {
                    await _dialogService.ShowAlertAsync("Error", "Transistor no encontrado", "OK");
                    await _navigationService.NavigateBackAsync();
                    return;
                }
                LoadTransistorData();
            }
        }

        private void InitializeNewTransistor()
        {
            Name = string.Empty;
            Parameters.Clear();

            // Inicializar parámetros con valores predeterminados
            var props = _modelType.GetProperties()
                .Where(p => p.Name != "Id" && p.Name != "Name" && p.Name != "StructId" &&
                           p.Name != "CapsIds" && p.Name != "R1" && p.Name != "R2");

            foreach (var prop in props)
            {
                Parameters.Add(new ParameterField
                {
                    Name = prop.Name,
                    DisplayName = GetParameterDisplayName(prop.Name),
                    Value = "0",
                    Unit = GetParameterUnit(prop.Name),
                    IsRequired = IsRequiredParameter(prop.Name)
                });
            }
        }

        private void LoadTransistorData()
        {
            Name = _transistor.Name;
            Parameters.Clear();

            // Cargar parámetros existentes
            var props = _modelType.GetProperties()
                .Where(p => p.Name != "Id" && p.Name != "Name" && p.Name != "StructId" &&
                           p.Name != "CapsIds" && p.Name != "R1" && p.Name != "R2");

            foreach (var prop in props)
            {
                var value = prop.GetValue(_transistor);
                Parameters.Add(new ParameterField
                {
                    Name = prop.Name,
                    DisplayName = GetParameterDisplayName(prop.Name),
                    Value = value?.ToString() ?? "0",
                    Unit = GetParameterUnit(prop.Name),
                    IsRequired = IsRequiredParameter(prop.Name)
                });
            }

            // Seleccionar estructura
            SelectedEstructura = Estructuras.FirstOrDefault(e => e.Id == _transistor.StructId);

            // Seleccionar encapsulados
            SelectedEncapsulados = new ObservableCollection<Encapsulado>(
                AllEncapsulados.Where(e => _transistor.CapsIds.Contains(e.Id)));
        }

        private async Task LoadEstructurasAsync()
        {
            var all = await _databaseService.GetAllEstructurasAsync();
            Estructuras.Clear();
            foreach (var item in all)
            {
                Estructuras.Add(item);
            }
        }

        private async Task LoadEncapsuladosAsync()
        {
            var all = await _databaseService.GetAllEncapsuladosAsync();
            AllEncapsulados.Clear();
            foreach (var item in all)
            {
                AllEncapsulados.Add(item);
            }
        }

        [RelayCommand]
        private async Task Save()
        {
            // Validar nombre
            if (string.IsNullOrWhiteSpace(Name))
            {
                await _dialogService.ShowAlertAsync("Error", "El nombre del transistor es obligatorio", "OK");
                return;
            }

            // Validar campos requeridos
            var requiredFields = Parameters.Where(p => p.IsRequired);
            foreach (var field in requiredFields)
            {
                if (string.IsNullOrWhiteSpace(field.Value) || field.Value == "0")
                {
                    await _dialogService.ShowAlertAsync("Error", $"El campo '{field.DisplayName}' es obligatorio", "OK");
                    return;
                }
            }

            // Validar estructura
            if (SelectedEstructura == null)
            {
                await _dialogService.ShowAlertAsync("Error", "Debe seleccionar una estructura", "OK");
                return;
            }

            // Validar encapsulado
            if (!SelectedEncapsulados.Any())
            {
                await _dialogService.ShowAlertAsync("Error", "Debe seleccionar al menos un encapsulado", "OK");
                return;
            }

            try
            {
                IsBusy = true;

                // Actualizar valores
                _transistor.Name = Name;
                _transistor.StructId = SelectedEstructura.Id;

                // Actualizar parámetros
                foreach (var param in Parameters)
                {
                    var prop = _modelType.GetProperty(param.Name);
                    if (prop != null)
                    {
                        if (double.TryParse(param.Value, out double doubleValue))
                        {
                            prop.SetValue(_transistor, doubleValue);
                        }
                        else
                        {
                            prop.SetValue(_transistor, null);
                        }
                    }
                }

                // Actualizar encapsulados
                _transistor.CapsIds = SelectedEncapsulados.Select(e => e.Id).ToList();

                if (_mode == "New")
                {
                    await _databaseService.InsertTransistorAsync(_tableType, _transistor);
                }
                else
                {
                    await _databaseService.UpdateTransistorAsync(_tableType, _transistor);
                }

                await _navigationService.NavigateBackAsync();
                await _dialogService.ShowToastAsync($"Transistor {(_mode == "New" ? "creado" : "actualizado")} correctamente");
            }
            catch (Exception ex)
            {
                await _dialogService.ShowAlertAsync("Error", $"Error al guardar: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task Cancel()
        {
            await _navigationService.NavigateBackAsync();
        }

        [RelayCommand]
        private void ToggleEncapsulado(Encapsulado encapsulado)
        {
            if (SelectedEncapsulados.Contains(encapsulado))
            {
                SelectedEncapsulados.Remove(encapsulado);
            }
            else
            {
                SelectedEncapsulados.Add(encapsulado);
            }
        }

        private Type GetModelType(string type)
        {
            return type switch
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

        private string GetTransistorDisplayType(string type)
        {
            return type switch
            {
                "bjtge" => "Bipolar Germanium",
                "bjtsi" => "Bipolar Silicio",
                "bjtprebias" => "Bipolar Pre-polarizado",
                "bjtprebiasdual" => "Bipolar Dual Pre-polarizado",
                "bjtsidual" => "Bipolar Dual de Silicio",
                "jfet" => "JFET",
                "mosfet" => "MOSFET",
                "mosfetdual" => "MOSFET Dual",
                "igbt" => "IGBT",
                "igbtdual" => "IGBT Dual",
                _ => "Transistor"
            };
        }

        private string GetParameterDisplayName(string fieldName)
        {
            return fieldName switch
            {
                "Pc" or "Pd" => "Potencia",
                "Vcb" => "VCB",
                "Vce" => "VCE",
                "Veb" => "VEB",
                "Vds" => "VDS",
                "Vgs" => "VGS",
                "Vgsth" => "VGSTH",
                "Vcesat" => "VCESAT",
                "Veg" => "VEG",
                "Ic" or "Id" => "IC",
                "Tj" => "TJ",
                "Ft" => "Ft",
                "Cc" => "CC",
                "Hfe" => "Hfe",
                "Qg" => "QG",
                "Tr" => "Tr",
                "Cd" => "CD",
                "Rds" => "RDS",
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

        private bool IsRequiredParameter(string fieldName)
        {
            // Definir qué parámetros son obligatorios
            return fieldName switch
            {
                "Pc" or "Pd" => true,
                "Vce" or "Vds" => true,
                "Ic" or "Id" => true,
                _ => false
            };
        }
    }

    public class ParameterField : ObservableObject
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Unit { get; set; }
        public bool IsRequired { get; set; }

        private string _value;
        public string Value
        {
            get => _value;
            set => SetProperty(ref _value, value);
        }
    }
}