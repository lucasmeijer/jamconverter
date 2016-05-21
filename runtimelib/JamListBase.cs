using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

public abstract class JamListBase : IEnumerable<string>
{
	protected string[] _elements;

	public IEnumerable<LocalJamList> ElementsAsJamLists
	{
		get { return Enumerable.Select<string, LocalJamList>(Elements, e => new LocalJamList(e)); }
	}

	public abstract string[] Elements { get; }

	public override string ToString()
	{
		var sb = new StringBuilder();
		bool first = true;
		foreach (var e in Elements)
		{
			if (!first)
				sb.Append(" ");
			sb.Append((string) e);
			first = false;
		}

		return sb.ToString();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	public LocalJamList WithSuffix(JamListBase value)
	{
        GlobalVariables.Singleton["internal_temp1"].Assign(this);

        if (!value.Elements.Any())
            return new LocalJamList(Jam.Interop.Expand("$(internal_temp1:S=)"));

        GlobalVariables.Singleton["internal_temp2"].Assign(value);
        return new LocalJamList(Jam.Interop.Expand("$(internal_temp1:S=$(internal_temp2))"));//"$(internal_temp2))"));
    }

    public LocalJamList GetSuffix()
    {
        GlobalVariables.Singleton["internal_temp1"].Assign(this);
        return new LocalJamList(Jam.Interop.Expand("$(internal_temp1:S)"));
    }

    public LocalJamList IndexedBy(JamListBase indices)
	{
		var result = new List<string>();
		foreach (var index in indices) {
			var indexJamList = DoIndex (Elements, index);
			if (indexJamList != null)
				result.AddRange(indexJamList);
		}
		
		return new LocalJamList(result.ToArray());
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

	public bool JamEquals(JamListBase other)
	{
		return Enumerable.SequenceEqual(Elements, other.Elements);
	}

	public abstract void Append(params JamListBase[] values);

	public JamListBase IfEmptyUse(JamListBase value)
	{
		return Elements.Length > 0 ? this : value;
	}

	public LocalJamList GristWith(JamListBase value)
	{
		if (value.Elements.Length>0)
			return new LocalJamList(Enumerable.Select<string, string>(Elements, e => $"<{value.Elements[0].Trim('<','>')}>{UnGrist(e)}").ToArray());
		return new LocalJamList(Enumerable.ToArray<string>(Elements.Select(UnGrist)));
	}

	public LocalJamList ToUpper()
	{
		return new LocalJamList(Enumerable.Select<string, string>(Elements, e => e.ToUpperInvariant()).ToArray());
	}

	public LocalJamList ToLower()
	{
		return new LocalJamList(Enumerable.Select<string, string>(Elements, e => e.ToLowerInvariant()).ToArray());
	}

	private string UnGrist(string s)
	{
		if (!s.StartsWith("<"))
			return s;
		return s.Substring(s.IndexOf('>')+1);
	}

	public static LocalJamList Combine(params JamListBase[] values)
	{
		IEnumerable<IEnumerable<string>> a = values.Select(v => v.Elements);
		return new LocalJamList(a.CartesianProduct().Select(MakeBigString).ToArray());
	}

	private static string MakeBigString(IEnumerable<string> inputs)
	{
		var sb = new StringBuilder();
		foreach (var s in inputs)
			sb.Append(s);
		return sb.ToString();
	}

	public LocalJamList JoinWithValue(JamListBase joinValue)
	{
	    return InvokeInternalModifier('J', joinValue);
	}

    private LocalJamList InvokeInternalModifier(char modifierLetter, JamListBase argument = null)
    {
        GlobalVariables.Singleton["internal_temp1"].Assign(this);
        if (argument == null)
            return new LocalJamList(Jam.Interop.Expand($"$(internal_temp1:{modifierLetter})"));

        if (!argument.Elements.Any())
            return new LocalJamList(Jam.Interop.Expand($"$(internal_temp1:{modifierLetter}=)"));

        GlobalVariables.Singleton["internal_temp2"].Assign(argument);
        return new LocalJamList(Jam.Interop.Expand($"$(internal_temp1:{modifierLetter}=$(internal_temp2))"));
    }

    public LocalJamList Join()
    {
        return InvokeInternalModifier('J');
    }

    public bool AsBool()
	{
		return Elements.Length > 0;
	}

	public abstract void Subtract(params JamListBase[] localJamListBases);

	public IEnumerator<string> GetEnumerator()
	{
		return new JamListEnumerator(this);
	}

	public class JamListEnumerator : IEnumerator<string>
	{
		private readonly JamListBase _jamList;
		private int _index = -1;

		public JamListEnumerator(JamListBase jamList)
		{
			_jamList = jamList;
		}

		public void Dispose()
		{
		}

		public bool MoveNext()
		{
			_index++;
			return _index < _jamList.Elements.Length;
		}

		public void Reset()
		{
			throw new NotImplementedException();
		}

		public string Current => _jamList.Elements[_index];

		object IEnumerator.Current => Current;
	}

	public bool IsIn(params JamListBase[] values) => Enumerable.All<string>(Elements, e => values.SelectMany(l=>l.Elements).Contains(e));

	public LocalJamList Include(JamListBase pattern)
	{
        return InvokeInternalModifier('I', pattern);
    }

    public LocalJamList Exclude(JamListBase pattern)
    {
        return InvokeInternalModifier('X', pattern);
    }

    public abstract void AssignIfEmpty(params JamListBase[] values);
	public abstract void Assign(params JamListBase[] values);

	protected static string[] ElementsOf(JamListBase[] values)
	{
		return values.SelectMany(v => v.Elements).ToArray();
	}

	public bool And(JamListBase value)
	{
		return AsBool() && value.AsBool();
	}

	public bool And(bool value)
	{
		return AsBool() && value;
	}

	public bool Or(JamListBase value)
	{
		return AsBool() || value.AsBool();
	}

	public bool Or(bool value)
	{
		return AsBool() || value;
	}

	public LocalJamList PModifier_TODO(params JamListBase[] value)
	{
		throw new NotImplementedException();
	}

	public LocalJamList DirectoryModifier(params JamListBase[] values)
	{
		var result = new List<string>();
		foreach (var element in Elements)
		{
			result.AddRange(Jam.Interop.Expand($"@({element}:D)"));
		}
		
		return new LocalJamList(result.ToArray());
	}

	public LocalJamList Rooted_TODO(params JamListBase[] value)
	{
		throw new NotImplementedException();
	}

    public LocalJamList SetBasePath(JamListBase value)
    {
        return InvokeInternalModifier('B', value);
    }

    public LocalJamList GetBasePath()
    {
        return InvokeInternalModifier('B', null);
    }

    public LocalJamList InterpetAsJamVariable()
    {
        return InvokeInternalModifier('A', null);
    }

    public LocalJamList JamGlob()
    {
        return InvokeInternalModifier('W', null);
    }

    public LocalJamList JamGlob(JamListBase value)
    {
        return InvokeInternalModifier('W', value);
    }

    public LocalJamList GetBoundPath()
    {
        return InvokeInternalModifier('T');
    }
    
    public bool NotJamEquals(JamListBase value)
	{
		return !JamEquals(value);
	}

	public bool GreaterThan(JamListBase right)
	{
		int leftInt = int.Parse(Enumerable.Single<string>(Elements));
		int rightInt = int.Parse(right.Elements.Single());
		return leftInt > rightInt;
	}

	public bool LessThan(JamListBase right)
	{
		int leftInt = int.Parse(Enumerable.Single<string>(Elements));
		int rightInt = int.Parse(right.Elements.Single());
		return leftInt < rightInt;
	}
	
	public static implicit operator JamListBase(string input)
	{
		return new LocalJamList(input);
	}

    public static implicit operator bool(JamListBase x)
    {
        return x.AsBool();
    }
}