using System;
using System.Linq;
using System.Text;

public static class BuiltinFunctions
{
	static string[][] JamListArrayToLOL(JamList[] values)
	{
		return Array.ConvertAll (values, i => i.Elements.ToArray ());
	}

    public static void Echo(params JamList[] values)
    {
		foreach (var value in values)
		{
			Console.Write(value.ToString());
			Console.Write(" ");
		}
		Console.WriteLine();
    }

	public static JamList InvokeRule (string rulename, params JamList[] values)
	{
		return new JamList(Jam.Interop.InvokeRule(rulename, JamListArrayToLOL(values)));
	}

	public static void MakeActions(string name,string actions,Jam.ActionsFlags flags = Jam.ActionsFlags.None, int maxTargets=0, int maxLines=0)
	{
		Jam.Interop.MakeActions (name, actions, (int)flags, maxTargets, maxLines);
	}
		
    public static JamList MD5(JamList input)
    {
        return new JamList(input.Elements.Select(CalculateMD5Hash).ToArray());
    }

    public static string SwitchTokenFor(JamList input)
    {
        return input.Elements.First();
    }

    static string CalculateMD5Hash(string input)
    {
        byte[] inputBytes = Encoding.ASCII.GetBytes(input);

        byte[] hash = System.Security.Cryptography.MD5.Create().ComputeHash(inputBytes);
        
        var sb = new StringBuilder();

        foreach (byte t in hash)
            sb.Append(t.ToString("x2"));

        return sb.ToString();
    }

	public static JamList Shell (params JamList[] values)
	{
		return InvokeRule(nameof(Shell), values);
	}

	public static JamList Always (params JamList[] values)
	{
		return InvokeRule(nameof(Always), values);
	}

	public static JamList Leaves (params JamList[] values)
	{
		return InvokeRule(nameof(Leaves), values);
	}

	public static JamList MightNotUpdate (params JamList[] values)
	{
		return InvokeRule(nameof(MightNotUpdate), values);
	}

	public static JamList NoCare (params JamList[] values)
	{
		return InvokeRule(nameof(NoCare), values);
	}

	public static JamList NotFile (params JamList[] values)
	{
		return InvokeRule(nameof(NotFile), values);
	}

	public static JamList NoUpdate (params JamList[] values)
	{
		return InvokeRule(nameof(NoUpdate), values);
	}

	public static JamList ScanContents (params JamList[] values)
	{
		return InvokeRule(nameof(ScanContents), values);
	}

	public static JamList Temporary (params JamList[] values)
	{
		return InvokeRule(nameof(Temporary), values);
	}

	public static JamList UseCommandLine (params JamList[] values)
	{
		return InvokeRule(nameof(UseCommandLine), values);
	}

/*	public static JamList Echo (params JamList[] values)
	{
		return InvokeRule(nameof(Echo), values);
	}*/

	public static JamList Exit (params JamList[] values)
	{
		return InvokeRule(nameof(Exit), values);
	}

	public static JamList Glob (params JamList[] values)
	{
		return InvokeRule(nameof(Glob), values);
	}

	public static JamList GroupByVar (params JamList[] values)
	{
		return InvokeRule(nameof(GroupByVar), values);
	}

	public static JamList Match (params JamList[] values)
	{
		return InvokeRule(nameof(Match), values);
	}

	public static JamList Math (params JamList[] values)
	{
		return InvokeRule(nameof(Math), values);
	}

	public static JamList MD5 (params JamList[] values)
	{
		return InvokeRule(nameof(MD5), values);
	}

	public static JamList MD5File (params JamList[] values)
	{
		return InvokeRule(nameof(MD5File), values);
	}

	public static JamList OptionalFileCache (params JamList[] values)
	{
		return InvokeRule(nameof(OptionalFileCache), values);
	}

	public static JamList QueueJamFile (params JamList[] values)
	{
		return InvokeRule(nameof(QueueJamFile), values);
	}

	public static JamList Subst (params JamList[] values)
	{
		return InvokeRule(nameof(Subst), values);
	}

	public static JamList UseDepCache (params JamList[] values)
	{
		return InvokeRule(nameof(UseDepCache), values);
	}

	public static JamList UseFileCache (params JamList[] values)
	{
		return InvokeRule(nameof(UseFileCache), values);
	}

	public static JamList UseMD5Callback (params JamList[] values)
	{
		return InvokeRule(nameof(UseMD5Callback), values);
	}

	public static JamList W32_GETREG (params JamList[] values)
	{
		return InvokeRule(nameof(W32_GETREG), values);
	}

	public static JamList Depends (params JamList[] values)
	{
		return InvokeRule(nameof(Depends), values);
	}

	public static JamList Includes (params JamList[] values)
	{
		return InvokeRule(nameof(Includes), values);
	}

	public static JamList Needs (params JamList[] values)
	{
		return InvokeRule(nameof(Needs), values);
	}

	public static JamList Clean (params JamList[] values)
	{
		return InvokeRule(nameof(Clean), values);
	}

	public static JamList Split (params JamList[] values)
	{
		return InvokeRule(nameof(Split), values);
	}
}