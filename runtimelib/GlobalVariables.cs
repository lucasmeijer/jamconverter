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
	        Variables variables = _currentOnContext ?? _values;
            JamList result = null;
            if (variables.TryGetValue(variableName, out result))
                return result;

            result = new JamList();
            variables[variableName] = result;
            return result;
        }
        set
        {
            _values[variableName] = value;
        }
    }

    public JamList this[JamList variable]
    {
        get { return this[variable.Elements.First()]; }
        set { this[variable.Elements.First()] = value; }
    }

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

	public JamList GetOrCreateVariableOnTargetContext(string target, string variable)
	{
		var variables = VariablesFor(target);
		
		JamList result;
		if (variables.TryGetValue(variable, out result))
			return result;

		var r = new JamList();
		variables[variable] = r;
		return r;
	}

	public IDisposable OnTargetContext(string targetName)
	{
		if (_currentOnContext != null)
			throw new NotSupportedException("Nesting target contexts");

		_onTargetVariables.TryGetValue(targetName, out _currentOnContext);
		return new TemporaryTargetContext(this);
	}

	private class TemporaryTargetContext : IDisposable
	{
		private GlobalVariables _owner;

		public TemporaryTargetContext(GlobalVariables owner)
		{
			_owner = owner;
		}

		public void Dispose()
		{
			_owner._currentOnContext = null;
		}
	}
}

