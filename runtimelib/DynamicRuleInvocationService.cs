using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace runtimelib
{
	public class DynamicRuleInvocationService
	{
		public static DynamicRuleInvocationService Instance;
		private readonly Type[] _types;

		public DynamicRuleInvocationService(params Type[] types)
		{
			_types = types;
		}

		public JamList InvokeRule(JamList jamList, params JamList[] arguments)
		{
            // Todo: Invoke multiple rules?
            var result = new List<string>();
		    foreach (var rule in jamList.Elements)
		        result.AddRange(BuiltinFunctions.InvokeRule(rule, arguments).Elements);

            return new JamList(result.ToArray());
		}

		static JamList InvokeMethod (MethodInfo method, JamList[] arguments)
		{
			bool isParamsMethod = method.GetParameters ().Any () && method.GetParameters () [0].ParameterType.IsArray;

			var targetArguments = isParamsMethod ? new object[] { arguments } : arguments;

			object result = method.Invoke (null, targetArguments);
			if (result == null)
				return new JamList ();
			return (JamList)result;
		}

		public void DynamicInclude(JamList value)
		{
            BuiltinFunctions.Include(value);
        }

		private MethodInfo FindMethod(string methodName)
		{
			return _types.SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static)).FirstOrDefault(m => m.Name == ConverterLogic.CleanIllegalCharacters(methodName));
		}
	}
}
