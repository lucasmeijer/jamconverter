using System;
using System.Linq;
using Jam;

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
				if (_onTarget != null)
					throw new NotSupportedException("You cannot read from a RemoteJamList which has a target. only writing please");

			    return Interop.GetVar(_variableName);
			}
		}

		public override void Append(params JamListBase[] values)
		{
			InteropAssign(Operator.VAR_APPEND, ElementsOf(values));
		}

		private void InteropAssign(Jam.Operator @operator, string[] values)
		{
            if (_onTarget != null)
                Jam.Interop.SetSetting(_variableName, new [] { _onTarget}, @operator, values);
            else
    		    Jam.Interop.SetVar(_variableName, @operator, values);
		}

	    //myvar1 = a ; myvar2 = b ;
		//variables = myvar1 myvar2 ;
		//$(variables) += hello ;
		//
		//RemoteJamList[] rj = Globals.DereferenceElements(Globals.variables)
		//
		public override void Subtract(params JamListBase[] values)
		{
		    var valueElements = ElementsOf(values);


            //we do a manual get, subtract, set here, because the internal jam implementatno seems to be able to do something smart where it doesn't use
            //a strcmp() to see if an item should be removed, but it does a direct const char* comparison.  for some reason jam is able to guarantee
            //that these strings will be unique. I have not yet figured out why. it's even able to do this if you construct the string at runtime.
            //until we figure out how we can do the same trick, use a slower implemenatation.
            InteropAssign(Operator.VAR_SET, Elements.Where(e => !valueElements.Contains(e)).ToArray());

            //this is the fast internal impl;
            //InteropAssign(Operator.VAR_REMOVE, valueElements);
		}

		public override void AssignIfEmpty(params JamListBase[] values)
		{
			InteropAssign(Operator.VAR_DEFAULT, ElementsOf(values));
		}

		public override void Assign(params JamListBase[] values)
		{
			InteropAssign(Operator.VAR_SET, ElementsOf(values));
		}
	}
}
