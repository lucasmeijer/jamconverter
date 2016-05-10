using System;
using System.Collections.Generic;
using System.Linq;
using Variables = System.Collections.Generic.Dictionary<string, JamList>;

public class GlobalVariables
{
	private readonly Variables _values = new Variables();

	private Variables _currentOnContext;
	private Dictionary<string, Variables> _onTargetVariables = new Dictionary<string, Variables>();

    public JamList this[string variableName]
    {
        get
        {
	        if (_currentOnContext != null)
	        {
		        JamList result = null;
		        if (_currentOnContext.TryGetValue(variableName, out result))
			        return result;
	        }

	        {
		        JamList result = null;
		        if (_values.TryGetValue(variableName, out result))
			        return result;
	        }

	        {
				JamList result;
				if (Jam.Interop.Enabled)
					result = new JamList (Jam.Interop.GetVar (variableName));
				else
					result = new JamList ();
				_values[variableName] = result;
				return result;
			}
        }
        set
        {
            _values[variableName] = value;
        }
    }

	/*
    public JamList this[JamList variable]
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

	public IEnumerable<JamList> GetOrCreateVariableOnTargetContext(JamList targetNames, JamList variableNames)
	{
		foreach (var targetName in targetNames)
		{
			var variables = VariablesFor(targetName);

			foreach (var variable in variableNames.Elements)
			{
				JamList result;
				if (variables.TryGetValue(variable, out result))
				{
					yield return result;
					continue;
				}

				var r = new JamList();
				variables[variable] = r;
				yield return r;
			}
		}
	}

	public IDisposable OnTargetContext(JamList targetName)
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

	public JamList[] DereferenceElementsNonFlat(JamList variableNames)
	{
		return variableNames.Elements.Select(e => this[e]).ToArray();
	}

	public JamList DereferenceElements(JamList variableNames)
	{
		return new JamList(variableNames.Elements.SelectMany(v=>this[v]).ToArray());
	}

	public void SendVariablesToJam()
	{
		foreach (var targetVars in _onTargetVariables) 
		{
			foreach (var targetVar in targetVars.Value)
			{
				Jam.Interop.Setting (targetVar.Key, new[]{ targetVars.Key }, targetVar.Value.Elements.ToArray ());
			}
		}
	}
}

