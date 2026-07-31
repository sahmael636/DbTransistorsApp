using DbTransistorsApp.Helpers;
using DbTransistorsApp.ViewModels;
using System.Diagnostics;

namespace DbTransistorsApp.Views;

public partial class TransistorListPage : ContentPage, IQueryAttributable
{
    private const double NameColumnWidth = 150;
    private readonly TransistorListViewModel _viewModel;
    private Task _initializationTask = Task.CompletedTask;
    private bool _tableBuilt;
    private bool _firstAppearance = true;

    public TransistorListPage(TransistorListViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // InitializeAsync configura los encabezados antes de su primera consulta.
        // Construir la plantilla cuanto antes evita que CollectionView dibuje una
        // lista provisional y luego tenga que reconstruirla completa.
        BuildTable();
        await _initializationTask;
        if (!_firstAppearance)
            await _viewModel.OnAppearingAsync();
        _firstAppearance = false;
        BuildTable();
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        await _viewModel.OnDisappearingAsync();
    }

    private void BuildTable()
    {
        if (_tableBuilt || _viewModel.HeaderFields.Count == 0)
            return;

        var stopwatch = Stopwatch.StartNew();
        BuildHeader();
        BuildItemTemplate();
        _tableBuilt = true;
        Debug.WriteLine($"TransistorList: plantilla visual construida en {stopwatch.ElapsedMilliseconds} ms.");
    }

    private void BuildHeader()
    {
        HeaderArea.ColumnDefinitions.Clear();
        HeaderArea.Children.Clear();
        HeaderArea.Padding = new Thickness(0);

        HeaderArea.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(NameColumnWidth) });
        HeaderArea.Children.Add(CreateHeaderLabel("Nombre", 0, NameColumnWidth));

        for (int index = 0; index < _viewModel.HeaderFields.Count; index++)
        {
            HeaderArea.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(_viewModel.ColumnWidth) });
            HeaderArea.Children.Add(CreateHeaderLabel(
                _viewModel.HeaderFields[index], index + 1, _viewModel.ColumnWidth));
        }
    }

    private static View CreateHeaderLabel(string text, int column, double width)
    {
        var label = new Label
        {
            Text = text,
            WidthRequest = width,
            Padding = new Thickness(4, 5),
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold,
            FontSize = 11,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
            LineBreakMode = LineBreakMode.WordWrap,
            MaxLines = 2
        };
        Grid.SetColumn(label, column);
        return label;
    }

    private void BuildItemTemplate()
    {
        int fieldCount = _viewModel.HeaderFields.Count;
        double columnWidth = _viewModel.ColumnWidth;

        TransistorsCollection.ItemTemplate = new DataTemplate(() =>
        {
            var grid = new Grid
            {
                HeightRequest = 38,
                RowSpacing = 0,
                ColumnSpacing = 0,
                BackgroundColor = Colors.White
            };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(NameColumnWidth) });
            for (int index = 0; index < fieldCount; index++)
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(columnWidth) });

            var tap = new TapGestureRecognizer();
            tap.SetBinding(TapGestureRecognizer.CommandProperty,
                new Binding("BindingContext.SelectTransistorCommand", source: this));
            tap.SetBinding(TapGestureRecognizer.CommandParameterProperty, new Binding("."));
            grid.GestureRecognizers.Add(tap);

            grid.Children.Add(CreateDataLabel("Name", 0, NameColumnWidth, 11, FontAttributes.Bold));
            for (int index = 0; index < fieldCount; index++)
                grid.Children.Add(CreateDataLabel($"Values[{index}]", index + 1, columnWidth, 10));

            var separator = new BoxView
            {
                HeightRequest = 0.5,
                BackgroundColor = Color.FromArgb("#E0E0E0"),
                VerticalOptions = LayoutOptions.End
            };
            Grid.SetColumnSpan(separator, fieldCount + 1);
            grid.Children.Add(separator);

            return grid;
        });
    }

    private static View CreateDataLabel(
        string bindingPath,
        int column,
        double width,
        double fontSize,
        FontAttributes fontAttributes = FontAttributes.None)
    {
        var label = new Label
        {
            WidthRequest = width,
            Padding = new Thickness(4, 5),
            FontSize = fontSize,
            FontAttributes = fontAttributes,
            TextColor = Colors.Black,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
            LineBreakMode = LineBreakMode.TailTruncation,
            MaxLines = 1
        };
        label.SetBinding(Label.TextProperty, bindingPath);
        Grid.SetColumn(label, column);
        return label;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        try
        {
            if (query.TryGetValue("TableType", out object? tableObj) ||
                query.TryGetValue("tableType", out tableObj))
            {
                string? value = tableObj?.ToString();
                if (!string.IsNullOrWhiteSpace(value) && Enum.TryParse(value, true, out TableType tableType))
                {
                    _tableBuilt = false;
                    _firstAppearance = true;
                    _initializationTask = MainThread.InvokeOnMainThreadAsync(
                        () => _viewModel.InitializeAsync(tableType));
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"TransistorListPage.ApplyQueryAttributes: {ex}");
        }
    }
}
