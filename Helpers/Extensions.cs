// Helpers/Extensions.cs
using System.Reflection;

namespace DbTransistorsApp.Helpers
{
    public static class Extensions
    {
        public static string GetPropertyValue<T>(this T obj, string propertyName)
        {
            var prop = typeof(T).GetProperty(propertyName);
            if (prop != null)
            {
                var value = prop.GetValue(obj);
                return value?.ToString() ?? string.Empty;
            }
            return string.Empty;
        }

        public static T GetPropertyValue<T>(this object obj, string propertyName)
        {
            var prop = obj.GetType().GetProperty(propertyName);
            if (prop != null)
            {
                var value = prop.GetValue(obj);
                if (value is T typedValue)
                    return typedValue;
            }
            return default;
        }

        public static bool IsNumericType(this Type type)
        {
            return type == typeof(int) || type == typeof(double) || type == typeof(float) ||
                   type == typeof(decimal) || type == typeof(long) || type == typeof(short) ||
                   type == typeof(byte) || type == typeof(uint) || type == typeof(ulong) ||
                   type == typeof(ushort) || type == typeof(sbyte);
        }
    }
}