using System.Runtime.CompilerServices;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Policy;

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
            return true;
        }
    }

    public class Interop
	{
		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public static extern string[] InvokeRule(string rulename, string[][] param);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		[DllImport("__Internal")]

		public static extern void MakeActions(string name,string actions,int flags, int maxTargets, int maxLines);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public static extern void SetVar(string name, Jam.Operator @operator, string[] value);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public static extern string[] GetVar(string name);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public static extern void SetSetting(string name, string[] targets, Jam.Operator @operator, string[] values);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern void Include(string jamfile);
        
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static extern void RegisterRuleInternal(string name, object callback);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public static extern string[] Expand(string name);

		public static void RegisterRule(string name, Func<string[][], string[]> callback)
        {
            RegisterRuleInternal(name, callback);
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern void PushSettingsFor(string target);

        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern void PopSettingsFor(string target);



	}

    public enum Operator
    {
        VAR_SET	= 0,	/* override previous value */
        VAR_APPEND = 1,	/* append to previous value */
        VAR_DEFAULT	= 2,	/* set only if no previous value */
        VAR_REMOVE = 3	/* filter an old value */
    }
}
