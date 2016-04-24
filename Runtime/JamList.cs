using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

class JamList
{
    string[] _elements;

    public JamList(params string[] values)
    {
        _elements = values;
    }

    public JamList(params JamList[] values)
    {
        _elements = values.SelectMany(v => v._elements).ToArray();
    }

    public JamList()
    {
        _elements = new string[0];
    }

    public IEnumerable<string> Elements => _elements;

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

    public JamList IndexedBy(JamList indices)
    {
        return new JamList(indices._elements.Select(i => DoIndex(_elements,i)).Where(e => e != null).ToArray());
    }

    private string DoIndex(string[] elements, string indexString)
    {
        int jamIndex = 0;
        if (!int.TryParse(indexString, out jamIndex))
            throw new NotSupportedException("Cannot index by non-integer: " + indexString);


        if (jamIndex < 1 || jamIndex > elements.Length)
            return null;

        //jam list indexing starts counting at 1.
        var csharpIndex = jamIndex - 1;
        
        return elements[csharpIndex];
    }

    public bool JamEquals(JamList other)
    {
        return Enumerable.SequenceEqual(_elements, other._elements);
    }

    public void Append(JamList values)
    {
        _elements = _elements.Concat(values._elements).ToArray();
    }

    public JamList IfEmptyUse(JamList value)
    {
        return _elements.Length > 0 ? this : value;
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