using System;
using System.Collections;
using System.Runtime.Remoting.Messaging;

namespace runtimelib
{
	public class RemoteJamList : JamListBase
	{
		private readonly string _variableName;
		private readonly string _onTarget;

		public RemoteJamList(string variableName)
		{
			_variableName = variableName;
		}

		public RemoteJamList(string variableName, string onTarget)
		{
			_variableName = variableName;
			_onTarget = onTarget;
		}

		public override string[] Elements
		{
			get {
#if EMBEDDED_MODE
				throw new NotImplementedException();
#else
				if (_onTarget != null)
					throw new NotSupportedException("You cannot read from a RemoteJamList which has a target. only writing please");
				return MockJamStorage.Instance.GetValue(_variableName);

#endif
			}
		}

		public override void Append(params JamListBase[] values)
		{
			InteropAssign("append", values);
		}

		private void InteropAssign(string @operator, JamListBase[] values)
		{
#if EMBEDDED_MODE
			//Jam.InteropAssign(_variableName,@operator,values);
#else
			MockJamStorage.Instance.InteropAssign(_variableName, _onTarget, @operator, values);
#endif
		}
		//myvar1 = a ; myvar2 = b ;
		//variables = myvar1 myvar2 ;
		//$(variables) += hello ;
		//
		//RemoteJamList[] rj = Globals.DereferenceElements(Globals.variables)
		//
		public override void Subtract(params JamListBase[] values)
		{
			InteropAssign("subtract", values);
		}

		public override void AssignIfEmpty(params JamListBase[] values)
		{
			InteropAssign("assignifempty", values);
		}

		public override void Assign(params JamListBase[] values)
		{
			InteropAssign("assign", values);
		}
	}
}
