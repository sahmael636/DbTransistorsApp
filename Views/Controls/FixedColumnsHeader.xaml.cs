using Microsoft.Maui.Controls;
using System.Collections.Generic;
using System.Linq;

namespace DbTransistorsApp.Views.Controls;

public partial class FixedColumnsHeader : ContentView
{
    public static readonly BindableProperty ColumnsProperty = BindableProperty.Create(
        nameof(Columns), typeof(IEnumerable<string>), typeof(FixedColumnsHeader), null, propertyChanged: OnColumnsChanged);

    public IEnumerable<string> Columns
    {
        get => (IEnumerable<string>)GetValue(ColumnsProperty);
        set => SetValue(ColumnsProperty, value);
    }

    public FixedColumnsHeader()
    {
        InitializeComponent();
    }

    private static void OnColumnsChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is FixedColumnsHeader ctrl)
        {
            ctrl.BuildHeader();
        }
    }

    private void BuildHeader()
    {
        RootGrid.Children.Clear();
        if (Columns == null) return;

        int col = 0;
        foreach (var c in Columns)
        {
            var lbl = new Label { Text = c, FontAttributes = FontAttributes.Bold, TextColor = Colors.White };
            RootGrid.Add(lbl, col++, 0);
        }
    }
}
