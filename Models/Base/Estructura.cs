// Models/Base/Estructura.cs
using SQLite;

[Table("estructuras")]
public class Estructura
{
    [PrimaryKey, AutoIncrement, Column("id")]
    public int Id { get; set; }

    [Column("nombre")]
    public string Nombre { get; set; }
}
