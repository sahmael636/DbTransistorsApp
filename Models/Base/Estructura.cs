using CommunityToolkit.Mvvm.ComponentModel;
using SQLite;

namespace DbTransistorsApp.Models.Base;

[Table("estructuras")]
public class Estructura : ObservableObject
{
    [PrimaryKey, AutoIncrement, Column("id")]
    public int Id { get; set; }

    [Column("nombre")]
    public string Nombre { get; set; } = string.Empty;

    private bool _isSelected;
    [Ignore]
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (SetProperty(ref _isSelected, value))
            {
                OnPropertyChanged(nameof(BackgroundColor));
                OnPropertyChanged(nameof(BorderColor));
            }
        }
    }

    [Ignore]
    public Color BackgroundColor => IsSelected ? Color.FromArgb("#E3F2FD") : Colors.White;

    [Ignore]
    public Color BorderColor => IsSelected ? Color.FromArgb("#2196F3") : Color.FromArgb("#D0D0D0");
}
