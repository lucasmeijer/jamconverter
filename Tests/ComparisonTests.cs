using System.Collections;
using System.Runtime.InteropServices;
using NUnit.Framework;

namespace jamconverter.Tests
{
    [TestFixture]
    class ComparisonTests
    {
        [Test]
        public void Simple()
        {
            AssertConvertedProgramHasIdenticalOutput("Echo Hello ;");
        }

        [Test]
        public void TwoEchos()
        {
            AssertConvertedProgramHasIdenticalOutput("Echo Hello ; Echo There ;");
        }


        [Test]
        public void EchoMultipleLiterals()
        {
            AssertConvertedProgramHasIdenticalOutput("Echo Hello There Sailor ;");
        }

        [Test]
        public void VariableExpansion()
        {
            AssertConvertedProgramHasIdenticalOutput("myvar = 123 ; Echo $(myvar) ;");
        }

        private static void AssertConvertedProgramHasIdenticalOutput(string simpleProgram)
        {
            var csharp = new JamToCSharpConverter().Convert(simpleProgram);

            var jamResult = new JamRunner().Run(simpleProgram);
            var csharpResult = new CSharpRunner().Run(csharp);

            CollectionAssert.AreEqual(jamResult, csharpResult);
        }
    }
}
