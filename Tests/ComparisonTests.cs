﻿using System;
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
		public void EchoEdgeCases()
		{
			AssertConvertedProgramHasIdenticalOutput(
@"
list = """" ;
Echo $(list) ; # No output.

list = """" foo ;
Echo $(list) ;
"
			);
		}

        [Test]
        public void VariableExpansion()
        {
            AssertConvertedProgramHasIdenticalOutput(
@"
myvar = 123 ;
Echo $(myvar) ;

foo = FOO ;
for x in $(foo) { Echo $(x) ; }
for x in ""$(foo)"" { Echo $(x) ; }
for x in $(""foo)"" { Echo $(x) ; }
#for x in $\(foo) { Echo $(x) ; }
for x in \$(foo) { Echo $(x) ; }
"
			);
        }

		[Test]
		public void DereferenceCombineExpression()
		{
			AssertConvertedProgramHasIdenticalOutput(
@"
abc = 123 ; 
myvar = a ; 
Echo $($(myvar)bc:G=hi) ;

Echo ""foo  $(abc)bar"" ;
"
			);
		}

		[Test]
		public void ValueSemantics()
		{
			AssertConvertedProgramHasIdenticalOutput(
@"myvar = a b c ;
myvar2 = $(myvar) ;
myvar2 += d ; 
Echo $(myvar) ;
Echo $(myvar2) ;

rule MyFunc myarg
{
  myarg += a ;
}

myvar3 = hello ;
MyFunc $(myvar3) ;
Echo $(myvar3) ;

#test return value value semantics

myreturnvalue = a b c ;

rule ReturnMe
{
  return $(myreturnvalue) ;
}

myvar5 = [ ReturnMe ] ;
myvar5 += d ;
Echo $(myvar5) ;
Echo $(myreturnvalue) ;




");
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
#This doesnt work because jam is crazy: if ! $(myvar) = 321 { Echo msg5 ; } else { Echo msg5a ; }

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
        public void Assignments()
        {
            AssertConvertedProgramHasIdenticalOutput(
@"myvar = a b ; 
Echo $(myvar) ; 

harry ?= sally ;
Echo $(harry) ;

harry ?= sailor ;
Echo $(harry) ;

myvar1 = a ;
myvar2 = a b ;

myvars = myvar1 myvar2 ;
$(myvars) += c ;
Echo $(myvar1) ;
Echo $(myvar2) ;
Echo $(myvars) ;

rule MyRule myarg
{
   myarg = 2 ;
   Echo myarg $(myarg) ;
}
myarg = 5 ;
MyRule 4 ;
Echo $(myarg) ;

myvar = harry johny ;
$(myvar)_sally = 123 ;
Echo $(harry_sally) _ $(johny_sally) ;

");
        }

		[Test]
	    public void DynamicRuleInvocation()
	    {

		    AssertConvertedProgramHasIdenticalOutput(
				@"
rule harry arg0 { Echo harry $(arg0) ; return harryreturn ; }
rule sally arg0 { Echo sally $(arg0) ; return sallyreturn ; }
rule MakeArg2 { Echo makearg2 ; return one ; }

myrules = harry sally ;
whynot = yolo ;
#Echo [ $(myrules) $(whynot) [ MakeArg2 ] ] ;
$(myrules) $(whynot) [ MakeArg2 ] ;
");
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
		public void RootedModifier()
		{
			AssertConvertedProgramHasIdenticalOutput(
@"
rooted = /foo/bar ;
notRooted = bar ;
multipleRoots = /foo /phumt ;

Echo $(rooted:R=/foo) ;
Echo $(notRooted:R=/foo) ;
Echo $(notRooted:R=$(multipleRoots)) ;
"
			);
		}

		[Test]
		public void ParentDirectoryModifier()
		{
			AssertConvertedProgramHasIdenticalOutput(
@"
path = /foo/bar ;
Echo 1 $(path:P) ;

nonpath = bar foo ;
Echo 2 $(nonpath:P) ;

multipleElements = foo bar foo/bar ;
Echo 3 $(multipleElements:P) ;

empty = ;
Echo 4 $(empty:P) ;
"
			);
		}

		[Test]
		[Ignore("Need to investigate jam behaviour")]
		public void MultipleDifferentModifiers()
		{
			AssertConvertedProgramHasIdenticalOutput(
@"
mylist = hello there sailor ;
Echo $(mylist:I=hello:S=exe:I=sailor:X=hello) ;
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
		[Ignore(("I hope we dont need this behaviour"))]
		public void KeywordsInExpressionList()
        {
            AssertConvertedProgramHasIdenticalOutput(
@"
myvar = i am on in for while if a boat ; 
Echo $(myvar) ;
");
        }

	    [Test]
	    public void GreaterThanOperator()
	    {
			AssertConvertedProgramHasIdenticalOutput(
@"
rule Return3 { return 3 ; }
rule Return0 { return 0 ; }
if [ Return3 ] > 1 { Echo Yes ; } else { Echo no ; }
if [ Return0 ] > 1 { Echo Yes ; } else { Echo no ; }

if [ Return3 ] < 3 { Echo Yes ; } else { Echo no ; }
if [ Return0 ] < 3 { Echo Yes ; } else { Echo no ; }

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
	    public void EmptyRuleInvocation()
	    {
			AssertConvertedProgramHasIdenticalOutput("rule Hello { } Hello ; Echo test ;");
		}

		[Test]
		public void AssignResultOfRuleInvocation()
		{
			AssertConvertedProgramHasIdenticalOutput(
@"
rule MyRule arg0 : arg1 { Echo $(arg0) $(arg1) ; return ""Hello"" ; }

myvar = [ MyRule a : b ] ;
Echo $(myvar) ;
"
			);
		}

		[Test]
		public void RuleInvocationWithImplicitParameters()
		{
			AssertConvertedProgramHasIdenticalOutput(@"
				# Single implicit parameter
				rule Hello1 { Echo $(1) ; } Hello1 a ;
				# Two implicit parameter with only one being referenced
				rule Hello2 { Echo $(2) ; } Hello2 a : b ;
				# argument with explicit name being referenced using numeric reference
				rule Hello3 explicitA : explicitB { Echo $(2) _ $(explicitB) ; } Hello3 a : b ;
				# > < syntax
				rule Hello4 { Echo $(<) _ $(>) ; } Hello4 a : b ;
			"
			);
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
		public void Braces()
		{
			AssertConvertedProgramHasIdenticalOutput(
@"
if x { }
# if x {} # Syntax error in Jam.
Echo end of test ;
"
			);
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

	    [Test]
	    public void Conditions()
	    {
			AssertConvertedProgramHasIdenticalOutput(
@"
one = 1 ;

if $(one) && $(zero) { Echo Yes ; } else { Echo no ; }
if $(one) && $(one) { Echo Yes ; } else { Echo no ; }
if $(zero) || $(one) { Echo Yes ; } else { Echo no ; }
if $(zero) || $(zero) { Echo Yes ; } else { Echo no ; }
if $(zero) != $(one) { Echo Yes ; } else { Echo no ; }
if $(zero) != $(zero) { Echo Yes ; } else { Echo no ; }
if $(zero) = $(one) { Echo Yes ; } else { Echo no ; }
if $(zero) = $(zero) { Echo Yes ; } else { Echo no ; }

if $(zero) { Echo with parenthesis ; }

if $(zero) && $(one) = 1 { Echo Yes ; } else { Echo no ; }


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


one = 1 ;
two = 2 ;
myvars = one two ; #evil bonus points: add myvar here
for myvar in $(myvars)
{
   Echo $(myvar) $($(myvar)) ;
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

        [Test]
        public void DynamicVariables()
        {
            AssertConvertedProgramHasIdenticalOutput(
@"
mylist = a b c d e ; 
myvar = mylist ;

Echo $($(myvar)) ;
$(myvar) = 1 2 3 ;

Echo $(mylist) ;

");
        }


		[Test]
		[Ignore("broken")]
		public void IncludeModifier()
		{
			AssertConvertedProgramHasIdenticalOutput(
@"
mylist = hello there sailor.c ; 
Echo $(mylist:I=th) ;

patterninvar = sai ;
Echo $(mylist:I=$(patterninvar)) ;

#make test for regex
Echo $(mylist:I=hel+) ;

# Jam treats double backslashes like one that still escapes the next character.
# Both expressions should match 'sailor.c'.
Echo $(mylist:I=\.c) ;
Echo $(mylist:I=\\.c) ;

pathWithBackslash = a\\b ;
Echo $(pathWithBackslash) ;
#Echo $(pathWithBackslash:I=\\) ; # Not valid in Jam. Jam does one level of escaping, regex another.
Echo $(pathWithBackslash:I=\\\\) ;

Echo $(mylist:I=\\.c$) ;
Echo $(mylist:I=\\.c\$) ;

");
		}

	    [Test]
	    public void Escaping()
	    {
		    AssertConvertedProgramHasIdenticalOutput(
@"
mylist = a\ b   a\\b    a\bb   a\n\r\t\bc  a\$b  a$b ;
for e in $(mylist) {
  Echo $(e) ;
}
"
			);
	    }

	    [Test]
		public void Quoting()
	    {
		    AssertConvertedProgramHasIdenticalOutput(
@"
mylist = foo"" ""bar a\""b ""a b c"": ;
for e in $(mylist) {
  Echo $(e) ;
}

local dollar = ""$"" ;
Echo $(dollar) ;
"
			);
	    }

	    [Test]
	    public void Regex()
	    {
			AssertConvertedProgramHasIdenticalOutput(
@"
x = x ;
mylist = x ab) ;
Echo 1 $(mylist:I=$(x)) ;
#Echo 2 $(mylist:I=$\(x\)) ;
#Echo 3 $(mylist:I=\$(x)) ;
#Echo 4 $(mylist:I=\\$(x)) ;
#Echo 5 $(mylist:I=b($$)) ;
#Echo 6 $$\(x) ;
"
			);
	    }

		[Test]
		public void BinaryOperatorPrecedence()
		{
			AssertConvertedProgramHasIdenticalOutput(
@"
#if 1 && 1 { echo 2 ; }
#if 1 && 1 && 1 { echo 3 ; }
#if 1 && 1 && 1 && 1 { echo 4 ; }
#if ( 1 ) && ( 1 ) { }

false = ;

if ( $(false) && $(false) ) { Echo true ; }

if 1 || $(false) && $(false) { Echo true ; }
#if ( 1 || $(false) ) && $(false) { Echo false ; }
"
			);
		}

		[Test]
		public void Parenthesis()
		{
			AssertConvertedProgramHasIdenticalOutput(
@"
# Jam does not handle parenthesis special at all (despite what you would expect in conditional expressions).

# Lines with ## are failing to parse correctly on our parser even though they are valid Jam.

# Lines with ### are parsing correctly but we generate invalid code for them.

Echo (a  b  c) ;
Echo $(a) ;

if (a) {
  Echo a ;
}

if (((b))) {
  Echo b ;
}

isTrue = a ;
isFalse = ;

##if ($(isFalse)) {
##Echo this is false ;
##}
###if $(isFalse)||$(isTrue) { # This is a combine expression, not a boolean.
###Echo this is NOT true ; ## !!!!!!!!
###}
if $(isFalse) || $(isTrue) { # Note whitespace
  Echo this is true ; ## :)
}
if $(isTrue)(a { # This is a combine expression showing parens is not a token at low-level.
  Echo this is true as well ;
}

# The following two are equivalent. Shows that the first case is not a parenthesized expression
# as you'd expect.
##if ($(isFalse) || $(isTrue)) { Echo aa ; }
##if x$(isFalse) || $(isTrue)) { Echo aa ; }

##if $(isFalse) && ($(isFalse) || $(isTrue)) {
##Echo combined ;
##}

##if )( = )( {
##Echo also wat? ;
##}

###if (x != x) {
###Echo wat? ;
###}
###if (x = (x {
###Echo this is what you meant, right? ;
###}

a = foo ;
if $(a)x = foox {
  Echo this one prints ;
}
if ($(a)x = foox) { # Just combine expression! Not parenthesis.
  Echo but this one does not ;
}

# Neither of these is valid even though you'd expect them to if parenthesis are not special.
# So why? Who knows....
#if ( { Echo x ; }
#if ) { Echo x ; }

if () {
  Echo this is just a literal with two characters ;
}

# This does not parse in Jam!
#Echo ) ;
"
			);
		}

		[Test]
		public void VariableExpansionInString()
		{
			AssertConvertedProgramHasIdenticalOutput(
@"
myvar = harry ;
Echo ""bla$(myvar)bla"" ;
Echo ""bla\\$\\(myvar\\)bla"" ; "
			);
		}

	    [Test]
		public void OnTargetVariables()
		{
			AssertConvertedProgramHasIdenticalOutput(
@"
myvar on harry = sally ;
myvar = 3 ;
myothervar = 5 ;
Echo $(myvar) ;

on harry { 
  Echo $(myvar) ;
  Echo $(myothervar) ;
  
  #today we have different semantics for writing to variable that exists on a target in an on block
  #we think and hope that we do not rely on this semantic in our jam program anywhere.
  #myvar = johny ;

  myothervar = 8 ;
}

Echo marker $(myvar) ;
Echo $(myothervar) ;

on harry {
  Echo $(myvar) ;
}

on doesNotExist {
  Echo $(myvar) from doesNotExist ;
}

rule GreenGoblin
{
  Echo FromGreenGoblin ;
  return greengoblin ;
}

Echo luca slucas lucas ;
mytargets = superman spiderman ;
myvar on $(mytargets) = [ GreenGoblin ] ;
myvar on $(mytargets) += uh oh ;

on superman {
  Echo $(myvar) ;
}
on spiderman {
  Echo $(myvar) ;
}

Echo valid ;
myvar = 3 ;
mads = myvar myvar2 ;
$(mads) on mytarget = 2 ;
containsmytarget = mytarget  ;
on $(containsmytarget) { Echo $(myvar) ;  Echo $(myvar2) ; }

");
		}

		[Test]
		public void LiteralExpansion()
		{
			AssertConvertedProgramHasIdenticalOutput(
@"
Echo @(harry:S=.exe) ;

myvar = hello there ;
Echo @($(myvar)/somepath:S=.ini) ;
"
			);
		}


		[Test]
		public void Include()
		{
			var jam1 =
@"
Echo jam1 ;
myvar = hello ;
include file2.jam ;
mydynamicfiles = file3.jam myfile2.jam ;
include $(mydynamicfile) ;
Echo jam1 post ;
";
			var jam2 =
@"
Echo hello $(hello) ;
Echo Jam2 ;
";

			var jam3 =
@"
Echo hello $(hello) ;
Echo Jam3 ;
";

			var jamProgram = new[]
			{
				new SourceFileDescription() { Contents = jam1, FileName = "Jamfile.jam" },
				new SourceFileDescription() { Contents = jam2, FileName = "file2.jam" },
				new SourceFileDescription() { Contents = jam3, FileName = "file3.jam" }
			};
			
			AssertConvertedProgramHasIdenticalOutput(jamProgram);
		}

		private static void AssertConvertedProgramHasIdenticalOutput(string simpleProgram)
	    {
		    AssertConvertedProgramHasIdenticalOutput(new[] {new SourceFileDescription() {FileName = "Jamfile.jam", Contents = simpleProgram}});
	    }

	    private static void AssertConvertedProgramHasIdenticalOutput(SourceFileDescription[] program)
	    {
		    var jamResult = new JamRunner().Run(program).Select(s => s.TrimEnd());
		    Console.WriteLine("Jam:");
		    foreach (var l in jamResult)
			    Console.WriteLine(l);

		    IEnumerable<string> csharpResult = null;

		    try
		    {
			    var csharp = new JamToCSharpConverter().Convert(program);
			    csharpResult =
				    new CSharpRunner().Run(csharp, new[] {JamRunner.ConverterRoot.Combine(new NPath("bin/runtimelib.dll"))})
					    .Select(s => s.TrimEnd());

			    Console.WriteLine("C#:");
			    foreach (var l in csharpResult)
				    Console.WriteLine(l);
			    Console.WriteLine();
		    }
		    catch (Exception e)
		    {
			    Console.WriteLine("Failed converting/running to c#: " + e);
		    }
		    CollectionAssert.AreEqual(jamResult, csharpResult);
	    }
    }
}
