using System;
using System.Linq;

namespace DbTransistorsApp.ViewModels
{
    public static class ColumnLayoutHelper
    {
        private static int? _maxParameterCount;

        public static int MaxParameterCount
        {
            get
            {
                if (_maxParameterCount.HasValue) return _maxParameterCount.Value;

                int max = 0;
                foreach (TableType t in Enum.GetValues(typeof(TableType)))
                {
                    var model = t.GetModelType();
                    var props = model.GetProperties()
                        .Where(p => p.Name != "Id" && p.Name != "Name" && p.Name != "StructId" && p.Name != "CapsIds" && p.Name != "R1" && p.Name != "R2")                        
                        .Count();
                    if (props > max) max = props;
                }

                _maxParameterCount = max;
                return _maxParameterCount.Value;
            }
        }

        public static int MaxTotalColumns => 1 + MaxParameterCount; // 1 for Name + parameters
    }
}
