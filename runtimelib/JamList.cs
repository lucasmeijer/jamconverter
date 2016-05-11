using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

public class JamList : IEnumerable<string>
{
    string[] _elements;

    public JamList(params string[] values)
    {
        _elements = values;
    }

    public JamList(params JamList[] values)
    {
	    _elements = ElementsOf(values);
    }

    public JamList()
    {
        _elements = new string[0];
    }

	public JamList(IEnumerable<JamList> values)
	{
		_elements = ElementsOf(values.ToArray());
	}

	public IEnumerable<string> Elements => _elements;

	public IEnumerable<JamList> ElementsAsJamLists
	{
		get { return _elements.Select(e => new JamList(e)); }
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
		var result = new JamList();
		foreach (var index in indices) {
			var indexJamList = DoIndex (_elements, index);
			if (indexJamList != null)
				result._elements = result._elements.Concat (indexJamList).ToArray ();
		}
		
		return result;
	}

	private string[] DoIndex(string[] elements, string indexString)
    {
		int lowerBound = 1;
		var higherBound = elements.Length;
		var dashIndex = indexString.IndexOf("-");
		var nsException = new NotSupportedException ("Cannot index by non-integer: " + indexString);
		if (dashIndex == -1) {
			if (!int.TryParse (indexString, out higherBound))
				throw nsException;
			lowerBound = higherBound;
		} else if (dashIndex == 0) {
			
			if (!int.TryParse (indexString.Substring (1), out higherBound))
				throw nsException;
		} else if (dashIndex == indexString.Length - 1) {
			if (!int.TryParse (indexString.Substring (0, indexString.Length - 1), out lowerBound))
				throw nsException;
		} else {
			var split = indexString.Split(new[]{'-'});
			if (!int.TryParse (split[0], out lowerBound))
				throw nsException;
			if (!int.TryParse (split[1], out higherBound))
				throw nsException;
		}

		if (lowerBound > elements.Length)
			return null;

		if (higherBound < lowerBound)
			higherBound = lowerBound;
		if (lowerBound < 1)
			lowerBound = 1;
		if (higherBound < 1)
			higherBound = 1;
		if (higherBound > elements.Length)
			higherBound = elements.Length;
		
        //jam list indexing starts counting at 1.       
		return elements.Skip(lowerBound-1).Take(higherBound-lowerBound+1).ToArray();
    }

    public bool JamEquals(JamList other)
    {
        return _elements.SequenceEqual(other._elements);
    }

    public void Append(params JamList[] values)
    {
        _elements = _elements.Concat(values.SelectMany(v=>v.Elements)).ToArray();
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

	public JamList ToUpper(JamList value)
	{
		return new JamList(_elements.Select(e => e.ToUpperInvariant()).ToArray());
	}

	public JamList ToLower(JamList value)
	{
		return new JamList(_elements.Select(e => e.ToLowerInvariant()).ToArray());
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

    public void Subtract(params JamList[] values)
    {
		_elements = _elements.Where(e => !ElementsOf(values).Contains(e)).ToArray();
    }

    public IEnumerator<string> GetEnumerator()
    {
        return new JamListEnumerator(this);
    }

    public class JamListEnumerator : IEnumerator<string>
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

        public string Current => _jamList._elements[_index];

	    object IEnumerator.Current => Current;
    }

    public bool IsIn(params JamList[] values) => Elements.All(e => values.SelectMany(l=>l._elements).Contains(e));

	public static implicit operator JamList(string input)
	{
		return new JamList(input);
	}

	public JamList Clone()
	{
		return new JamList(_elements);
	}

	public JamList Include(JamList pattern)
	{
		if (pattern.Elements.Count() != 1)
			throw new ArgumentException();

		var patternStr = pattern.Elements.Single();
		
		var strings = Elements.Where(e=>Regex.Matches(e, patternStr).Count > 0).ToArray();
		return new JamList(strings);
	}

	public void AssignIfEmpty(params JamList[] values)
	{
		if (Elements.Any())
			return;
		Assign(values);
	}

	public void Assign(params JamList[] values)
	{
		_elements = ElementsOf(values);
	}

	static string[] ElementsOf(JamList[] values)
	{
		return values.SelectMany(v => v.Elements).ToArray();
	}

	public bool And(JamList value)
	{
		return AsBool() && value.AsBool();
	}

	public bool Or(JamList value)
	{
		return AsBool() || value.AsBool();
	}

	public bool NotJamEquals(JamList value)
	{
		return !JamEquals(value);
	}
}

public static class JamListExtensions
{
	public static void Assign(this IEnumerable<JamList> jamlists, params JamList[] values)
	{
		foreach(var jamlist in jamlists)
			jamlist.Assign(values);
	}

	public static void Append(this IEnumerable<JamList> jamlists, params JamList[] values)
	{
		foreach (var jamlist in jamlists)
			jamlist.Append(values);
	}

	public static void AssignIfEmpty(this IEnumerable<JamList> jamlists, params JamList[] values)
	{
		foreach (var jamlist in jamlists)
			jamlist.AssignIfEmpty(values);
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