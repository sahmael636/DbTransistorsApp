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

    public TransistorListPage(TransistorListViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _initializationTask;
        await _viewModel.OnAppearingAsync();
        BuildTable();
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        await _viewModel.OnDisappearingAsync();
    }

    private void BuildTable()
    {
        if (_viewModel.HeaderFields.Count == 0)
            return;

        BuildHeader();
        BuildItemTemplate();
        _tableBuilt = true;
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
        var border = new Border
        {
            WidthRequest = width,
            Padding = new Thickness(4, 5),
            Stroke = Colors.White.WithAlpha(0.18f),
            StrokeThickness = 0.5,
            Content = new Label
            {
                Text = text,
                TextColor = Colors.White,
                FontAttributes = FontAttributes.Bold,
                FontSize = 11,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center,
                LineBreakMode = LineBreakMode.WordWrap,
                MaxLines = 2
            }
        };
        Grid.SetColumn(border, column);
        return border;
    }

    private void BuildItemTemplate()
    {
        int fieldCount = _viewModel.HeaderFields.Count;
        double columnWidth = _viewModel.ColumnWidth;

        TransistorsCollection.ItemTemplate = new DataTemplate(() =>
        {
            var grid = new Grid
            {
                RowDefinitions = { new RowDefinition(GridLength.Auto) },
                BackgroundColor = Colors.Transparent
            };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(NameColumnWidth) });
            for (int index = 0; index < fieldCount; index++)
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(columnWidth) });

            var tap = new TapGestureRecognizer();
            tap.SetBinding(TapGestureRecognizer.CommandProperty,
                new Binding("BindingContext.SelectTransistorCommand", source: this));
            tap.SetBinding(TapGestureRecognizer.CommandParameterProperty, new Binding("."));
            grid.GestureRecognizers.Add(tap);

            grid.Children.Add(CreateDataCell("Name", 0, NameColumnWidth, 11, FontAttributes.Bold));
            for (int index = 0; index < fieldCount; index++)
                grid.Children.Add(CreateDataCell($"Values[{index}]", index + 1, columnWidth, 10));

            return grid;
        });
    }

    private static View CreateDataCell(
        string bindingPath,
        int column,
        double width,
        double fontSize,
        FontAttributes fontAttributes = FontAttributes.None)
    {
        var label = new Label
        {
            FontSize = fontSize,
            FontAttributes = fontAttributes,
            TextColor = Colors.Black,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
            LineBreakMode = LineBreakMode.TailTruncation,
            MaxLines = 1
        };
        label.SetBinding(Label.TextProperty, bindingPath);

        var border = new Border
        {
            WidthRequest = width,
            MinimumHeightRequest = 36,
            Padding = new Thickness(4, 7),
            Stroke = Color.FromArgb("#E0E0E0"),
            StrokeThickness = 0.5,
            Content = label
        };
        Grid.SetColumn(border, column);
        return border;
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
