using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class JamList : IEnumerable<JamList>
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

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public JamList WithSuffix(JamList value)
    {
        return new JamList(_elements.Select(s =>
        {
            var suffix = value._elements.Length > 0 ? value._elements[0] : "";
            return WithSuffix(s, suffix);
        }).ToArray());
    }

    private string WithSuffix(string value, string suffix)
    {
        if (suffix.Length == 0)
        {
            var lastIndexOf = value.LastIndexOf(".");
            if (lastIndexOf<0)
                return value;
            return value.Substring(0, lastIndexOf);
        }

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
        return _elements.SequenceEqual(other._elements);
    }

    public void Append(JamList values)
    {
        _elements = _elements.Concat(values._elements).ToArray();
    }

    public JamList IfEmptyUse(JamList value)
    {
        return _elements.Length > 0 ? this : value;
    }

    public JamList GristWith(JamList value)
    {
        if (value._elements.Length>0)
            return new JamList(Elements.Select(e => $"<{value._elements[0].Trim('<','>')}>{UnGrist(e)}").ToArray());
        return new JamList(_elements.Select(UnGrist).ToArray());
    }

    private string UnGrist(string s)
    {
        if (!s.StartsWith("<"))
            return s;
        return s.Substring(s.IndexOf('>')+1);
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

    public JamList JoinWithValue(JamList joinValue)
    {
        if (joinValue._elements.Length != 1)
            throw new NotSupportedException();

        return new JamList(string.Join(joinValue._elements[0], _elements));
    }

    public JamList With(params string[] extra)
    {
        return new JamList {_elements = _elements.Concat(extra).ToArray()};
    }

    public JamList With(JamList extra)
    {
        return new JamList { _elements = _elements.Concat(extra._elements).ToArray() };
    }

    public bool AsBool()
    {
        return _elements.Length > 0;
    }

    public void Subtract(JamList values)
    {
        _elements = _elements.Where(e => !values.Elements.Contains(e)).ToArray();
    }

    public IEnumerator<JamList> GetEnumerator()
    {
        return new JamListEnumerator(this);
    }

    public class JamListEnumerator : IEnumerator<JamList>
    {
        private readonly JamList _jamList;
        private int _index = -1;

        public JamListEnumerator(JamList jamList)
        {
            _jamList = jamList;
        }

        public void Dispose()
        {
        }

        public bool MoveNext()
        {
            _index++;
            return _index < _jamList._elements.Length;
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }

        public JamList Current { get { return new JamList(_jamList._elements[_index]);} }

        object IEnumerator.Current => Current;
    }
}

//stolen from eric lippert
internal static class Helper
{
    internal static IEnumerable<IEnumerable<string>> CartesianProduct(this IEnumerable<IEnumerable<string>> sequences)
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