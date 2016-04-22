using System.Collections.Generic;
using System.Text;

class JamList
{
    public List<string> _elements = new List<string>();

    public JamList(string value)
    {
        _elements.Add(value);
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
}

