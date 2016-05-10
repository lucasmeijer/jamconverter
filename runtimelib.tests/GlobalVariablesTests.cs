using System;
using System.Linq;
using NUnit.Framework;

namespace runtimelib.tests
{
    [TestFixture]
    class GlobalVariablesTests
    {
        [Test]
        public void CanRetrieve()
        {
            var globals = new GlobalVariables();
            var value = new JamList("hello");
            globals["myvar"] = value;
            Assert.AreEqual(value, globals["myvar"]);
        }

        [Test]
        public void GettingNonExistingVariableAndChangingItIsPersistent()
        {
            var globals = new GlobalVariables();
            var value = new JamList("hello");
            globals["myvar"].Append(value);
            
            var jamList = globals["myvar"];
            CollectionAssert.AreEqual(value.Elements, jamList.Elements);
        }
		
		[Test]
		public void CanSetVariableOnTarget()
	    {
			var globals = new GlobalVariables();
			var value = new JamList("hello");
			globals.GetOrCreateVariableOnTargetContext("harry", "myvar").Assign(value);
			using (globals.OnTargetContext("harry"))
			{
				Assert.That(globals["myvar"].Elements, Is.EqualTo(value.Elements));
			}
	    }

	    [Test]
	    public void IgnoresIfOnTargetDoesNotExist()
	    {
		    var globals = new GlobalVariables();
			var value = new JamList("sally");
		    globals["harry"] = value;
		    using (globals.OnTargetContext("doesNotExist"))
		    {
				Assert.That(globals["harry"].Elements, Is.EqualTo(value.Elements));
		    }
	    }

	    [Test]
	    public void NestedUsingsThrowsException()
	    {
		    var globals = new GlobalVariables();
		    globals.GetOrCreateVariableOnTargetContext("outer", "harry").Assign("sally");
		    globals.GetOrCreateVariableOnTargetContext("inner", "harry").Assign("sally");
		    using (globals.OnTargetContext("outer"))
		    {
			    Assert.That(() =>
			    {
				    globals.OnTargetContext("inner");
			    }, Throws.InstanceOf<NotSupportedException>());
		    }
	    }
	}
}
