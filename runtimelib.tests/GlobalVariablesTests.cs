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
            Assert.AreEqual(value, globals.Get("myvar"));
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
    }
}
