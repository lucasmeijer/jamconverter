using System;
using System.Collections;
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
            Assert.AreEqual("<myapp>Harry", new LocalJamList("Harry").GristWith(new LocalJamList("myapp")).ToString());
        }

        [Test]
        public void ReGrist()
        {
            Assert.AreEqual("<myapp>Harry", new LocalJamList("<oldapp>Harry").GristWith(new LocalJamList("myapp")).ToString());
        }

        [Test]
        public void UnGrist()
        {
            Assert.AreEqual("Harry", new LocalJamList("<oldapp>Harry").GristWith(new LocalJamList()).ToString());
        }

        [Test]
        public void GristWithAngleBrackets()
        {
            Assert.AreEqual("<myapp>Harry", new LocalJamList("<oldapp>Harry").GristWith(new LocalJamList("<myapp>")).ToString());
        }
		
        [Test]
        public void AsBoolReturnsTrue()
        {
            Assert.IsTrue(new LocalJamList("a","c").AsBool());
        }

        [Test]
        public void AsBoolReturnsFase()
        {
            Assert.IsFalse(new LocalJamList().AsBool());
        }

        [Test]
        public void Subtract()
        {
            var jamlist = new LocalJamList("one", "two", "two", "three", "four");
            jamlist.Subtract(new LocalJamList("four", "two"));
            CollectionAssert.AreEqual(new[] { "one","three"}, jamlist.Elements.ToArray());
        }

        [Test]
        public void ForEach()
        {
            var jamlist = new LocalJamList("one", "two", "three");

            var result = new List<string>();
            foreach (LocalJamList v in jamlist)
            {
                Assert.AreEqual(1, v.Elements.Count());
                result.Add(v.Elements.First());
            }

            CollectionAssert.AreEqual(new[] {"one","two","three"}, result);
        }

        [Test]
        public void IsIn()
        {
            var jamlist = new LocalJamList("one", "two", "three", "four");

            Assert.IsTrue(new LocalJamList("one").IsIn(jamlist));
            Assert.IsTrue(new LocalJamList("one","two").IsIn(jamlist));
            Assert.IsFalse(new LocalJamList("one", "five").IsIn(jamlist));
        }

	    [Test]
	    public void ImplicitConversion()
	    {
		    LocalJamList j = "hello";
			CollectionAssert.AreEqual(new[] { "hello"}, j.Elements);
	    }

	    [Test]
	    public void AppendWithMoreArguments()
	    {
		    var j = new LocalJamList("initial");
			j.Append("asd", "asd2");

			CollectionAssert.AreEqual(new[] { "initial", "asd", "asd2"}, j.Elements);
	    }

	    [Test]
	    public void Clone()
	    {
		    var j = new LocalJamList("juha");
		    var clone = j.Clone();
			CollectionAssert.AreEqual(new[] { "juha"}, clone.Elements);
	    }

	    [Test]
	    public void AssignIfEmpty()
	    {
		    var j = new LocalJamList();
		    j.AssignIfEmpty("harry");
			CollectionAssert.AreEqual(new[] { "harry" }, j.Elements);

			j.AssignIfEmpty("sally");
			CollectionAssert.AreEqual(new[] { "harry" }, j.Elements);
		}

	    [Test]
	    public void GreaterThan()
	    {
		    var left = new LocalJamList("1");
		    var right = new LocalJamList("3");
		    Assert.IsFalse(left.GreaterThan(right));
			Assert.IsTrue(right.GreaterThan(left));
		}

		[Test]
		public void LessThan()
		{
			var left = new LocalJamList("1");
			var right = new LocalJamList("3");
			Assert.IsTrue(left.LessThan(right));
			Assert.IsFalse(right.LessThan(left));
		}

		[Test]
	    public void ElementsAsJamList()
	    {
		    var j = new LocalJamList("one", "two");

		    var elementsAsLists = j.ElementsAsJamLists.ToArray();
			CollectionAssert.AreEqual(new[] { "one" }, elementsAsLists[0]);
			CollectionAssert.AreEqual(new[] { "two" }, elementsAsLists[1]);

		}
	}

}
