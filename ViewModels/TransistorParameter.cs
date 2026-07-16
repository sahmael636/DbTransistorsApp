// ViewModels/TransistorParameter.cs
using CommunityToolkit.Mvvm.ComponentModel;

namespace DbTransistorsApp.ViewModels
{
    public partial class TransistorParameter : ObservableObject
    {
        private string _value;
        private string _displayName;
        private string _unit;
        private string _defaultValue;
        private string _name;
        private bool _isReadOnly;
        private bool _isRequired;

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string DisplayName
        {
            get => _displayName;
            set => SetProperty(ref _displayName, value);
        }

        public string Unit
        {
            get => _unit;
            set => SetProperty(ref _unit, value);
        }

        public string Value
        {
            get => _value;
            set => SetProperty(ref _value, value);
        }

        public string DefaultValue
        {
            get => _defaultValue;
            set => SetProperty(ref _defaultValue, value);
        }

        public bool IsReadOnly
        {
            get => _isReadOnly;
            set => SetProperty(ref _isReadOnly, value);
        }

        public bool IsRequired
        {
            get => _isRequired;
            set => SetProperty(ref _isRequired, value);
        }

        // Constructor para facilitar la creación
        public TransistorParameter()
        {
            IsReadOnly = false;
            IsRequired = false;
        }

        public TransistorParameter(string name, string displayName, string value, string unit = "", string defaultValue = null)
        {
            Name = name;
            DisplayName = displayName;
            Value = value;
            Unit = unit;
            DefaultValue = defaultValue ?? value;
            IsReadOnly = false;
            IsRequired = false;
        }
    }
}