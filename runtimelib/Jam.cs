using System.Runtime.CompilerServices;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;

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

	public class InteropHelper
	{
		static InteropHelper()
		{
			InteropHelper.Enabled = InteropHelper.GetHasInterop();
		}

		public static bool Enabled { get; set; }

		private static bool GetHasInterop()
		{
#if EMBEDDED_MODE
			return true;
#else
			return false;
#endif
		}

#if EMBEDDED_MODE
	public class Interop
	{
		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public static extern string[] InvokeRule(string rulename, string[][] param);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		[DllImport("__Internal")]

		public static extern void MakeActions(string name,string actions,int flags, int maxTargets, int maxLines);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public static extern void SetVar(string name,string[] value);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public static extern string[] GetVar(string name);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public static extern void Setting(string name, string[] targets, string[] values);
	}
#endif
	}
}
