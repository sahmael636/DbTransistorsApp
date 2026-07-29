namespace DbTransistorsApp.Services;

public sealed record DatabaseTableData(
    string Name,
    IReadOnlyList<string> Columns,
    IReadOnlyList<IReadOnlyList<object?>> Rows);
