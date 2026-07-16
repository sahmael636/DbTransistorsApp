// Models/Base/ITransistor.cs
using System.Collections.Generic;

namespace DbTransistorsApp.Models.Base
{
    public interface ITransistor
    {
        int Id { get; set; }
        string Name { get; set; }
        int StructId { get; set; }
        List<int> CapsIds { get; set; }
    }
}
