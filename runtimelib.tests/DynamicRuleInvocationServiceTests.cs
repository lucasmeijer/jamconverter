using System.Collections.Generic;
using NUnit.Framework;

namespace runtimelib.tests
{
	[TestFixture]
	class DynamicRuleInvocationServiceTests
	{
		private static readonly List<string> _calls = new List<string>();

		[Test]
		public void CanInvokeMultipleMethods()
		{
			var result = new DynamicRuleInvocationService(typeof(DynamicRuleInvocationServiceTests)).InvokeRule(new JamList("Hello","There"));
			CollectionAssert.AreEqual(new[] { "hello", "there"}, _calls);
			CollectionAssert.AreEqual(new[] { "helloReturn", "thereReturn" }, result);
		}

		public static JamList Hello()
		{
			_calls.Add("hello");
			return new JamList("helloReturn");
		}

		public static JamList There()
		{
			_calls.Add("there");
			return new JamList("thereReturn");
		}



		static List<string> _params = new List<string>();

		[Test]
		public void CanInvokeMethodTakingParamsJamList()
		{
			var result = new DynamicRuleInvocationService(typeof(DynamicRuleInvocationServiceTests)).InvokeRule(new JamList("MethodTakingParams"), new JamList("2"));
			CollectionAssert.AreEqual(new[] { "2" }, _params);
			CollectionAssert.AreEqual(new[] { "return" }, result);
		}

		static public JamList MethodTakingParams(params JamList[] values)
		{
			Assert.AreEqual (1, values.Length);
			_params.AddRange(values[0].Elements);
			return new JamList ("return");
		}



		static List<string> _params2 = new List<string>();

		[Test]
		public void CanInvokeMethodTakingMultipleJamList()
		{
			var result = new DynamicRuleInvocationService(typeof(DynamicRuleInvocationServiceTests)).InvokeRule(new JamList("MethodTakingMultipleJamList"), new JamList("2"), new JamList("hello"));
			CollectionAssert.AreEqual(new[] { "2","hello" }, _params2);
			CollectionAssert.AreEqual(new[] { "return" }, result);
		}

		static public JamList MethodTakingMultipleJamList(JamList one, JamList two)
		{
			_params2.AddRange (one.Elements);
			_params2.AddRange (two.Elements);
			return new JamList ("return");
		}
	}
}
