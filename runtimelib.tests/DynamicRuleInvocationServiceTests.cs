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
	}
}
