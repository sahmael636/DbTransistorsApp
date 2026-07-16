using SQLite;

namespace DbTransistorsApp.Models.Base
{
    [Table("mosfetdual")]
    public class MosfetDual : ITransistor
    {
    [PrimaryKey, AutoIncrement, Column("_id")]
    public int Id { get; set; }

    [Column("name")]
    public string Name { get; set; }

    [Column("pd")]
    public double? Pd { get; set; }

    [Column("vds")]
    public double? Vds { get; set; }

    [Column("vgs")]
    public double? Vgs { get; set; }

    [Column("vgsth")]
    public double? Vgsth { get; set; }

    //[Column("id")]
    ///public double? Id { get; set; }

    [Column("tj")]
    public double? Tj { get; set; }

    [Column("qg")]
    public double? Qg { get; set; }

    [Column("tr")]
    public double? Tr { get; set; }

    [Column("cd")]
    public double? Cd { get; set; }

    [Column("rds")]
    public double? Rds { get; set; }

    [Column("struct_id")]
    public int StructId { get; set; }

        [Ignore]
        public List<int> CapsIds { get; set; } = new();
    }
}
