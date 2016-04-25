using NUnit.Framework;

namespace runtimelib.tests
{
    [TestFixture]
    public class JamListTests
    {
        [Test]
        public void Grist()
        {
            Assert.AreEqual("<myapp>Harry", new JamList("Harry").GristWith(new JamList("myapp")).ToString());
        }

        [Test]
        public void ReGrist()
        {
            Assert.AreEqual("<myapp>Harry", new JamList("<oldapp>Harry").GristWith(new JamList("myapp")).ToString());
        }

        [Test]
        public void UnGrist()
        {
            Assert.AreEqual("Harry", new JamList("<oldapp>Harry").GristWith(new JamList()).ToString());
        }

        [Test]
        public void GristWithAngleBrackets()
        {
            Assert.AreEqual("<myapp>Harry", new JamList("<oldapp>Harry").GristWith(new JamList("<myapp>")).ToString());
        }


    }

}
