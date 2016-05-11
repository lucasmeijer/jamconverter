using System;
using System.Linq;
using System.Text;

public static class BuiltinFunctions
{
	static string[][] JamListArrayToLOL(JamList[] values)
	{
		return Array.ConvertAll (values, i => i.Elements.ToArray ());
	}

	public static JamList Echo(params JamList[] values)
    {
		foreach (var value in values)
		{
			Console.Write(value.ToString());
			Console.Write(" ");
		}
		Console.WriteLine();
		return null;
		//return InvokeRule(nameof(Echo), values);
    }

	public static JamList echo(params JamList[] values)
	{
		return Echo (values);
	}

	public static JamList ECHO(params JamList[] values)
	{
		return Echo (values);
	}

	public static JamList InvokeRule (string rulename, params JamList[] values)
	{
		GlobalVariables.Singleton.SendVariablesToJam ();
		return new JamList(Jam.Interop.InvokeRule(rulename, JamListArrayToLOL(values)));
	}

	public static void MakeActions(string name,string actions,Jam.ActionsFlags flags = Jam.ActionsFlags.None, int maxTargets=0, int maxLines=0)
	{
		Jam.Interop.MakeActions (name, actions, (int)flags, maxTargets, maxLines);
	}

    public static string SwitchTokenFor(JamList input)
    {
        return input.Elements.First();
    }

	public static JamList Always (params JamList[] values)
	{
		return InvokeRule(nameof(Always), values);
	}
	public static JamList ALWAYS (params JamList[] values)
	{
		return InvokeRule(nameof(ALWAYS), values);
	}
	public static JamList Depends (params JamList[] values)
	{
		return InvokeRule(nameof(Depends), values);
	}
	public static JamList DEPENDS (params JamList[] values)
	{
		return InvokeRule(nameof(DEPENDS), values);
	}
	/*public static JamList echo (params JamList[] values)
	{
		return InvokeRule(nameof(echo), values);
	}
	public static JamList Echo (params JamList[] values)
	{
		return InvokeRule(nameof(Echo), values);
	}
	public static JamList ECHO (params JamList[] values)
	{
		return InvokeRule(nameof(ECHO), values);
	}*/
	public static JamList exit (params JamList[] values)
	{
		return InvokeRule(nameof(exit), values);
	}
	public static JamList Exit (params JamList[] values)
	{
		return InvokeRule(nameof(Exit), values);
	}
	public static JamList EXIT (params JamList[] values)
	{
		return InvokeRule(nameof(EXIT), values);
	}
	public static JamList Glob (params JamList[] values)
	{
		return InvokeRule(nameof(Glob), values);
	}
	public static JamList GLOB (params JamList[] values)
	{
		return InvokeRule(nameof(GLOB), values);
	}
	public static JamList Includes (params JamList[] values)
	{
		return InvokeRule(nameof(Includes), values);
	}
	public static JamList INCLUDES (params JamList[] values)
	{
		return InvokeRule(nameof(INCLUDES), values);
	}
	public static JamList Leaves (params JamList[] values)
	{
		return InvokeRule(nameof(Leaves), values);
	}
	public static JamList LEAVES (params JamList[] values)
	{
		return InvokeRule(nameof(LEAVES), values);
	}
	public static JamList Match (params JamList[] values)
	{
		return InvokeRule(nameof(Match), values);
	}
	public static JamList MATCH (params JamList[] values)
	{
		return InvokeRule(nameof(MATCH), values);
	}
	public static JamList ForceCare (params JamList[] values)
	{
		return InvokeRule(nameof(ForceCare), values);
	}
	public static JamList NoCare (params JamList[] values)
	{
		return InvokeRule(nameof(NoCare), values);
	}
	public static JamList NOCARE (params JamList[] values)
	{
		return InvokeRule(nameof(NOCARE), values);
	}
	public static JamList NOTIME (params JamList[] values)
	{
		return InvokeRule(nameof(NOTIME), values);
	}
	public static JamList NotFile (params JamList[] values)
	{
		return InvokeRule(nameof(NotFile), values);
	}
	public static JamList NOTFILE (params JamList[] values)
	{
		return InvokeRule(nameof(NOTFILE), values);
	}
	public static JamList NoUpdate (params JamList[] values)
	{
		return InvokeRule(nameof(NoUpdate), values);
	}
	public static JamList NOUPDATE (params JamList[] values)
	{
		return InvokeRule(nameof(NOUPDATE), values);
	}
	public static JamList Subst (params JamList[] values)
	{
		return InvokeRule(nameof(Subst), values);
	}
	public static JamList SubstLiteralize (params JamList[] values)
	{
		return InvokeRule(nameof(SubstLiteralize), values);
	}
	public static JamList Temporary (params JamList[] values)
	{
		return InvokeRule(nameof(Temporary), values);
	}
	public static JamList TEMPORARY (params JamList[] values)
	{
		return InvokeRule(nameof(TEMPORARY), values);
	}
	public static JamList QueueJamfile (params JamList[] values)
	{
		return InvokeRule(nameof(QueueJamfile), values);
	}
	public static JamList MD5 (params JamList[] values)
	{
		return InvokeRule(nameof(MD5), values);
	}
	public static JamList MD5File (params JamList[] values)
	{
		return InvokeRule(nameof(MD5File), values);
	}
	public static JamList Math (params JamList[] values)
	{
		return InvokeRule(nameof(Math), values);
	}
	public static JamList W32_GETREG (params JamList[] values)
	{
		return InvokeRule(nameof(W32_GETREG), values);
	}
	public static JamList W32_GETREG64 (params JamList[] values)
	{
		return InvokeRule(nameof(W32_GETREG64), values);
	}
	public static JamList W32_SHORTNAME (params JamList[] values)
	{
		return InvokeRule(nameof(W32_SHORTNAME), values);
	}
	public static JamList UseDepCache (params JamList[] values)
	{
		return InvokeRule(nameof(UseDepCache), values);
	}
	public static JamList UseFileCache (params JamList[] values)
	{
		return InvokeRule(nameof(UseFileCache), values);
	}
	public static JamList OptionalFileCache (params JamList[] values)
	{
		return InvokeRule(nameof(OptionalFileCache), values);
	}
	public static JamList UseCommandLine (params JamList[] values)
	{
		return InvokeRule(nameof(UseCommandLine), values);
	}
	public static JamList ScanContents (params JamList[] values)
	{
		return InvokeRule(nameof(ScanContents), values);
	}
	public static JamList SCANCONTENTS (params JamList[] values)
	{
		return InvokeRule(nameof(SCANCONTENTS), values);
	}
	public static JamList MightNotUpdate (params JamList[] values)
	{
		return InvokeRule(nameof(MightNotUpdate), values);
	}
	public static JamList Needs (params JamList[] values)
	{
		return InvokeRule(nameof(Needs), values);
	}
	public static JamList NEEDS (params JamList[] values)
	{
		return InvokeRule(nameof(NEEDS), values);
	}
	public static JamList LuaString (params JamList[] values)
	{
		return InvokeRule(nameof(LuaString), values);
	}
	public static JamList LuaFile (params JamList[] values)
	{
		return InvokeRule(nameof(LuaFile), values);
	}
	public static JamList UseMD5Callback (params JamList[] values)
	{
		return InvokeRule(nameof(UseMD5Callback), values);
	}
	public static JamList Shell (params JamList[] values)
	{
		return InvokeRule(nameof(Shell), values);
	}
	public static JamList GroupByVar (params JamList[] values)
	{
		return InvokeRule(nameof(GroupByVar), values);
	}
	public static JamList Split (params JamList[] values)
	{
		return InvokeRule(nameof(Split), values);
	}
	public static JamList ExpandFileList (params JamList[] values)
	{
		return InvokeRule(nameof(ExpandFileList), values);
	}
	public static JamList ListSort (params JamList[] values)
	{
		return InvokeRule(nameof(ListSort), values);
	}
	public static JamList DependsList (params JamList[] values)
	{
		return InvokeRule(nameof(DependsList), values);
	}
	public static JamList RuleExists (params JamList[] values)
	{
		return InvokeRule(nameof(RuleExists), values);
	}

}