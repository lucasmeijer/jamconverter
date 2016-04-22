using System.Collections;
using System.Linq;
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

        [Test]
        public void DoubleVariableAssignment()
        {
            AssertConvertedProgramHasIdenticalOutput("myvar = 123 ; myvar = 234 ; Echo $(myvar) ;");
        }

        [Test]
        public void IfStatement()
        {
            AssertConvertedProgramHasIdenticalOutput("myvar = 123 ; if $(myvar) { Echo Yes ; } ");
        }
    
        public void CustomRule()
        {
            AssertConvertedProgramHasIdenticalOutput(
                @"rule customrule { Echo Hello ; } customrule ;"
                );
        }

        private static void AssertConvertedProgramHasIdenticalOutput(string simpleProgram)
        {
            var csharp = new JamToCSharpConverter().Convert(simpleProgram);

            var jamResult = new JamRunner().Run(simpleProgram).Select(s => s.TrimEnd());
            var csharpResult = new CSharpRunner().Run(csharp).Select(s => s.TrimEnd());

            CollectionAssert.AreEqual(jamResult, csharpResult);
        }
    }
}
