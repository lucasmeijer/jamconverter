using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

class JamList
{
    readonly string[] _elements;

    public JamList(params string[] values)
    {
        _elements = values;
    }

    public JamList(params JamList[] values)
    {
        _elements = values.SelectMany(v => v._elements).ToArray();
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        bool first = true;
        foreach (var e in _elements)
        {
            if (!first)
                sb.Append(" ");
            sb.Append(e);
            first = false;
        }

        return sb.ToString();
    }

    public JamList WithSuffix(JamList value)
    {
        return new JamList(_elements.Select(s => WithSuffix(s,value._elements[0])).ToArray());
    }

    private string WithSuffix(string value, string suffix)
    {
        return System.IO.Path.ChangeExtension(value, suffix);
    }

    public static JamList Combine(params JamList[] values)
    {
        IEnumerable<IEnumerable<string>> a = values.Select(v => v._elements);
        return new JamList(a.CartesianProduct().Select(MakeBigString).ToArray());
    }

    private static string MakeBigString(IEnumerable<string> inputs)
    {
        var sb = new StringBuilder();
        foreach (var s in inputs)
            sb.Append(s);
        return sb.ToString();
    }
}

static internal class Helper
{
    static internal IEnumerable<IEnumerable<string>> CartesianProduct(this IEnumerable<IEnumerable<string>> sequences)
    {
        IEnumerable<IEnumerable<string>> emptyProduct = new[] { Enumerable.Empty<string>() };
        return sequences.Aggregate(
          emptyProduct,
          (accumulator, sequence) =>
            from accseq in accumulator
            from item in sequence
            select accseq.Concat(new[] { item }));
    }
}