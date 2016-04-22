using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Markup;

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

        public static string SeperateWithSpace(this IEnumerable<String> values )
        {
            var result = new StringBuilder();

            bool first = true;
            foreach (var v in values)
            {
                if (!first)
                    result.Append(" ");
                result.Append(v);
                first = false;
            }
            return result.ToString();
        }
    }
}