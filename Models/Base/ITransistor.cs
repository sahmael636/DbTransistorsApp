// Models/Base/ITransistor.cs
public interface ITransistor
{
    int Id { get; set; }
    string Name { get; set; }
    int StructId { get; set; }
    List<int> CapsIds { get; set; }
}
