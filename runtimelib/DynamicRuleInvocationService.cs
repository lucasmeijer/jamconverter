using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace runtimelib
{
	public class DynamicRuleInvocationService
	{
		private readonly Type[] _types;

		public DynamicRuleInvocationService(params Type[] types)
		{
			_types = types;
		}

		public JamList InvokeRule(JamList jamList, params JamList[] arguments)
		{
			var results = new JamList();
			foreach (var value in jamList.Elements)
			{
				MethodInfo method = FindMethod(value);
				if (method == null)
					throw new ArgumentException("Unable to find dynamically invoked rule: " + value);

				results.Append((JamList) method.Invoke(null, arguments));
			}
			return results;
		}

		private MethodInfo FindMethod(string methodName)
		{
			return _types.SelectMany(t => t.GetMethods(BindingFlags.NonPublic | BindingFlags.Static)).FirstOrDefault(m => m.Name == methodName);
		}
	}
}
