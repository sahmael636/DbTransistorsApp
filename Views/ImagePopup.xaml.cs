namespace DbTransistorsApp.Views;

public partial class ImagePopup : ContentPage
{
    public ImagePopup(string imagePath, string title)
    {
        InitializeComponent();
        ImagePath = imagePath;
        PopupTitle = title;
        BindingContext = this;
    }

    public string ImagePath { get; }
    public string PopupTitle { get; }

    private async void OnCloseClicked(object? sender, EventArgs e)
    {
        try
        {
            if (Navigation.ModalStack.Count > 0)
                await Navigation.PopModalAsync(true);
        }
        catch
        {
            if (Shell.Current?.Navigation.ModalStack.Count > 0)
                await Shell.Current.Navigation.PopModalAsync(true);
        }
    }
}
