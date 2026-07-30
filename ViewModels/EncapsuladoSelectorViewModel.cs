using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DbTransistorsApp.Models.Base;

namespace DbTransistorsApp.ViewModels;

public partial class EncapsuladoSelectorViewModel : ObservableObject
{
    private readonly List<EncapsuladoSelectionItem> _allItems;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<EncapsuladoSelectionItem> _filteredItems = Array.Empty<EncapsuladoSelectionItem>();

    [ObservableProperty]
    private int _selectedCount;

    public string SelectionSummary => SelectedCount == 1
        ? "1 encapsulado seleccionado"
        : $"{SelectedCount:N0} encapsulados seleccionados";

    public event EventHandler<IReadOnlyList<int>?>? Completed;

    public EncapsuladoSelectorViewModel(
        IEnumerable<Encapsulado> encapsulados,
        IEnumerable<int> selectedIds)
    {
        var selected = selectedIds.ToHashSet();
        _allItems = encapsulados
            .OrderBy(x => x.Nombre, StringComparer.CurrentCultureIgnoreCase)
            .Select(x => new EncapsuladoSelectionItem(
                x.Id,
                x.Nombre,
                selected.Contains(x.Id),
                UpdateSelectedCount))
            .ToList();

        FilteredItems = _allItems;
        UpdateSelectedCount();
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter(value);

    private void ApplyFilter(string? search)
    {
        string term = search?.Trim() ?? string.Empty;
        if (term.Length == 0)
        {
            FilteredItems = _allItems;
            return;
        }

        FilteredItems = _allItems
            .Where(x => x.Nombre.Contains(term, StringComparison.CurrentCultureIgnoreCase)
                || x.Id.ToString().Contains(term, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private void UpdateSelectedCount()
    {
        SelectedCount = _allItems.Count(x => x.IsSelected);
        OnPropertyChanged(nameof(SelectionSummary));
    }

    [RelayCommand]
    private void Toggle(EncapsuladoSelectionItem item)
        => item.IsSelected = !item.IsSelected;

    [RelayCommand]
    private void ClearSelection()
    {
        foreach (var item in _allItems.Where(x => x.IsSelected))
            item.IsSelected = false;
    }

    [RelayCommand]
    private void Accept()
        => Completed?.Invoke(this, _allItems.Where(x => x.IsSelected).Select(x => x.Id).ToList());

    [RelayCommand]
    private void Cancel() => Completed?.Invoke(this, null);
}

public class EncapsuladoSelectionItem : ObservableObject
{
    private readonly Action _selectionChanged;
    private bool _isSelected;

    public EncapsuladoSelectionItem(int id, string nombre, bool isSelected, Action selectionChanged)
    {
        Id = id;
        Nombre = nombre;
        _isSelected = isSelected;
        _selectionChanged = selectionChanged;
    }

    public int Id { get; }
    public string Nombre { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (SetProperty(ref _isSelected, value))
                _selectionChanged();
        }
    }
}
