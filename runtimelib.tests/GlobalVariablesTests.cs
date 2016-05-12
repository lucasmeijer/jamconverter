using System;
using System.Linq;
using NUnit.Framework;

namespace runtimelib.tests
{
    [TestFixture]
    class GlobalVariablesTests
    {
		/*
        [Test]
        public void CanRetrieve()
        {
            var globals = new GlobalVariables();
            var value = new LocalJamList("hello");
            globals["myvar"] = value;
            Assert.AreEqual(value, globals["myvar"]);
        }

        [Test]
        public void GettingNonExistingVariableAndChangingItIsPersistent()
        {
            var globals = new GlobalVariables();
            var value = new LocalJamList("hello");
            globals["myvar"].Append(value);
            
            var jamList = globals["myvar"];
            CollectionAssert.AreEqual(value.Elements, jamList.Elements);
        }
		
		[Test]
		public void CanSetVariableOnTarget()
	    {
			var globals = new GlobalVariables();
			var value = new LocalJamList("hello");
			globals.GetOrCreateVariableOnTargetContext("harry", "myvar").Assign(value);
			using (globals.OnTargetContext("harry"))
			{
				Assert.That(globals["myvar"].Elements, Is.EqualTo(value.Elements));
			}
	    }

		[Test]
		public void FallsBackToGlobalVariableIfNotFoundOnTarget()
		{
			var globals = new GlobalVariables();
			var hello = new LocalJamList("hello");
			globals["myglobal"] = hello;

			globals.GetOrCreateVariableOnTargetContext("harry", "dummy").Assign();
			using (globals.OnTargetContext("harry"))
			{
				Assert.That(globals["myglobal"].Elements, Is.EqualTo(hello.Elements));
			}
		}

		[Test]
	    public void IgnoresIfOnTargetDoesNotExist()
	    {
		    var globals = new GlobalVariables();
			var value = new LocalJamList("sally");
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
	    }*/

	    [Test]
	    public void AssignIfEmpty()
	    {
		    var g = new GlobalVariables();
		    var harry = g["harry"];
		    harry.AssignIfEmpty("sally");
			CollectionAssert.AreEqual(new[] { "sally"}, harry.Elements);
	    }

	    [Test]
	    public void Seiasdioaj()
	    {
		    var g = new GlobalVariables();
		    RemoteJamList[] jamLists = g.RemoteJamListsForVariableOnTargets("myvar", new LocalJamList("target1", "target2"));
		    jamLists.Assign("c");

			MockJamStorage.Instance.GetValue("myvar")
	    }
	}
}
