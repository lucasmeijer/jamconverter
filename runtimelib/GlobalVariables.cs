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

	/*
    public LocalJamList this[LocalJamList variable]
    {
        get { return this[variable.Elements.First()]; }
        set { this[variable.Elements.First()] = value; }
    }*/

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

	public IEnumerable<LocalJamList> GetOrCreateVariableOnTargetContext(LocalJamList targetNames, LocalJamList variableNames)
	{
		foreach (var targetName in targetNames)
		{
			var variables = VariablesFor(targetName);

			foreach (var variable in variableNames.Elements)
			{
				LocalJamList result;
				if (variables.TryGetValue(variable, out result))
				{
					yield return result;
					continue;
				}

				var r = new LocalJamList();
				variables[variable] = r;
				yield return r;
			}
		}
	}

	public IDisposable OnTargetContext(LocalJamList targetName)
	{
		if (_currentOnContext != null)
			throw new NotSupportedException("Nesting target contexts");

		if (targetName.Elements.Count() != 1)
			throw new ArgumentException("on statement being invoked on multiple targets. you couldn't even do this in jam!");

		_onTargetVariables.TryGetValue(targetName.Elements.Single(), out _currentOnContext);
		return new TemporaryTargetContext(this);
	}

	private class TemporaryTargetContext : IDisposable
	{
		private readonly GlobalVariables _owner;

		public TemporaryTargetContext(GlobalVariables owner)
		{
			_owner = owner;
		}

		public void Dispose()
		{
			_owner._currentOnContext = null;
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

	public void SendVariablesToJam()
	{
#if EMBEDDED_MODE
		foreach (var targetVars in _onTargetVariables) 
		{
			foreach (var targetVar in targetVars.Value)
			{
				Jam.Interop.Setting (targetVar.Key, new[]{ targetVars.Key }, targetVar.Value.Elements.ToArray ());
			}
		}
#endif
	}
}

