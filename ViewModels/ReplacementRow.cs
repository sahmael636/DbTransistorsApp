using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DbTransistorsApp.ViewModels
{
    public class ReplacementRow
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public ObservableCollection<string> Values { get; set; } = new();
        public object Original { get; set; }
    }
}
