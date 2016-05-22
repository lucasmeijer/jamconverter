using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;

public class LocalJamList : JamListBase
{
	public LocalJamList(params string[] values)
    {
		_elements = values;
    }

    public LocalJamList(params JamListBase[] values)
    {
		_elements = ElementsOf(values);
    }

    public LocalJamList()
    {
		_elements = new string[0];
    }

	public LocalJamList(IEnumerable<JamListBase> values)
	{
		_elements = ElementsOf(values.ToArray());
	}

	public override string[] Elements => _elements;

	public override void Append(params JamListBase[] values)
    {
        _elements = Elements.Concat(values.SelectMany(v=>v.Elements)).ToArray();
    }

	public override void Subtract(params JamListBase[] values)
    {
		_elements = Elements.Where(e => !ElementsOf(values).Contains(e)).ToArray();
    }

	public static implicit operator LocalJamList(string input)
	{
		return new LocalJamList(input);
	}

	public override void AssignIfEmpty(params JamListBase[] values)
	{
		if (Elements.Any())
			return;
		Assign(values);
	}

	public override void Assign(params JamListBase[] values)
	{
		_elements = ElementsOf(values);
	}
}

public static class JamListExtensions
{
	public static void Assign(this IEnumerable<JamListBase> jamlists, params JamListBase[] values)
	{
		foreach(var jamlist in jamlists)
			jamlist.Assign(values);
	}

	public static void Append(this IEnumerable<JamListBase> jamlists, params JamListBase[] values)
	{
		foreach (var jamlist in jamlists)
			jamlist.Append(values);
	}

    public static void Subtract(this IEnumerable<JamListBase> jamlists, params JamListBase[] values)
    {
        foreach (var jamlist in jamlists)
            jamlist.Subtract(values);
    }

    public static void AssignIfEmpty(this IEnumerable<JamListBase> jamlists, params JamListBase[] values)
	{
		foreach (var jamlist in jamlists)
			jamlist.AssignIfEmpty(values);
	}

	public static JamListBase Clone(this JamListBase list)
	{
		if (list == null)
			return new LocalJamList();

		return new LocalJamList(list.Elements.ToArray());
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
