namespace DbTransistorsApp.Services;

public enum ImportIssueKind
{
    Duplicate,
    Validation,
    Database
}

public sealed record ImportIssue(
    int RowNumber,
    string Name,
    ImportIssueKind Kind,
    string Message);

public sealed class ImportResult
{
    public int ProcessedRows { get; set; }
    public int ImportedRows { get; set; }
    public int DuplicateRows { get; set; }
    public int ErrorRows { get; set; }
    public List<ImportIssue> Issues { get; } = new();
}
