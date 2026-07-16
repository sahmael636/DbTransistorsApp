// Models/Base/Encapsulado.cs
using SQLite;
//using System.ComponentModel.DataAnnotations.Schema;
namespace DbTransistorsApp.Models.Base 
{
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
}