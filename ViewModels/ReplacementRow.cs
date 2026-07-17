using System.Collections.Generic;

namespace DbTransistorsApp.ViewModels
{
    public class ReplacementRow
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<string> Values { get; set; } = new();
        public object Original { get; set; }
    }
}
