// Models/Base/ByName.cs
using SQLite;

[Table("byname")]
public class ByName
{
    [PrimaryKey, AutoIncrement, Column("_id")]
    public int Id { get; set; }

    [Column("name")]
    public string Name { get; set; }

    [Column("type")]
    public string Type { get; set; }

    [Column("idx")]
    public int Idx { get; set; }
}