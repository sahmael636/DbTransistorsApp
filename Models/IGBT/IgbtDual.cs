// Models/IGBT/Igbt.cs
using SQLite;

namespace DbTransistorsApp.Models.Base
{
    [Table("igbtdual")]
    public class IgbtDual : ITransistor
    {
    [PrimaryKey, AutoIncrement, Column("_id")]
    public int Id { get; set; }

    [Column("name")]
    public string Name { get; set; }

    [Column("pc")]
    public double? Pc { get; set; }

    [Column("vce")]
    public double? Vce { get; set; }

    [Column("vcesat")]
    public double? Vcesat { get; set; }

    [Column("veg")]
    public double? Veg { get; set; }

    [Column("ic")]
    public double? Ic { get; set; }

    [Column("tj")]
    public double? Tj { get; set; }

    [Column("tr")]
    public double? Tr { get; set; }

    [Column("cc")]
    public double? Cc { get; set; }

    [Column("struct_id")]
    public int StructId { get; set; }

        [Ignore]
        public List<int> CapsIds { get; set; } = new();
    }
}
