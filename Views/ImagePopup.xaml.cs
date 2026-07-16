// Views/ImagePopup.xaml.cs
using System.Windows.Input;

namespace DbTransistorsApp.Views;

public partial class ImagePopup : ContentPage
{
    private readonly string _imagePath;
    private readonly string _title;

    public ImagePopup(string imagePath, string title)
    {
        InitializeComponent();
        _imagePath = imagePath;
        _title = title;

        // Configurar binding
        BindingContext = this;

        // Comando para cerrar
        CloseCommand = new Command(OnClose);
    }

    private void InitializeComponent()
    {
        throw new NotImplementedException();
    }

    public ICommand CloseCommand { get; }

    public string ImagePath => _imagePath;
    public string TitleText => _title;

    private async void OnClose()
    {
        await Navigation.PopModalAsync();
    }
}