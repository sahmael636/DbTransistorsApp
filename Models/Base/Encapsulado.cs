// Models/Base/Encapsulado.cs
using System.ComponentModel.DataAnnotations.Schema;

[Table("encapsulados")]
public class Encapsulado
{
    [PrimaryKey, AutoIncrement, Column("id")]
    public int Id { get; set; }

    [Column("nombre")]
    public string Nombre { get; set; }

    [Column("imagen")]
    public string Imagen { get; set; }
}