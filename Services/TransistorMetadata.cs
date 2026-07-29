using DbTransistorsApp.Models.Base;
using SQLite;
using System.Reflection;

namespace DbTransistorsApp.Services;

public sealed record TransistorColumn(
    string PropertyName,
    string ColumnName,
    Type PropertyType,
    bool IsNumeric,
    bool IsNullable);

public static class TransistorMetadata
{
    public static readonly string[] TableNames =
    {
        "bjtge", "bjtsi", "bjtsidual", "bjtprebias", "bjtprebiasdual",
        "jfet", "mosfet", "mosfetdual", "igbt", "igbtdual"
    };

    public static readonly string[] RelationTableNames =
        TableNames.Select(name => $"{name}_caps").ToArray();

    public static string NormalizeTableName(string tableName)
    {
        string normalized = tableName?.Trim().ToLowerInvariant()
            ?? throw new ArgumentNullException(nameof(tableName));

        // La tabla byname usa identificadores históricos distintos de los nombres
        // físicos de algunas tablas. Se aceptan ambos para conservar la búsqueda.
        return normalized switch
        {
            "bjtge" => "bjtge",
            "bjtsi" => "bjtsi",
            "bjtsidual" or "bjtdual" => "bjtsidual",
            "bjtprebias" => "bjtprebias",
            "bjtprebiasdual" or "prebiasdual" => "bjtprebiasdual",
            "jfet" => "jfet",
            "mosfet" => "mosfet",
            "mosfetdual" => "mosfetdual",
            "igbt" => "igbt",
            "igbtdual" => "igbtdual",
            _ => throw new ArgumentException($"Tabla o tipo de transistor no válido: {tableName}", nameof(tableName))
        };
    }

    public static Type GetModelType(string tableName)
    {
        return NormalizeTableName(tableName) switch
        {
            "bjtge" => typeof(BjtGe),
            "bjtsi" => typeof(BjtSi),
            "bjtsidual" => typeof(BjtSiDual),
            "bjtprebias" => typeof(BjtPrebias),
            "bjtprebiasdual" => typeof(BjtPrebiasDual),
            "jfet" => typeof(Jfet),
            "mosfet" => typeof(Mosfet),
            "mosfetdual" => typeof(MosfetDual),
            "igbt" => typeof(Igbt),
            "igbtdual" => typeof(IgbtDual),
            _ => throw new ArgumentOutOfRangeException(nameof(tableName))
        };
    }

    public static string GetDisplayName(string tableName)
    {
        return NormalizeTableName(tableName) switch
        {
            "bjtge" => "Bipolar Germanio",
            "bjtsi" => "Bipolar Silicio",
            "bjtsidual" => "Bipolar Dual Silicio",
            "bjtprebias" => "Bipolar Pre-polarizado",
            "bjtprebiasdual" => "Bipolar Dual Pre-polarizado",
            "jfet" => "JFET",
            "mosfet" => "MOSFET",
            "mosfetdual" => "MOSFET Dual",
            "igbt" => "IGBT",
            "igbtdual" => "IGBT Dual",
            _ => "Transistor"
        };
    }

    public static string GetByNameType(string tableName)
    {
        return NormalizeTableName(tableName) switch
        {
            "bjtge" => "BJTGe",
            "bjtsi" => "BJTSi",
            "bjtsidual" => "BJTDual",
            "bjtprebias" => "BJTPreBias",
            "bjtprebiasdual" => "PreBiasDual",
            "jfet" => "JFET",
            "mosfet" => "MOSFET",
            "mosfetdual" => "MOSFETDual",
            "igbt" => "IGBT",
            "igbtdual" => "IGBTDual",
            _ => tableName
        };
    }

    public static IReadOnlyList<string> GetByNameTypeAliases(string tableName)
    {
        return NormalizeTableName(tableName) switch
        {
            "bjtsidual" => new[] { "BJTDual", "BJTSiDual" },
            "bjtprebiasdual" => new[] { "PreBiasDual", "BJTPreBiasDual" },
            _ => new[] { GetByNameType(tableName) }
        };
    }

    public static IReadOnlyList<PropertyInfo> GetDisplayProperties(string tableName)
    {
        return GetModelType(tableName).GetProperties()
            .Where(p => p.Name is not ("Id" or "Name" or "StructId" or "CapsIds" or "R1" or "R2"))
            .ToList();
    }

    public static IReadOnlyList<PropertyInfo> GetEditableProperties(string tableName)
    {
        return GetModelType(tableName).GetProperties()
            .Where(p => p.Name is not ("Id" or "Name" or "StructId" or "CapsIds"))
            .ToList();
    }

    public static IReadOnlyList<TransistorColumn> GetImportColumns(string tableName)
    {
        return GetModelType(tableName).GetProperties()
            .Where(p => p.Name is not ("Id" or "CapsIds"))
            .Select(p =>
            {
                var column = p.GetCustomAttribute<ColumnAttribute>()?.Name
                    ?? p.Name.ToLowerInvariant();
                var underlying = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;
                var numeric = underlying == typeof(double) || underlying == typeof(float) ||
                              underlying == typeof(decimal) || underlying == typeof(int) ||
                              underlying == typeof(long) || underlying == typeof(short);
                return new TransistorColumn(
                    p.Name,
                    column,
                    p.PropertyType,
                    numeric,
                    Nullable.GetUnderlyingType(p.PropertyType) != null || !p.PropertyType.IsValueType);
            })
            .ToList();
    }

    public static string GetDisplayNameForProperty(string fieldName)
    {
        return fieldName switch
        {
            "Pc" or "Pd" => "Potencia",
            "Vcb" => "VCB",
            "Vce" => "VCE",
            "Veb" => "VEB",
            "Vds" => "VDS",
            "Vgs" => "VGS",
            "Vgsth" => "VGSTH",
            "Vcesat" => "VCESAT",
            "Veg" => "VEG",
            "Ic" => "IC",
            "CurrentId" => "ID",
            "Tj" => "TJ",
            "Ft" => "Ft",
            "Cc" => "CC",
            "Hfe" => "Hfe",
            "Qg" => "QG",
            "Tr" => "Tr",
            "Cd" => "CD",
            "Rds" => "RDS",
            "R1" => "R1",
            "R2" => "R2",
            _ => fieldName
        };
    }

    public static string GetUnitForProperty(string fieldName)
    {
        return fieldName switch
        {
            "Pc" or "Pd" => "W",
            "Vcb" or "Vce" or "Veb" or "Vds" or "Vgs" or "Vgsth" or "Vcesat" or "Veg" => "V",
            "Ic" or "CurrentId" => "A",
            "Tj" => "°C",
            "Ft" => "MHz",
            "Cc" or "Cd" => "pF",
            "Qg" => "nC",
            "Tr" => "ns",
            "Rds" => "Ω",
            _ => string.Empty
        };
    }
}
