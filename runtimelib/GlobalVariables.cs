using System;
using System.Collections.Generic;
using System.Linq;
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
		if (targetName.Elements.Count() != 1)
			throw new ArgumentException("on statement being invoked on multiple targets. you couldn't even do this in jam!");

		return new TemporaryTargetContext(targetName.Elements.Single());
	}

	private class TemporaryTargetContext : IDisposable
	{
	    private readonly string _target;

	    public TemporaryTargetContext(string target)
	    {
	        _target = target;
	        Jam.Interop.PushSettingsFor(_target);
	    }

	    public void Dispose()
	    {
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

