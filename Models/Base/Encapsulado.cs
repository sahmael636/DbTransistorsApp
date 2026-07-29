using CommunityToolkit.Mvvm.ComponentModel;
using SQLite;

namespace DbTransistorsApp.Models.Base;

[Table("encapsulados")]
public class Encapsulado : ObservableObject
{
    [PrimaryKey, AutoIncrement, Column("id")]
    public int Id { get; set; }

    [Column("nombre")]
    public string Nombre { get; set; } = string.Empty;

    // La base original no siempre trae esta columna. DatabaseService la crea como migración.
    [Column("ruta")]
    public string? Imagen { get; set; }

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

    private string? _imagenPreview;
    [Ignore]
    public string? ImagenPreview
    {
        get => _imagenPreview;
        set => SetProperty(ref _imagenPreview, value);
    }

    [Ignore]
    public Color BackgroundColor => IsSelected ? Color.FromArgb("#E3F2FD") : Colors.White;

    [Ignore]
    public Color BorderColor => IsSelected ? Color.FromArgb("#2196F3") : Color.FromArgb("#D0D0D0");

    [Ignore]
    public bool HasImage => !string.IsNullOrWhiteSpace(Imagen);
}
