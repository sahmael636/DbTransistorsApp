using DbTransistorsApp.ViewModels;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System.ComponentModel;
using System.Diagnostics;

namespace DbTransistorsApp.Views;

public partial class TransistorDetailPage : ContentPage, IQueryAttributable
{
    private readonly TransistorDetailViewModel _viewModel;
    private bool _replacementsHeaderBuilt = false;

    public TransistorDetailPage(TransistorDetailViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    private void ViewModel_PropertyChanged(object sender,
    PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TransistorDetailViewModel.ReplacementHeaders))
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                BuildReplacementsHeader(_viewModel);
            });
        }
    }

    // Recibir parámetros de navegación cuando se usa Shell GoToAsync con parámetros
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        try
        {
            if (BindingContext is TransistorDetailViewModel vm)
            {
                object typeObj = null;
                object idObj = null;

                if (!query.TryGetValue("type", out typeObj))
                    query.TryGetValue("Type", out typeObj);

                if (!query.TryGetValue("id", out idObj))
                    query.TryGetValue("Id", out idObj);

                if (typeObj != null && idObj != null && int.TryParse(idObj.ToString(), out int id))
                {
                    var type = typeObj.ToString();
                    Debug.WriteLine($"ApplyQueryAttributes: type={type}, id={id}");
                    // Ejecutar inicialización asíncrona en hilo de UI
                    _ = MainThread.InvokeOnMainThreadAsync(async () => await vm.InitializeAsync(type, id));
                }
            }
        }
        catch (Exception ex)
        {
            // evitar que errores de navegación rompan la experiencia; mostrar alerta si es necesario
            System.Diagnostics.Debug.WriteLine($"ApplyQueryAttributes error: {ex}");
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            // ✅ Obtener parámetros de navegación
            if (BindingContext is TransistorDetailViewModel vm)
            {
                // Verificar si el ViewModel ya tiene los datos
                if (string.IsNullOrEmpty(vm.TransistorName))
                {
                    await vm.OnAppearingAsync();
                }

                // Construir encabezado de reemplazos si es necesario
                if (!_replacementsHeaderBuilt && vm.ReplacementHeaders != null)
                {
                    BuildReplacementsHeader(vm);
                    _replacementsHeaderBuilt = true;
                }

                // Construir ItemTemplate dinámico para Replacements
                var coll = this.FindByName<CollectionView>("ReplacementsCollection");
                if (coll != null && coll.ItemTemplate == null)
                {
                    BuildReplacementsItemTemplate();
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al cargar: {ex.Message}", "OK");
        }
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        await _viewModel.OnDisappearingAsync();
    }

    private void BuildReplacementsHeader(TransistorDetailViewModel vm)
    {
        try
        {
            var grid = this.FindByName<Grid>("ReplacementsHeader");
            if (grid == null)
                return;

            grid.ColumnDefinitions.Clear();
            grid.Children.Clear();

            // Columna fija Nombre
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });

            var lblName = new Label { Text = "Nombre", FontAttributes = FontAttributes.Bold, TextColor = Colors.Black, VerticalOptions = LayoutOptions.Center };
            grid.Add(lblName, 0, 0);

            var stack = new HorizontalStackLayout
            {
                Spacing = 0,
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Center
            };
            for (int i = 0; i < vm.ReplacementHeaders.Count; i++)
            {
                var border = new Border
                {
                    Padding = new Thickness(4, 0),
                    StrokeThickness = 0,
                    BackgroundColor = Colors.Transparent,
                    WidthRequest = vm.ColumnWidth,
                    HeightRequest = 40
                };
                var lbl = new Label { Text = vm.ReplacementHeaders[i], FontAttributes = FontAttributes.Bold, VerticalOptions = LayoutOptions.Center, HorizontalTextAlignment = TextAlignment.Center };
                border.Content = lbl;
                stack.Add(border);
            }
            grid.Add(stack, 1, 0);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"BuildReplacementsHeader error: {ex}");
        }
    }

    private void BuildReplacementsItemTemplate()
    {
        var collection = this.FindByName<CollectionView>("ReplacementsCollection");
        if (collection == null) return;

        var vm = (TransistorDetailViewModel)BindingContext;

        collection.ItemTemplate = new DataTemplate(() =>
        {
            var grid = new Grid
            {
                Padding = new Thickness(5, 2),
                BackgroundColor = Colors.Transparent
            };

            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });

            var tap = new TapGestureRecognizer();
            tap.SetBinding(
                TapGestureRecognizer.CommandProperty,
                new Binding("BindingContext.SelectTransistorCommand", source: this));

            tap.SetBinding(
                TapGestureRecognizer.CommandParameterProperty,
                new Binding("Original"));

            grid.GestureRecognizers.Add(tap);

            var lblName = new Label
            {
                FontSize = 13,
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center,
                Padding = new Thickness(5, 6),
                TextColor = Colors.Black
            };

            lblName.SetBinding(Label.TextProperty, "Name");

            Grid.SetColumn(lblName, 0);

            grid.Children.Add(lblName);

            var stack = new HorizontalStackLayout
            {
                Spacing = 0,
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Center
            };

            int max = ColumnLayoutHelper.MaxParameterCount;

            for (int i = 0; i < max; i++)
            {
                var border = new Border
                {
                    Padding = new Thickness(4, 0),
                    StrokeThickness = 0,
                    BackgroundColor = Colors.Transparent,
                    WidthRequest = vm.ColumnWidth,
                    HeightRequest = 38
                };

                var lbl = new Label
                {
                    FontSize = 13,
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center,
                    LineBreakMode = LineBreakMode.TailTruncation
                };

                lbl.SetBinding(Label.TextProperty,
                    new Binding($"Values[{i}]"));

                border.Content = lbl;

                stack.Children.Add(border);
            }

            Grid.SetColumn(stack, 1);

            grid.Children.Add(stack);

            return grid;
        });
    }
}