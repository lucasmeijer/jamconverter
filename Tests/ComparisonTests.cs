using System;
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
        public void EqualsConditional()
        {
            AssertConvertedProgramHasIdenticalOutput(
@"
myvar = 123 ; 
if $(myvar) = 123 { Echo Yes1 ; } 
if $(myvar) = 321 { Echo Yes2 ; } 
");
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
#myvar = main.cs ; 
#Echo $(myvar:S=.cpp) ;

#myvar = main.cs.pieter ; 
#Echo $(myvar:S=.cpp:S=.exe) ;

myvar = main.cs.pieter ; 
Echo $(myvar:S=) ;
");
        }

        [Test]
        public void EmptyVariableExpansion()
        {
            AssertConvertedProgramHasIdenticalOutput(
@"
myvar = ; 
Echo $(myvar:E=alternative) ;

myvar = realvalue ;
Echo $(myvar:E=alternative) ;

myvar = ;
Echo $(myvar:E=*) ;

");
        }

        [Test]
        public void JoinValueExpansion()
        {
            AssertConvertedProgramHasIdenticalOutput(
@"
myvar = im on a boat ; 
Echo $(myvar:J=_) ;
");
        }
        

      [Test]
        public void GristVariableExpansion()
        {
            AssertConvertedProgramHasIdenticalOutput(@"
myvar = harry ; 
Echo $(myvar:G=mygrist) ;

myvar = <pregisted>realvalue ;
Echo $(myvar:G=mygrist) ;

Echo $(myvar:G=<gristwithanglebrackets>) ;

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
        public void RuleAndVariableWithDotInName()
        {
            AssertConvertedProgramHasIdenticalOutput(
                @"

rule I.Love.Dots dot.in.argument
{
  return $(dot.in.argument) dot.in.literal ;
}

dots.in.variable = 3 ;
Echo [ I.Love.Dots 18 ] ;
Echo  $(dots.in.variable) ;
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

#index out of range:
myvar = a b c ;
Echo $(myvar[4]) ;

myvar = a b c ;
myindices = 1 5 3 ;  #note 5 is out of range
Echo $(myvar[$(myindices)]) ;

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

      //  [Test]
        public void ExpandToBound()
        {
            AssertConvertedProgramHasIdenticalOutput(
@"
myvar = a ;
Echo $(myvar:T) ;
");
        }

        private static void AssertConvertedProgramHasIdenticalOutput(string simpleProgram)
        {
            var csharp = new JamToCSharpConverter().Convert(simpleProgram);

            var jamResult = new JamRunner().Run(simpleProgram).Select(s => s.TrimEnd());
            var csharpResult = new CSharpRunner().Run(csharp, new [] { new NPath("c:/jamconverter/bin/runtimelib.dll") }).Select(s => s.TrimEnd());

            Console.WriteLine("C#:");
            foreach (var l in csharpResult)
                Console.WriteLine(l);
            Console.WriteLine();
            Console.WriteLine("Jam:");
            foreach (var l in jamResult)
                Console.WriteLine(l);

            CollectionAssert.AreEqual(jamResult, csharpResult);
        }
    }
}
