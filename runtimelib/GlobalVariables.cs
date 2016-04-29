using System.Collections.Generic;
using System.Linq;

public class GlobalVariables
{
    private readonly Dictionary<string, JamList> _values = new Dictionary<string, JamList>();

    public JamList Get(string myvar)
    {
        JamList result = null;
        if (_values.TryGetValue(myvar, out result))
            return result;

        result = new JamList();
        _values[myvar] = result;
        return result;
    }

    public JamList this[string variableName]
    {
        get {
            JamList result = null;
            if (_values.TryGetValue(variableName, out result))
                return result;

            result = new JamList();
            _values[variableName] = result;
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
}

