using DbTransistorsApp.ViewModels;

namespace DbTransistorsApp.Views;

public partial class EncapsuladoEditPage : ContentPage, IQueryAttributable
{
    private readonly EncapsuladoEditViewModel _viewModel;
    private Task _initializationTask = Task.CompletedTask;

    public EncapsuladoEditPage(EncapsuladoEditViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            await _initializationTask;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
            await Shell.Current.GoToAsync("..");
        }
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        string mode = query.TryGetValue("Mode", out object? modeObj) ? modeObj?.ToString() ?? "New" : "New";
        int id = query.TryGetValue("Id", out object? idObj) && int.TryParse(idObj?.ToString(), out int parsed) ? parsed : 0;
        _initializationTask = MainThread.InvokeOnMainThreadAsync(() => _viewModel.InitializeAsync(mode, id));
    }
}
