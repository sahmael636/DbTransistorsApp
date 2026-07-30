using DbTransistorsApp.Models.Base;
using DbTransistorsApp.ViewModels;

namespace DbTransistorsApp.Views;

public partial class EncapsuladoSelectorPage : ContentPage
{
    private readonly TaskCompletionSource<IReadOnlyList<int>?> _resultSource =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private bool _completed;

    public EncapsuladoSelectorPage(
        IEnumerable<Encapsulado> encapsulados,
        IEnumerable<int> selectedIds)
    {
        InitializeComponent();
        var viewModel = new EncapsuladoSelectorViewModel(encapsulados, selectedIds);
        viewModel.Completed += OnCompleted;
        BindingContext = viewModel;
    }

    public Task<IReadOnlyList<int>?> ResultTask => _resultSource.Task;

    private void OnCompleted(object? sender, IReadOnlyList<int>? result)
    {
        if (_completed)
            return;

        _completed = true;
        _resultSource.TrySetResult(result);
    }

    protected override bool OnBackButtonPressed()
    {
        if (!_completed)
        {
            _completed = true;
            _resultSource.TrySetResult(null);
        }
        return true;
    }
}
