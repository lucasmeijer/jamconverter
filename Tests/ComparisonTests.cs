using System.Collections;
using System.Linq;
using System.Runtime.InteropServices;
using NiceIO;
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

        [Test]
        public void ExpressionListAssignment()
        {
            AssertConvertedProgramHasIdenticalOutput("myvar = a b ; Echo $(myvar) ; ");
        }

        [Test]
        public void CombineExpression()
        {
            AssertConvertedProgramHasIdenticalOutput("myvar = john doe ; Echo $(myvar)postfix ; ");
        }

        [Test]
        public void CustomRule()
        {
            AssertConvertedProgramHasIdenticalOutput(
                @"rule customrule { Echo Hello ; } customrule ;"
                );
        }

        [Test]
        public void CustomRuleWithArgument()
        {
            AssertConvertedProgramHasIdenticalOutput(
                @"rule customrule arg1 { Echo $(arg1) ; } customrule hello ;"
                );
        }

        [Test]
        public void SuffixVariableExpansion()
        {
            AssertConvertedProgramHasIdenticalOutput(
@"
myvar = main.cs ; 
Echo $(myvar:S=.cpp) ;

myvar = main.cs.pieter ; 
Echo $(myvar:S=.cpp:S=.exe) ;

myvar = main.cs.pieter ; 
Echo $(myvar:S=:S=.exe) ;
");
        }

        [Test]
        public void RuleReturningValue()
        {
            AssertConvertedProgramHasIdenticalOutput(
@"
rule GimmeFive
{
  return five ;
}
Echo [ GimmeFive ] ;
");
        }

        [Test]
        public void BuiltinMD5()
        {
            AssertConvertedProgramHasIdenticalOutput("Echo [ MD5 harry ] ;");
        }

        [Test]
        public void VariableDereferencingWithIndexer()
        {
            AssertConvertedProgramHasIdenticalOutput(
@"
myvar = a b c d e ; 
Echo $(myvar[2]) ;

myindex = 3 ;
Echo $(myvar[$(myindex)]) ;

myindices = 3 4 1 ;
Echo $(myvar[$(myindices)]) ;

Echo $(myvar[$(myindices)]:S=.mysuffix) ;
");
        }

        [Test]
        public void AppendOperator()
        {
            AssertConvertedProgramHasIdenticalOutput(
@"
myvar = a ;
myvar += b c ;
Echo $(myvar) ;
");
        }


        private static void AssertConvertedProgramHasIdenticalOutput(string simpleProgram)
        {
            var csharp = new JamToCSharpConverter().Convert(simpleProgram);

            var jamResult = new JamRunner().Run(simpleProgram).Select(s => s.TrimEnd());
            var csharpResult = new CSharpRunner().Run(csharp, new [] { new NPath("c:/jamconverter/Runtime/JamList.cs"), new NPath("c:/jamconverter/Runtime/BuiltinFunctions.cs") }).Select(s => s.TrimEnd());

            CollectionAssert.AreEqual(jamResult, csharpResult);
        }
    }
}
