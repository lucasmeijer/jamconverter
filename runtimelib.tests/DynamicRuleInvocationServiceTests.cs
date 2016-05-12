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
			var result = new DynamicRuleInvocationService(typeof(DynamicRuleInvocationServiceTests)).InvokeRule(new LocalJamList("Hello","There"));
			CollectionAssert.AreEqual(new[] { "hello", "there"}, _calls);
			CollectionAssert.AreEqual(new[] { "helloReturn", "thereReturn" }, result);
		}

		public static LocalJamList Hello()
		{
			_calls.Add("hello");
			return new LocalJamList("helloReturn");
		}

		public static LocalJamList There()
		{
			_calls.Add("there");
			return new LocalJamList("thereReturn");
		}



		static List<string> _params = new List<string>();

		[Test]
		public void CanInvokeMethodTakingParamsJamList()
		{
			var result = new DynamicRuleInvocationService(typeof(DynamicRuleInvocationServiceTests)).InvokeRule(new LocalJamList("MethodTakingParams"), new LocalJamList("2"));
			CollectionAssert.AreEqual(new[] { "2" }, _params);
			CollectionAssert.AreEqual(new[] { "return" }, result);
		}

		static public LocalJamList MethodTakingParams(params LocalJamList[] values)
		{
			Assert.AreEqual (1, values.Length);
			_params.AddRange(values[0].Elements);
			return new LocalJamList ("return");
		}



		static List<string> _params2 = new List<string>();

		[Test]
		public void CanInvokeMethodTakingMultipleJamList()
		{
			var result = new DynamicRuleInvocationService(typeof(DynamicRuleInvocationServiceTests)).InvokeRule(new LocalJamList("MethodTakingMultipleJamList"), new LocalJamList("2"), new LocalJamList("hello"));
			CollectionAssert.AreEqual(new[] { "2","hello" }, _params2);
			CollectionAssert.AreEqual(new[] { "return" }, result);
		}

		static public LocalJamList MethodTakingMultipleJamList(LocalJamList one, LocalJamList two)
		{
			_params2.AddRange (one.Elements);
			_params2.AddRange (two.Elements);
			return new LocalJamList ("return");
		}
	}
}
