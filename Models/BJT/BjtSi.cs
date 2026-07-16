// Models/BJT/BjtSi.cs
using SQLite;

[Table("bjtsi")]
public class BjtSi : ITransistor
{
    [PrimaryKey, AutoIncrement, Column("_id")]
    public int Id { get; set; }

    [Column("name")]
    public string Name { get; set; }

    [Column("pc")]
    public double? Pc { get; set; }

    [Column("vcb")]
    public double? Vcb { get; set; }

    [Column("vce")]
    public double? Vce { get; set; }

    [Column("veb")]
    public double? Veb { get; set; }

    [Column("ic")]
    public double? Ic { get; set; }

    [Column("tj")]
    public double? Tj { get; set; }

    [Column("ft")]
    public double? Ft { get; set; }

    [Column("cc")]
    public double? Cc { get; set; }

    [Column("hfe")]
    public double? Hfe { get; set; }

    [Column("struct_id")]
    public int StructId { get; set; }

    [Ignore]
    public List<int> CapsIds { get; set; } = new();
}