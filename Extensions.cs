using System;
using System.Collections.Generic;
using System.Linq;
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

		public static IEnumerable<TSource> DistinctBy<TSource, TKey>
		(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
		{
			HashSet<TKey> seenKeys = new HashSet<TKey>();
			foreach (TSource element in source)
			{
				if (seenKeys.Add(keySelector(element)))
				{
					yield return element;
				}
			}
		}

        public static string SeperateWithSpace(this IEnumerable<String> values )
        {
            return SeperateWith(values, " ");
        }

        public static string SeperateWithComma(this IEnumerable<String> values)
        {
            return SeperateWith(values, ",");
        }

        public static IEnumerable<string> InQuotes(this IEnumerable<String> values)
        {
            return values.Select(v => $"\"{v}\"");
        }

        public static string SeperateWith(IEnumerable<string> values, string seperator)
        {
            var result = new StringBuilder();

            bool first = true;
            foreach (var v in values)
            {
                if (!first)
                    result.Append(seperator);
                result.Append(v);
                first = false;
            }
            return result.ToString();
        }

        public static string InQuotes(this string s)
        {
            return $"\"{s}\"";
        }
    }
}