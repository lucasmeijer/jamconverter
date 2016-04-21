using System.Collections.Generic;

namespace jamconverter
{
    internal static class Extensions
    {
        public static IEnumerable<T> Prepend<T>(this IEnumerable<T> collection, T element)
        {
            yield return element;
            foreach (var e in collection)
                yield return e;
        }
    }
}