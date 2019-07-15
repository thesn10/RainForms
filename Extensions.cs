using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PluginRainForms
{
    static class Extensions
    {
        public static IEnumerable<Type> GetBaseTypes(this Type type, bool bIncludeInterfaces = false)
        {
            if (type == null)
                yield break;

            for (var nextType = type.BaseType; nextType != null; nextType = nextType.BaseType)
                yield return nextType;

            if (!bIncludeInterfaces)
                yield break;

            foreach (var i in type.GetInterfaces())
                yield return i;
        }
    }
}
