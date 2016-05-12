using System;
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
			#if EMBEDDED_MODE

			#else
			var results = new JamList();
			foreach (var value in jamList.Elements)
			{
				MethodInfo method = FindMethod(value);
				if (method == null)
					throw new ArgumentException("Unable to find dynamically invoked rule: " + value);

				results.Append (InvokeMethod (method, arguments));
			}
			return results;
			#endif
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
			foreach (var fileName in value.Elements)
			{
				var type = _types.Single(t => t.Name == ConverterLogic.ClassNameForJamFile(fileName));
				type.GetMethod("TopLevel").Invoke(null, null);
			}
		}

		private MethodInfo FindMethod(string methodName)
		{
			return _types.SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static)).FirstOrDefault(m => m.Name == ConverterLogic.CleanIllegalCharacters(methodName));
		}
	}
}
