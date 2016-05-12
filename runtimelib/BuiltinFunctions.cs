using System;
using System.Linq;
using System.Text;
using System.Reflection;

public static class BuiltinFunctions
{
	public static void RegisterJamFiles(params string[] paths)
	{
#if EMBEDDED_MODE
		// Nothing to do.
#else
		////TODO
#endif
	}

	static string[][] JamListArrayToLOL(JamListBase[] values)
	{
		return Array.ConvertAll (values, i => i.Elements.ToArray ());
	}

	public static LocalJamList Echo(params JamListBase[] values)
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

	public static LocalJamList echo(params JamListBase[] values)
	{
		return Echo (values);
	}

	public static LocalJamList ECHO(params JamListBase[] values)
	{
		return Echo (values);
	}

	public static LocalJamList InvokeRule (string rulename, params JamListBase[] values)
	{
#if EMBEDDED_MODE
		GlobalVariables.Singleton.SendVariablesToJam ();
		return new LocalJamList(Jam.Interop.InvokeRule(rulename, JamListArrayToLOL(values)));
#else
		throw new NotImplementedException();
#endif
	}

	public static void MakeActions(string name,string actions,Jam.ActionsFlags flags = Jam.ActionsFlags.None, int maxTargets=0, int maxLines=0)
	{
#if EMBEDDED_MODE
		Jam.Interop.MakeActions (name, actions, (int)flags, maxTargets, maxLines);
#else
		throw new NotImplementedException();
#endif
	}

	public static void RegisterRule(string rulename, MethodInfo callback)
	{
		System.Func<string[][], string[]> d = jamLists => 
		{
			var targetArguments = new object[callback.GetParameters().Length];
			for (int i=0; i!=targetArguments.Length; i++)
			{
				targetArguments[i] = jamLists.Length > i ? new LocalJamList(jamLists[i]) : new LocalJamList();
			}
			object result = callback.Invoke(null,targetArguments);
			if (result == null)
				return new string[0];
			return ((LocalJamList)result).Elements.ToArray();
		};
		#if EMBEDDED_MODE
		Jam.Interop.RegisterRule(rulename, d);
		#endif
	}

    public static string SwitchTokenFor(JamListBase input)
    {
        return input.Elements.First();
    }

	public static LocalJamList Always (params JamListBase[] values)
	{
		return InvokeRule(nameof(Always), values);
	}
	public static LocalJamList ALWAYS (params JamListBase[] values)
	{
		return InvokeRule(nameof(ALWAYS), values);
	}
	public static LocalJamList Depends (params JamListBase[] values)
	{
		return InvokeRule(nameof(Depends), values);
	}
	public static LocalJamList DEPENDS (params JamListBase[] values)
	{
		return InvokeRule(nameof(DEPENDS), values);
	}
	/*public static LocalJamList echo (params JamListBase[] values)
	{
		return InvokeRule(nameof(echo), values);
	}
	public static LocalJamList Echo (params JamListBase[] values)
	{
		return InvokeRule(nameof(Echo), values);
	}
	public static LocalJamList ECHO (params JamListBase[] values)
	{
		return InvokeRule(nameof(ECHO), values);
	}*/
	public static LocalJamList exit (params JamListBase[] values)
	{
		return InvokeRule(nameof(exit), values);
	}
	public static LocalJamList Exit (params JamListBase[] values)
	{
		return InvokeRule(nameof(Exit), values);
	}
	public static LocalJamList EXIT (params JamListBase[] values)
	{
		return InvokeRule(nameof(EXIT), values);
	}
	public static LocalJamList Glob (params JamListBase[] values)
	{
		return InvokeRule(nameof(Glob), values);
	}
	public static LocalJamList GLOB (params JamListBase[] values)
	{
		return InvokeRule(nameof(GLOB), values);
	}
	public static LocalJamList Includes (params JamListBase[] values)
	{
		return InvokeRule(nameof(Includes), values);
	}
	public static LocalJamList INCLUDES (params JamListBase[] values)
	{
		return InvokeRule(nameof(INCLUDES), values);
	}
	public static LocalJamList Leaves (params JamListBase[] values)
	{
		return InvokeRule(nameof(Leaves), values);
	}
	public static LocalJamList LEAVES (params JamListBase[] values)
	{
		return InvokeRule(nameof(LEAVES), values);
	}
	public static LocalJamList Match (params JamListBase[] values)
	{
		return InvokeRule(nameof(Match), values);
	}
	public static LocalJamList MATCH (params JamListBase[] values)
	{
		return InvokeRule(nameof(MATCH), values);
	}
	public static LocalJamList ForceCare (params JamListBase[] values)
	{
		return InvokeRule(nameof(ForceCare), values);
	}
	public static LocalJamList NoCare (params JamListBase[] values)
	{
		return InvokeRule(nameof(NoCare), values);
	}
	public static LocalJamList NOCARE (params JamListBase[] values)
	{
		return InvokeRule(nameof(NOCARE), values);
	}
	public static LocalJamList NOTIME (params JamListBase[] values)
	{
		return InvokeRule(nameof(NOTIME), values);
	}
	public static LocalJamList NotFile (params JamListBase[] values)
	{
		return InvokeRule(nameof(NotFile), values);
	}
	public static LocalJamList NOTFILE (params JamListBase[] values)
	{
		return InvokeRule(nameof(NOTFILE), values);
	}
	public static LocalJamList NoUpdate (params JamListBase[] values)
	{
		return InvokeRule(nameof(NoUpdate), values);
	}
	public static LocalJamList NOUPDATE (params JamListBase[] values)
	{
		return InvokeRule(nameof(NOUPDATE), values);
	}
	public static LocalJamList Subst (params JamListBase[] values)
	{
		return InvokeRule(nameof(Subst), values);
	}
	public static LocalJamList SubstLiteralize (params JamListBase[] values)
	{
		return InvokeRule(nameof(SubstLiteralize), values);
	}
	public static LocalJamList Temporary (params JamListBase[] values)
	{
		return InvokeRule(nameof(Temporary), values);
	}
	public static LocalJamList TEMPORARY (params JamListBase[] values)
	{
		return InvokeRule(nameof(TEMPORARY), values);
	}
	public static LocalJamList QueueJamfile (params JamListBase[] values)
	{
		return InvokeRule(nameof(QueueJamfile), values);
	}
	public static LocalJamList MD5 (params JamListBase[] values)
	{
		return InvokeRule(nameof(MD5), values);
	}
	public static LocalJamList MD5File (params JamListBase[] values)
	{
		return InvokeRule(nameof(MD5File), values);
	}
	public static LocalJamList Math (params JamListBase[] values)
	{
		return InvokeRule(nameof(Math), values);
	}
	public static LocalJamList W32_GETREG (params JamListBase[] values)
	{
		return InvokeRule(nameof(W32_GETREG), values);
	}
	public static LocalJamList W32_GETREG64 (params JamListBase[] values)
	{
		return InvokeRule(nameof(W32_GETREG64), values);
	}
	public static LocalJamList W32_SHORTNAME (params JamListBase[] values)
	{
		return InvokeRule(nameof(W32_SHORTNAME), values);
	}
	public static LocalJamList UseDepCache (params JamListBase[] values)
	{
		return InvokeRule(nameof(UseDepCache), values);
	}
	public static LocalJamList UseFileCache (params JamListBase[] values)
	{
		return InvokeRule(nameof(UseFileCache), values);
	}
	public static LocalJamList OptionalFileCache (params JamListBase[] values)
	{
		return InvokeRule(nameof(OptionalFileCache), values);
	}
	public static LocalJamList UseCommandLine (params JamListBase[] values)
	{
		return InvokeRule(nameof(UseCommandLine), values);
	}
	public static LocalJamList ScanContents (params JamListBase[] values)
	{
		return InvokeRule(nameof(ScanContents), values);
	}
	public static LocalJamList SCANCONTENTS (params JamListBase[] values)
	{
		return InvokeRule(nameof(SCANCONTENTS), values);
	}
	public static LocalJamList MightNotUpdate (params JamListBase[] values)
	{
		return InvokeRule(nameof(MightNotUpdate), values);
	}
	public static LocalJamList Needs (params JamListBase[] values)
	{
		return InvokeRule(nameof(Needs), values);
	}
	public static LocalJamList NEEDS (params JamListBase[] values)
	{
		return InvokeRule(nameof(NEEDS), values);
	}
	public static LocalJamList LuaString (params JamListBase[] values)
	{
		return InvokeRule(nameof(LuaString), values);
	}
	public static LocalJamList LuaFile (params JamListBase[] values)
	{
		return InvokeRule(nameof(LuaFile), values);
	}
	public static LocalJamList UseMD5Callback (params JamListBase[] values)
	{
		return InvokeRule(nameof(UseMD5Callback), values);
	}
	public static LocalJamList Shell (params JamListBase[] values)
	{
		return InvokeRule(nameof(Shell), values);
	}
	public static LocalJamList GroupByVar (params JamListBase[] values)
	{
		return InvokeRule(nameof(GroupByVar), values);
	}
	public static LocalJamList Split (params JamListBase[] values)
	{
		return InvokeRule(nameof(Split), values);
	}
	public static LocalJamList ExpandFileList (params JamListBase[] values)
	{
		return InvokeRule(nameof(ExpandFileList), values);
	}
	public static LocalJamList ListSort (params JamListBase[] values)
	{
		return InvokeRule(nameof(ListSort), values);
	}
	public static LocalJamList DependsList (params JamListBase[] values)
	{
		return InvokeRule(nameof(DependsList), values);
	}
	public static LocalJamList RuleExists (params JamListBase[] values)
	{
		return InvokeRule(nameof(RuleExists), values);
	}

}
