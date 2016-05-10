using System.Runtime.CompilerServices;
using System;
using System.Linq;

namespace Jam
{
	public enum ActionsFlags
	{
		None = 0,
		Updated = 1 << 0,
		Together = 1 << 1,
		Ignore = 1 << 2,
		Quietly = 1 << 3,
		Piecemeal = 1 << 4,
		Existing = 1 << 5,
		Response = 1 << 6,
		MaxLine = 1 << 7,
		Lua = 1 << 8,
		MaxTargets = 1 << 9,
		WriteFile = 1 << 10,
		ScreenOutput = 1 << 11,
		RemoveEmptyDirs = 1 << 12
	}

	public class Interop
	{
		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public static extern string[] InvokeRule(string rulename, string[][] param);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public static extern void MakeActions(string name,string actions,int flags, int maxTargets, int maxLines);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public static extern void SetVar(string name,string[] value);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public static extern string[] GetVar(string name);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public static extern void Setting(string name, string[] targets, string[] values);
					
		public static bool Enabled { get; set; }	

		static bool GetHasInterop()
		{
			try {
				GetVar("OS");
			}
			catch (MissingMethodException) 
			{
				return false;
			}
			return true;
		}
		static Interop()
		{
			Enabled = GetHasInterop();
		}
	}
}
