using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using runtimelib;
using Variables = System.Collections.Generic.Dictionary<string, LocalJamList>;

public class GlobalVariables
{
	private Variables _currentOnContext;
	private Dictionary<string, Variables> _onTargetVariables = new Dictionary<string, Variables>();

	public static GlobalVariables Singleton = null;

	public GlobalVariables()
	{
		Singleton = this;
	}

    public JamListBase this[string variableName] => new RemoteJamList(variableName);

	private Variables VariablesFor(string targetName)
	{
		Variables variables;
		if (!_onTargetVariables.TryGetValue(targetName, out variables))
		{
			variables = new Variables();
			_onTargetVariables[targetName] = variables;
		}
		return variables;
	}

	public IEnumerable<JamListBase> GetOrCreateVariableOnTargetContext(JamListBase targetNames, JamListBase variableNames)
	{
		foreach (var targetName in targetNames.Elements)
		{
            foreach(var variableName in variableNames.Elements)
                yield return new RemoteJamList(variableName, targetName);
        }
	}

	public IDisposable OnTargetContext(JamListBase targetName)
	{
	    var count = targetName.Elements.Count();
	    if (count == 0)
	        return new TemporaryTargetContext();

        
	    if (count > 1)
	    {
	        var sb = new StringBuilder("Warning, you are creating an OnTargetContext with multiple targets, which does not do what you expect. everything past the first target is ignored. values are: ");
	        foreach (var e in targetName.Elements)
	            sb.AppendLine(e);
            Console.WriteLine(sb);
	    }

	    return new TemporaryTargetContext(targetName.Elements.First());
	}

	private class TemporaryTargetContext : IDisposable
	{
	    private readonly string _target;

        public TemporaryTargetContext()
        {
        }

        public TemporaryTargetContext(string target)
	    {
	        _target = target;
	        Jam.Interop.PushSettingsFor(_target);
	    }

	    public void Dispose()
	    {
            if (_target != null)
    	        Jam.Interop.PopSettingsFor(_target);
	    }
	}

	public RemoteJamList[] DereferenceElementsNonFlat(JamListBase variableNames)
	{
		return variableNames.Elements.Select(e => new RemoteJamList(e)).ToArray();
	}

	public LocalJamList DereferenceElements(JamListBase variableNames)
	{
		return new LocalJamList(variableNames.Elements.SelectMany(v => this[v]).ToArray());
	}
}

