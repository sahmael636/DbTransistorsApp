// ViewModels/FilterField.cs
using CommunityToolkit.Mvvm.ComponentModel;

namespace DbTransistorsApp.ViewModels
{
    public partial class FilterField : ObservableObject
    {
        private string _displayName;
        private string _field;
        private string _unit;
        private string _minValue = "0";
        private string _maxValue = "9999";
        private string _textValue;
        private bool _isTextFilter;

        public string DisplayName
        {
            get => _displayName;
            set => SetProperty(ref _displayName, value);
        }

        public string Field
        {
            get => _field;
            set => SetProperty(ref _field, value);
        }

        public string Unit
        {
            get => _unit;
            set => SetProperty(ref _unit, value);
        }

        public string MinValue
        {
            get => _minValue;
            set => SetProperty(ref _minValue, value);
        }

        public string MaxValue
        {
            get => _maxValue;
            set => SetProperty(ref _maxValue, value);
        }

        public string TextValue
        {
            get => _textValue;
            set => SetProperty(ref _textValue, value);
        }

        public bool IsTextFilter
        {
            get => _isTextFilter;
            set => SetProperty(ref _isTextFilter, value);
        }

        // Constructor por defecto
        public FilterField()
        {
            IsTextFilter = false;
            MinValue = "0";
            MaxValue = "9999";
        }

        // Constructor para filtros numéricos
        public FilterField(string displayName, string field, string unit = "")
        {
            DisplayName = displayName;
            Field = field;
            Unit = unit;
            IsTextFilter = false;
            MinValue = "0";
            MaxValue = "9999";
        }

        // Constructor para filtros de texto
        public FilterField(string displayName, string field, bool isTextFilter)
        {
            DisplayName = displayName;
            Field = field;
            IsTextFilter = isTextFilter;
            TextValue = string.Empty;
        }

        // Propiedades calculadas
        public double? MinValueAsDouble
        {
            get
            {
                if (double.TryParse(MinValue, out double result))
                    return result;
                return null;
            }
        }

        public double? MaxValueAsDouble
        {
            get
            {
                if (double.TryParse(MaxValue, out double result))
                    return result;
                return null;
            }
        }

        public bool HasMinValue => MinValueAsDouble.HasValue && MinValueAsDouble.Value > 0;
        public bool HasMaxValue => MaxValueAsDouble.HasValue && MaxValueAsDouble.Value < 9999;
        public bool HasTextValue => IsTextFilter && !string.IsNullOrWhiteSpace(TextValue);

        // Método para limpiar el filtro
        public void Clear()
        {
            if (IsTextFilter)
            {
                TextValue = string.Empty;
            }
            else
            {
                MinValue = "0";
                MaxValue = "9999";
            }
        }

        // Método para verificar si el filtro está activo
        public bool IsActive
        {
            get
            {
                if (IsTextFilter)
                    return !string.IsNullOrWhiteSpace(TextValue);
                else
                    return (HasMinValue || HasMaxValue);
            }
        }

        // Método para obtener el valor del filtro para la consulta SQL
        public (string Condition, object Value) GetSqlFilter(string tableAlias = "")
        {
            string prefix = string.IsNullOrEmpty(tableAlias) ? "" : $"{tableAlias}.";

            if (IsTextFilter && HasTextValue)
            {
                return ($"{prefix}{Field} LIKE ?", $"%{TextValue}%");
            }
            else if (!IsTextFilter)
            {
                if (HasMinValue && HasMaxValue)
                {
                    return ($"{prefix}{Field} BETWEEN ? AND ?", new object[] { MinValueAsDouble.Value, MaxValueAsDouble.Value });
                }
                else if (HasMinValue)
                {
                    return ($"{prefix}{Field} >= ?", MinValueAsDouble.Value);
                }
                else if (HasMaxValue)
                {
                    return ($"{prefix}{Field} <= ?", MaxValueAsDouble.Value);
                }
            }

            return (null, null);
        }
    }
}