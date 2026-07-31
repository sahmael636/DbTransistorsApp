using DbTransistorsApp.ViewModels;

namespace DbTransistorsApp.Views;

public partial class TransistorEditPage : ContentPage, IQueryAttributable
{
    private readonly TransistorEditViewModel _viewModel;
    private Task _initializationTask = Task.CompletedTask;
    private string? _queryKey;

    public TransistorEditPage(TransistorEditViewModel viewModel)
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
        string type = query.TryGetValue("Type", out object? typeObj) ? typeObj?.ToString() ?? string.Empty : string.Empty;
        string mode = query.TryGetValue("Mode", out object? modeObj) ? modeObj?.ToString() ?? "New" : "New";
        int id = query.TryGetValue("Id", out object? idObj) && int.TryParse(idObj?.ToString(), out int parsed) ? parsed : 0;
        string key = $"{type}|{id}|{mode}";
        if (string.Equals(_queryKey, key, StringComparison.OrdinalIgnoreCase))
            return;

        _queryKey = key;
        _initializationTask = MainThread.InvokeOnMainThreadAsync(() => _viewModel.InitializeAsync(type, id, mode));
    }
}
