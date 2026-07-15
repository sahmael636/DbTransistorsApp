// Models/Base/Estructura.cs
using System.ComponentModel.DataAnnotations.Schema;

[Table("estructuras")]
public class Estructura
{
    [PrimaryKey, AutoIncrement, Column("id")]
    public int Id { get; set; }

    [Column("nombre")]
    public string Nombre { get; set; }
}
