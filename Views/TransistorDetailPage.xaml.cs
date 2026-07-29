using DbTransistorsApp.ViewModels;
using System.ComponentModel;
using System.Diagnostics;

namespace DbTransistorsApp.Views;

public partial class TransistorDetailPage : ContentPage, IQueryAttributable
{
    private const double NameColumnWidth = 150;
    private readonly TransistorDetailViewModel _viewModel;
    private Task _initializationTask = Task.CompletedTask;
    private bool _firstAppearance = true;

    public TransistorDetailPage(TransistorDetailViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            await _initializationTask;
            if (!_firstAppearance)
                await _viewModel.OnAppearingAsync();
            _firstAppearance = false;
            BuildReplacementTable();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TransistorDetailViewModel.ReplacementHeaders) ||
            e.PropertyName == nameof(TransistorDetailViewModel.ColumnWidth))
        {
            MainThread.BeginInvokeOnMainThread(BuildReplacementTable);
        }
    }

    private void BuildReplacementTable()
    {
        if (_viewModel.ReplacementHeaders.Count == 0)
            return;

        ReplacementsHeader.ColumnDefinitions.Clear();
        ReplacementsHeader.Children.Clear();
        ReplacementsHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(NameColumnWidth) });
        ReplacementsHeader.Children.Add(CreateHeaderCell("Nombre", 0, NameColumnWidth));

        for (int index = 0; index < _viewModel.ReplacementHeaders.Count; index++)
        {
            ReplacementsHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(_viewModel.ColumnWidth) });
            ReplacementsHeader.Children.Add(CreateHeaderCell(
                _viewModel.ReplacementHeaders[index], index + 1, _viewModel.ColumnWidth));
        }

        int fieldCount = _viewModel.ReplacementHeaders.Count;
        double columnWidth = _viewModel.ColumnWidth;
        ReplacementsCollection.ItemTemplate = new DataTemplate(() =>
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(NameColumnWidth) });
            for (int index = 0; index < fieldCount; index++)
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(columnWidth) });

            var tap = new TapGestureRecognizer();
            tap.SetBinding(TapGestureRecognizer.CommandProperty,
                new Binding("BindingContext.SelectReplacementCommand", source: this));
            tap.SetBinding(TapGestureRecognizer.CommandParameterProperty, new Binding("."));
            grid.GestureRecognizers.Add(tap);

            grid.Children.Add(CreateDataCell("Name", 0, NameColumnWidth, FontAttributes.Bold));
            for (int index = 0; index < fieldCount; index++)
                grid.Children.Add(CreateDataCell($"Values[{index}]", index + 1, columnWidth));
            return grid;
        });
    }

    private static View CreateHeaderCell(string text, int column, double width)
    {
        var cell = new Border
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
        Grid.SetColumn(cell, column);
        return cell;
    }

    private static View CreateDataCell(
        string bindingPath,
        int column,
        double width,
        FontAttributes fontAttributes = FontAttributes.None)
    {
        var label = new Label
        {
            FontSize = 10,
            FontAttributes = fontAttributes,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
            TextColor = Colors.Black,
            LineBreakMode = LineBreakMode.TailTruncation,
            MaxLines = 1
        };
        label.SetBinding(Label.TextProperty, bindingPath);
        var cell = new Border
        {
            WidthRequest = width,
            MinimumHeightRequest = 36,
            Padding = new Thickness(4, 7),
            Stroke = Color.FromArgb("#E0E0E0"),
            StrokeThickness = 0.5,
            Content = label
        };
        Grid.SetColumn(cell, column);
        return cell;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        try
        {
            query.TryGetValue("Type", out object? typeObj);
            if (typeObj == null)
                query.TryGetValue("type", out typeObj);

            query.TryGetValue("Id", out object? idObj);
            if (idObj == null)
                query.TryGetValue("id", out idObj);

            if (typeObj != null && int.TryParse(idObj?.ToString(), out int id))
            {
                _firstAppearance = true;
                _initializationTask = MainThread.InvokeOnMainThreadAsync(
                    () => _viewModel.InitializeAsync(typeObj.ToString() ?? string.Empty, id));
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"TransistorDetailPage.ApplyQueryAttributes: {ex}");
        }
    }
}
