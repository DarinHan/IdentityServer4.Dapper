using System;
using System.Linq;
using System.Text;

namespace System.Collections.Generic
{
    public static class IEnumerableExtensions
    {
        public static bool IsEmpty<T>(this IEnumerable<T> ts)
        {
            return ts == null || ts.Count() == 0;
        }
    }
}
