using System;
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
        public void IsIn()
        {
            var jamlist = new JamList("one", "two", "three", "four");

            Assert.IsTrue(new JamList("one").IsIn(jamlist));
            Assert.IsTrue(new JamList("one","two").IsIn(jamlist));
            Assert.IsFalse(new JamList("one", "five").IsIn(jamlist));
        }

	    [Test]
	    public void ImplicitConversion()
	    {
		    JamList j = "hello";
			CollectionAssert.AreEqual(new[] { "hello"}, j.Elements);
	    }

	    [Test]
	    public void AppendWithMoreArguments()
	    {
		    var j = new JamList("initial");
			j.Append("asd", "asd2");

			CollectionAssert.AreEqual(new[] { "initial", "asd", "asd2"}, j.Elements);
	    }

	    [Test]
	    public void Clone()
	    {
		    var j = new JamList("juha");
		    var clone = j.Clone();
			CollectionAssert.AreEqual(new[] { "juha"}, clone.Elements);
	    }

	    [Test]
	    public void Include()
	    {
		    var j = new JamList("hello","there","sailor");
			CollectionAssert.AreEqual(new[] {"there"}, j.Include("th").Elements);
	    }

		[Test]
		public void IncludeWithRegex()
		{
			var j = new JamList("hello", "there", "sailor");
			CollectionAssert.AreEqual(new[] { "hello" }, j.Include("hel+").Elements);
		}

		[Test]
		public void IncludeOnlyAllowsSingleElement()
		{
			var j = new JamList("hello", "there", "sailor");
			var pattern = new JamList("one","two");
			Assert.Throws<ArgumentException>(() => j.Include(pattern));
		}


		[Test]
        public void PlayGround()
        {
        }
    }

}
