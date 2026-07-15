// ViewModels/Base/TableType.cs
public enum TableType
{
    BjtGe,
    BjtSi,
    BjtPrebias,
    BjtPrebiasDual,
    BjtSiDual,
    Jfet,
    Mosfet,
    MosfetDual,
    Igbt,
    IgbtDual
}

public static class TableTypeExtensions
{
    public static string GetTableName(this TableType type)
    {
        return type switch
        {
            TableType.BjtGe => "bjtge",
            TableType.BjtSi => "bjtsi",
            TableType.BjtPrebias => "bjtprebias",
            TableType.BjtPrebiasDual => "bjtprebiasdual",
            TableType.BjtSiDual => "bjtsidual",
            TableType.Jfet => "jfet",
            TableType.Mosfet => "mosfet",
            TableType.MosfetDual => "mosfetdual",
            TableType.Igbt => "igbt",
            TableType.IgbtDual => "igbtdual",
            _ => throw new ArgumentException("Tipo de tabla no válido")
        };
    }

    public static string GetDisplayName(this TableType type)
    {
        return type switch
        {
            TableType.BjtGe => "Bipolar Germanium",
            TableType.BjtSi => "Bipolar Silicio",
            TableType.BjtPrebias => "Pre-biased Bipolar",
            TableType.BjtPrebiasDual => "Dual Pre-biased Bipolar",
            TableType.BjtSiDual => "Dual Bipolar Silicio",
            TableType.Jfet => "JFET",
            TableType.Mosfet => "MOSFET",
            TableType.MosfetDual => "Dual MOSFET",
            TableType.Igbt => "IGBT",
            TableType.IgbtDual => "Dual IGBT",
            _ => throw new ArgumentException("Tipo de tabla no válido")
        };
    }

    public static Type GetModelType(this TableType type)
    {
        return type switch
        {
            TableType.BjtGe => typeof(BjtGe),
            TableType.BjtSi => typeof(BjtSi),
            TableType.BjtPrebias => typeof(BjtPrebias),
            TableType.BjtPrebiasDual => typeof(BjtPrebiasDual),
            TableType.BjtSiDual => typeof(BjtSiDual),
            TableType.Jfet => typeof(Jfet),
            TableType.Mosfet => typeof(Mosfet),
            TableType.MosfetDual => typeof(MosfetDual),
            TableType.Igbt => typeof(Igbt),
            TableType.IgbtDual => typeof(IgbtDual),
            _ => throw new ArgumentException("Tipo de tabla no válido")
        };
    }
}