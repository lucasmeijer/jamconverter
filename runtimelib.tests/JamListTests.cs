using System.Collections.Generic;
using System.Linq;
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

        [Test]
        public void WithEmptySuffix()
        {
            Assert.AreEqual("myfile", new JamList("myfile").WithSuffix(new JamList()).ToString());
        }


        [Test]
        public void JoinWithValue()
        {
            Assert.AreEqual("this_is_nice", new JamList("this", "is","nice").JoinWithValue(new JamList("_")).ToString());
        }

        [Test]
        public void With()
        {
            var jamlist = new JamList().With("one").With(new JamList("two", "three")).With("four");
            Assert.AreEqual("one two three four", jamlist.ToString());
        }

        [Test]
        public void AsBoolReturnsTrue()
        {
            Assert.IsTrue(new JamList("a","c").AsBool());
        }

        [Test]
        public void AsBoolReturnsFase()
        {
            Assert.IsFalse(new JamList().AsBool());
        }

        [Test]
        public void Subtract()
        {
            var jamlist = new JamList("one", "two", "two", "three", "four");
            jamlist.Subtract(new JamList("four", "two"));
            CollectionAssert.AreEqual(new[] { "one","three"}, jamlist.Elements.ToArray());
        }

        [Test]
        public void ForEach()
        {
            var jamlist = new JamList("one", "two", "three");

            var result = new List<string>();
            foreach (JamList v in jamlist)
            {
                Assert.AreEqual(1, v.Elements.Count());
                result.Add(v.Elements.First());
            }

            CollectionAssert.AreEqual(new[] {"one","two","three"}, result);
        }


        [Test]
        public void PlayGround()
        {
        }
    }

}
