using System;
using System.Collections.Generic;
using System.Linq;

namespace PentaWork.Tools.PowerPlatform.XrmProxies
{
    public static class LinqExtensions
    {
        public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            var seenKeys = new HashSet<TKey>();
            foreach (var element in source)
            {
                if (seenKeys.Add(keySelector(element)))
                {
                    yield return element;
                }
            }
        }

        public static void AddDistinct<TSource>(this IList<TSource> source, Func<TSource, object> keySelector, TSource value)
        {
            var lookup = source.ToLookup(keySelector);
            if (!lookup.Contains(keySelector(value)))
            {
                source.Add(value);
            }
        }
    }
}
