using System;
using System.Collections;
using System.Collections.Generic;
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
            AssertConvertedProgramHasIdenticalOutput(
@"
myvar = 123 ; 
if $(myvar) { Echo msg1 ; } else { Echo msg1a ; }
if ! $(myvar) { Echo msg2 ; } else { Echo msg2a ; }

if $(myvar) = 123 { Echo msg3 ; }  else { Echo msg3a ; }

if $(myvar) = 321 { Echo msg5 ; } else { Echo msg5a ; }

myemptyvar = ;
if $(myemptyvar) { Echo msgA ; } else { Echo msg6a ; }
if ! $(myemptyvar) { Echo msgB ; } else { Echo msg7a ; }

if $(myvar) = 3212 { Echo yes ; } else if $(myvar) = 123 { Echo no ; } else Echo Boink ;
myvar = neither ;
if $(myvar) = 3212 { Echo yes ; } else if $(myvar) = 123 { Echo no ; } else Echo Boink ;

        Echo end ;
");
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
        public void InOperator()
        {
            AssertConvertedProgramHasIdenticalOutput(
@"
myvar = 123 ; 
if $(myvar) in 123 { Echo Yes ; } else { Echo No ; } 
if $(myvar) in a b 123 { Echo Yes ; } else { Echo No ; } 
if $(myvar) in a b 125 { Echo Yes ; } else { Echo No ; } 

myvar = a b ;
if $(myvar) in a b { Echo Yes ; } else { Echo No ; }
if $(myvar) in a x b { Echo Yes ; } else { Echo No ; }
if $(myvar) in a c { Echo Yes ; } else { Echo No ; }
if $(myvar) in b c { Echo Yes ; } else { Echo No ; }
if $(myvar) in d e { Echo Yes ; } else { Echo No ; }


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
            AssertConvertedProgramHasIdenticalOutput(
@"myvar = john doe ; 
Echo $(myvar)postfix ; 

myemptyvar = ;
Echo $(myvar)$(myemptyvar)hello ;

Echo one$(myvar)two ;
Echo one$(myvar)$(myvar)two ;

");
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
        public void KeywordsInExpressionList()
        {
            AssertConvertedProgramHasIdenticalOutput(
@"
myvar = i am on in for while if a boat ; 
Echo $(myvar) ;
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
        public void BlockStatement()
        {
            AssertConvertedProgramHasIdenticalOutput(
                @"
{
  Echo a ; 
  {
      Echo b ;
  }
  Echo c ;
}

Echo d ;
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

        [Test]
        public void While()
        {
            AssertConvertedProgramHasIdenticalOutput(
@"
myvar = one two three four ;
while $(myvar)
{
   Echo $(myvar) ;
   myvar -= $(myvar[1]) ;
}");
        }

        //[Test]
        public void OnVariables()
        {
            AssertConvertedProgramHasIdenticalOutput(
@"
myvar = one ;
Echo a $(myvar) ;
on $(mytarget) { Echo b $(myvar) ; myglobal = wham ; }
Echo $(myglobal) ;
");
        }

        [Test]
        public void ForLoop()
        {
            AssertConvertedProgramHasIdenticalOutput(
@"
mylist = a b c d e ;
for myvar in $(mylist) f g
{
  if $(myvar) = c { Echo continueing ; continue ; }

  if $(myvar) = f { break ; }

  Echo $(myvar) ;
}

");
        }


        [Test]
        public void SwitchStatement()
        {
            AssertConvertedProgramHasIdenticalOutput(
@"

rule MySwitch myvar
{
   switch $(myvar)
   {
       case a :
         Echo I was a ;
       case b :
         Echo I was b ;
   }
   Echo after case ;
}

MySwitch a ;
MySwitch b ;
MySwitch c ;

");
        }



        private static void AssertConvertedProgramHasIdenticalOutput(string simpleProgram)
        {
            var jamResult = new JamRunner().Run(simpleProgram).Select(s => s.TrimEnd());
            Console.WriteLine("Jam:");
            foreach (var l in jamResult)
                Console.WriteLine(l);

            IEnumerable<string> csharpResult = null;

            try
            {
                var csharp = new JamToCSharpConverter().Convert(simpleProgram);
                csharpResult = new CSharpRunner().Run(csharp, new[] {new NPath("c:/jamconverter/bin/runtimelib.dll")}).Select(s => s.TrimEnd());

                Console.WriteLine("C#:");
                foreach (var l in csharpResult)
                    Console.WriteLine(l);
                Console.WriteLine();
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed converting/running to c#: "+e);
            }
            CollectionAssert.AreEqual(jamResult, csharpResult);

        }
    }
}
