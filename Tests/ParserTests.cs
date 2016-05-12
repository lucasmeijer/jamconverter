using System;
using System.Collections.Concurrent;
using System.Linq;
using ICSharpCode.NRefactory.CSharp;
using jamconverter.AST;
using NUnit.Framework;
using BinaryOperatorExpression = jamconverter.AST.BinaryOperatorExpression;
using BlockStatement = jamconverter.AST.BlockStatement;
using BreakStatement = jamconverter.AST.BreakStatement;
using ContinueStatement = jamconverter.AST.ContinueStatement;
using Expression = jamconverter.AST.Expression;
using ExpressionStatement = jamconverter.AST.ExpressionStatement;
using ForStatement = jamconverter.AST.ForStatement;
using InvocationExpression = jamconverter.AST.InvocationExpression;
using ReturnStatement = jamconverter.AST.ReturnStatement;
using Statement = jamconverter.AST.Statement;
using SwitchStatement = jamconverter.AST.SwitchStatement;
using WhileStatement = jamconverter.AST.WhileStatement;

namespace jamconverter.Tests
{
    [TestFixture]
    public class ParserTests
    {
        [Test]
        public void SimpleInvocationExpression()
        {
            var expressionStatement = ParseStatement<ExpressionStatement>("somerule ;");
            var invocationExpression = expressionStatement.Expression.As<InvocationExpression>();
            Assert.AreEqual("somerule", invocationExpression.RuleExpression.As<LiteralExpression>().Value);
        }

        [Test]
        public void SimpleInvocationWithOneLiteralArguments()
        {
            var node = ParseStatement<ExpressionStatement>("input a ;");

            var invocationExpression = node.Expression.As<InvocationExpression>();

            Assert.AreEqual(1, invocationExpression.Arguments.Length);

            var expressionList = invocationExpression.Arguments[0];
            Assert.AreEqual(1, expressionList.Length);
            Assert.AreEqual("a", expressionList[0].As<LiteralExpression>().Value);
        }

        [Test]
        public void SimpleInvocationWithTwoLiteralArguments()
        {
              var node = ParseStatement<ExpressionStatement>("input a : b ;");
         
            var invocationExpression = (InvocationExpression)node.Expression;

            Assert.AreEqual(2, invocationExpression.Arguments.Length);
            
            Assert.AreEqual("a", invocationExpression.Arguments[0][0].As<LiteralExpression>().Value);
            Assert.AreEqual("b", invocationExpression.Arguments[1][0].As<LiteralExpression>().Value);
        }

        [Test]
        public void SimpleInvocationWithMultiValueArgument()
        {
            var expressionStatement = ParseStatement<ExpressionStatement>("input a b c ;");

            var invocationExpression = expressionStatement.Expression.As<InvocationExpression>();

            Assert.AreEqual(1, invocationExpression.Arguments.Length);

            var expressionList = invocationExpression.Arguments[0];
            Assert.AreEqual(3, expressionList.Length);

            var literalExpressions = expressionList.Cast<LiteralExpression>().ToArray();
            Assert.AreEqual("a", literalExpressions[0].Value);
            Assert.AreEqual("b", literalExpressions[1].Value);
            Assert.AreEqual("c", literalExpressions[2].Value);
        }

        [Test]
        public void VariableDereference()
        {
            var variableDereferenceExpression = ParseExpression<VariableDereferenceExpression>("$(myvar)");
            Assert.AreEqual("myvar",variableDereferenceExpression.VariableExpression.As<LiteralExpression>().Value);
        }

        [Test]
        public void NestedVariableDereference()
        {
            var variableDereferenceExpression = ParseExpression<VariableDereferenceExpression>("$($(myvar))");
            var nestedVariableDereferenceExpression = variableDereferenceExpression.VariableExpression.As<VariableDereferenceExpression>();
            Assert.AreEqual("myvar", nestedVariableDereferenceExpression.VariableExpression.As<LiteralExpression>().Value);
        }

        [Test]
        public void VariableDereferenceWithIndexer()
        {
            var variableDereferenceExpression = ParseExpression<VariableDereferenceExpression>("$(myvar[123])");
            Assert.AreEqual("myvar", variableDereferenceExpression.VariableExpression.As<LiteralExpression>().Value);
            Assert.AreEqual("123", variableDereferenceExpression.IndexerExpression.As<LiteralExpression>().Value);
        }

        [Test]
        public void Assignment()
        {
            var assignmentExpression = ParseStatement<AssignmentStatement>("a = b ;");

            AssertLeftIsA_and_RightIsB(assignmentExpression);
            Assert.AreEqual(Operator.Assignment, assignmentExpression.Operator);
        }

		[Test]
		public void AssignmentIfEmpty()
		{
			var assignmentExpression = ParseStatement<AssignmentStatement>("a ?= b ;");

			AssertLeftIsA_and_RightIsB(assignmentExpression);
			Assert.AreEqual(Operator.AssignmentIfEmpty, assignmentExpression.Operator);
		}


		[Test]
        public void Subtract()
        {
            var assignmentStatement = ParseStatement<AssignmentStatement>("a -= b ;");

            AssertLeftIsA_and_RightIsB(assignmentStatement);
            Assert.AreEqual(Operator.Subtract, assignmentStatement.Operator);
        }


        private static void AssertLeftIsA_and_RightIsB(AssignmentStatement assignmentStatement)
        {
            var left = (LiteralExpression)assignmentStatement.Left;
            Assert.AreEqual("a", left.Value);

            var right = assignmentStatement.Right;
            Assert.AreEqual(1, right.Length);
            Assert.AreEqual("b", right[0].As<LiteralExpression>().Value);
        }

        [Test]
        public void BlockStatement()
        {
            var blockStatement = ParseStatement<BlockStatement>("{ Echo ; }");

            Assert.AreEqual(1, blockStatement.Statements.Length);

            var invocationExpression = blockStatement.Statements[0].As<ExpressionStatement>().Expression.As<InvocationExpression>();
            Assert.AreEqual("Echo", invocationExpression.RuleExpression.As<LiteralExpression>().Value);
        }
        
        [Test]
        public void EmptyBlockStatement()
        {
            Assert.AreEqual(0, ParseStatement<BlockStatement>("{ }").Statements.Length);
        }
        
        [Test]
        public void IfStatement()
        {
            var ifStatement = ParseStatement<IfStatement>("if $(somevar) { }");
            Assert.AreEqual(0, ifStatement.Body.Statements.Length);

            Assert.AreEqual(ifStatement, ifStatement.Body.Parent);
        }
		
	    [Test]
	    public void IfStatementWithAddOperator()
	    {
		    var ifStatement = ParseStatement<IfStatement>("if $(a) && $(b) { }");
		    Assert.AreEqual(Operator.And, ifStatement.Condition.As<BinaryOperatorExpression>().Operator);
		    AssertLeftIsA_And_RightIsB(ifStatement);
	    }

	    private static void AssertLeftIsA_And_RightIsB(IfStatement ifStatement)
	    {
		    Assert.AreEqual("a",
			    ifStatement.Condition.As<BinaryOperatorExpression>().Left.As<VariableDereferenceExpression>().VariableExpression.As<LiteralExpression>().Value);
		    Assert.AreEqual("b",
			    ifStatement.Condition.As<BinaryOperatorExpression>().Right[0].As<VariableDereferenceExpression>().VariableExpression.As<LiteralExpression>().Value);
	    }

	    [Test]
		public void IfStatementWithOrOperator()
		{
			var ifStatement = ParseStatement<IfStatement>("if $(a) || $(b) { }");
			Assert.AreEqual(Operator.Or, ifStatement.Condition.As<BinaryOperatorExpression>().Operator);
		    Assert.AreEqual("a", ifStatement.Condition.As<BinaryOperatorExpression>().Left.As<VariableDereferenceExpression>().VariableExpression.As<LiteralExpression>().Value);
		    Assert.AreEqual("b", ifStatement.Condition.As<BinaryOperatorExpression>().Right[0].As<VariableDereferenceExpression>().VariableExpression.As<LiteralExpression>().Value);
		}

		[Test]
		public void IfStatementWithNotEqualOperator()
		{
			var ifStatement = ParseStatement<IfStatement>("if $(a) != $(b) { }");
			Assert.AreEqual(Operator.NotEqual, ifStatement.Condition.As<BinaryOperatorExpression>().Operator);
			AssertLeftIsA_And_RightIsB(ifStatement);
		}

		[Test]
        public void IfStatementWithBinaryOperatorCondition()
        {
            var ifStatement = ParseStatement<IfStatement>("if $(somevar) = 3 { }");
            Assert.AreEqual(Operator.Assignment, ifStatement.Condition.As<BinaryOperatorExpression>().Operator);
            Assert.AreEqual("3", ifStatement.Condition.As<BinaryOperatorExpression>().Right[0].As<LiteralExpression>().Value);

            Assert.AreEqual(0, ifStatement.Body.Statements.Length);
        }

	    [Test]
        public void IfStatementWithNegatedCondition()
        {
            var ifStatement = ParseStatement<IfStatement>("if ! $(somevar) { }");
            Assert.IsTrue(ifStatement.Condition.As<NotOperatorExpression>().Expression is VariableDereferenceExpression);
        }

        [Test]
		[Ignore("investigate failure after parenthesis support")]
        public void IfStatementWithNegationAndRightSide()
        {
            //jam is crazy.  if you do:
            //
            //myvar = 123 ;
            //if ! $(myvar) = 321 { }
            //
            //that condition is not considered true. the ! does not apply to the equals expression somehow.  for now we're just going to make the parser throw on any case
            //where ! is used with a non trivial condition, and hope our jam program doesn't actually use this construct.

            Assert.Throws<ParsingException>(()=>ParseStatement<IfStatement>("if ! $(somevar) = 321 { }"));
        }

        [Test]
        public void IfWithElseStatement()
        {
            var ifStatement = ParseStatement<IfStatement>("if $(somevar) { } else { Echo ; }");
            Assert.AreEqual("Echo", ifStatement.Else.As<BlockStatement>().Statements[0].As<ExpressionStatement>().Expression.As<InvocationExpression>().RuleExpression.As<LiteralExpression>().Value);
        }

        [Test]
        public void IfWithElseStatementThatsNotBlockStatement()
        {
            var ifStatement = ParseStatement<IfStatement>("if $(somevar) { } else Echo ;");
            Assert.AreEqual("Echo", ifStatement.Else.As<ExpressionStatement>().Expression.As<InvocationExpression>().RuleExpression.As<LiteralExpression>().Value);
        }

		[Test]
		public void BinaryOperatorExpression()
		{
			ParseCondition<BinaryOperatorExpression>("a = b");
			ParseCondition<BinaryOperatorExpression>("a != b");
			ParseCondition<BinaryOperatorExpression>("a in b");
			ParseCondition<BinaryOperatorExpression>("a > b");
			ParseCondition<BinaryOperatorExpression>("a < b");
		}

		[Test]
        public void CombineExpression()
        {
            var combineExpression = ParseExpression<CombineExpression>("$(b)c$(d)");

            Assert.AreEqual(3, combineExpression.Elements.Length);
            Assert.IsTrue(combineExpression.Elements[0] is VariableDereferenceExpression);
            Assert.IsTrue(combineExpression.Elements[1] is LiteralExpression);
            Assert.IsTrue(combineExpression.Elements[2] is VariableDereferenceExpression);
        }

	    [Test]
	    public void IncludeStatement()
	    {
			var includeStatement = ParseStatement<IncludeStatement>("include myfile ;");
			Assert.AreEqual("myfile", includeStatement.Expression.As<LiteralExpression>().Value);
		}
        
        [Test]
        public void RuleDeclaration()
        {
            var ruleDeclaration = ParseStatement<RuleDeclarationStatement>("rule myrule arg1 : arg2 { Echo hello ; }");

            Assert.AreEqual("myrule", ruleDeclaration.Name);

            CollectionAssert.AreEqual(new[] { "arg1", "arg2"}, ruleDeclaration.Arguments);

            Assert.AreEqual(1, ruleDeclaration.Body.Statements.Length);
            Assert.IsTrue(ruleDeclaration.Body.Statements[0] is ExpressionStatement);
        }

        [Test]
        public void VariableExpansionModifiers()
        {
            var variableDereferenceExpression = ParseExpression<VariableDereferenceExpression>("$(harry:BS)");

            Assert.AreEqual("harry", variableDereferenceExpression.VariableExpression.As<LiteralExpression>().Value);

            Assert.AreEqual(2, variableDereferenceExpression.Modifiers.Length);
            Assert.AreEqual('B', variableDereferenceExpression.Modifiers[0].Command);
            Assert.AreEqual('S', variableDereferenceExpression.Modifiers[1].Command);
        }

        [Test]
        public void VariableExpansionModifiersWithValue()
        {
            var variableDereferenceExpression = ParseExpression<VariableDereferenceExpression>("$(harry:B=value:S)");

            Assert.AreEqual("harry", variableDereferenceExpression.VariableExpression.As<LiteralExpression>().Value);

            Assert.AreEqual(2, variableDereferenceExpression.Modifiers.Length);
            Assert.AreEqual('B', variableDereferenceExpression.Modifiers[0].Command);
            Assert.AreEqual("value", variableDereferenceExpression.Modifiers[0].Value.As<LiteralExpression>().Value);

            Assert.AreEqual('S', variableDereferenceExpression.Modifiers[1].Command);
            Assert.IsNull(variableDereferenceExpression.Modifiers[1].Value);
        }

		[Test]
        public void VariableExpansionModifiersWithEmptyValue()
        {
            var variableDereferenceExpression = ParseExpression<VariableDereferenceExpression>("$(harry:B=)");

            Assert.AreEqual("harry", variableDereferenceExpression.VariableExpression.As<LiteralExpression>().Value);

            Assert.AreEqual(1, variableDereferenceExpression.Modifiers.Length);
            Assert.AreEqual('B', variableDereferenceExpression.Modifiers[0].Command);
            Assert.AreEqual(null, variableDereferenceExpression.Modifiers[0].Value);
        }

        [Test]
        public void VariableExpansionModifiersWithNonLiteralValue()
        {
            var variableDereferenceExpression = ParseExpression<VariableDereferenceExpression>("$(harry:B=$(pietje))");

            Assert.AreEqual("harry", variableDereferenceExpression.VariableExpression.As<LiteralExpression>().Value);

            Assert.AreEqual(1, variableDereferenceExpression.Modifiers.Length);
            Assert.AreEqual('B', variableDereferenceExpression.Modifiers[0].Command);

            var value = variableDereferenceExpression.Modifiers[0].Value.As<VariableDereferenceExpression>();
            Assert.AreEqual("pietje", (value.VariableExpression).As<LiteralExpression>().Value);
        }

	    [Test]
	    public void VariableExpansionModifierWithRegex()
	    {
		    //var variableDereferenceExpression
	    }

        [Test]
        public void InvocationExpressionWithBrackets()
        {
            var invocationExpression = ParseExpression<InvocationExpression>("[ MyRule myarg ]");

            Assert.AreEqual("MyRule", invocationExpression.RuleExpression.As<LiteralExpression>().Value);

            Assert.AreEqual(1, invocationExpression.Arguments.Length);
            Assert.AreEqual("myarg", (invocationExpression.Arguments[0][0]).As<LiteralExpression>().Value);
        }

        [Test]
        public void ReturnStatement()
        {
            var node = ParseStatement<ReturnStatement>("return 123 ;");

            Assert.AreEqual("123", node.ReturnExpression[0].As<LiteralExpression> ().Value);
        }

        [Test]
        public void ReturnStatementWithMultipleValues()
        {
            var returnStatement = ParseStatement<ReturnStatement>("return 123 harry ;");

            var expressions = returnStatement.ReturnExpression.Cast<LiteralExpression>().ToArray();
            Assert.AreEqual("123", expressions[0].Value);
            Assert.AreEqual("harry", expressions[1].Value);
        }

        [Test]
        public void AppendOperator()
        {
            var assignmentStatement = ParseStatement<AssignmentStatement>("a += 3 ;");
            Assert.IsTrue(assignmentStatement.Operator == Operator.Append);
        }


        [Test]
        public void Comment()
        {
            var assignmentStatement = ParseStatement<AssignmentStatement>(
@"#mycomment
a = 3 ;
");
            Assert.IsTrue(assignmentStatement.Operator == Operator.Assignment);
        }

        [Test]
        public void ParseExpressionListTest()
        {
            var expressionList = ParseExpressionList("[ MD5 myvalue] harry");
            Assert.AreEqual(2, expressionList.Length);
            Assert.IsTrue(expressionList[0] is InvocationExpression);
            Assert.IsTrue(expressionList[1] is LiteralExpression);
        }


        [Test]
        public void ActionsDeclarationStatement()
        {
            ActionsDeclarationStatement actionsDeclarationStatement = ParseStatement<ActionsDeclarationStatement>(@"
actions response myactionname
{
    echo something
    echo somethingelse
}");
            Assert.AreEqual("myactionname", actionsDeclarationStatement.Name);
            CollectionAssert.AreEqual(new[] { "response"}, actionsDeclarationStatement.Modifiers.Cast<LiteralExpression>().Select(le => le.Value));

            Assert.AreEqual(2, actionsDeclarationStatement.Actions.Length);
            Assert.AreEqual("echo something", actionsDeclarationStatement.Actions[0].TrimStart());
            Assert.AreEqual("echo somethingelse", actionsDeclarationStatement.Actions[1].TrimStart());
        }
		
      
        [Test]
        public void InCondition()
        {
            var condition = ParseCondition<BinaryOperatorExpression>("$(myvar) in MAC PC WINDOWS");

            Assert.AreEqual("myvar", condition.Left.As<VariableDereferenceExpression>().VariableExpression.As<LiteralExpression>().Value);
            Assert.AreEqual(Operator.In, condition.Operator);
            Assert.AreEqual(3, condition.Right.Length);
        }

	    [Test]
        public void SwitchStatement()
        {
            var switchStatement = ParseStatement<SwitchStatement>(
@"switch $(myvar) {
  case a :
     hello ;
  case b :
     there ;
     sailor ;
}" 
);
            Assert.AreEqual("myvar", switchStatement.Variable.As<VariableDereferenceExpression>().VariableExpression.As<LiteralExpression>().Value);

            Assert.AreEqual(2, switchStatement.Cases.Length);

            var case0 = switchStatement.Cases[0];
            Assert.AreEqual("a", case0.CaseExpression.Value);
            Assert.AreEqual(1, case0.Statements.Length);
            AssertIsRuleInvocationWithName(case0.Statements[0], "hello");

            var case1 = switchStatement.Cases[1];
            Assert.AreEqual("b", case1.CaseExpression.Value);
            Assert.AreEqual(2, case1.Statements.Length);

            AssertIsRuleInvocationWithName(case1.Statements[0], "there");
            AssertIsRuleInvocationWithName(case1.Statements[1], "sailor");            
        }

        private static void AssertIsRuleInvocationWithName(Statement statement, string ruleName)
        {
            Assert.AreEqual(ruleName, statement.As<ExpressionStatement>().Expression.As<InvocationExpression>().RuleExpression.As<LiteralExpression>().Value);
        }

        private static T ParseCondition<T>(string jamCode) where T : Expression
        {
	        return new Parser(jamCode).ParseCondition().As<T>();
        }


        [Test]
        public void ExpressionListWithOnLiteral()
        {
            //the tricky part here is that we don't misqualify the "on" as a on keyword like in "myvar on target = bla"
            CollectionAssert.AreEqual(new[] { "I","am","on","else","boat"}, ParseExpressionList("I am on else boat").Cast<LiteralExpression>().Select(le => le.Value));
        }

        [Test]
        public void VariableOnTargetAssignment()
        {
            //the tricky part here is that we don't misqualify the "on" as a on keyword like in "myvar on target = bla"
            var assignmentStatment = ParseStatement<AssignmentStatement>("myvar on mytarget = 3 ;").Left.As<VariableOnTargetExpression>();

            Assert.AreEqual("myvar", assignmentStatment.Variable.As<LiteralExpression>().Value);
            Assert.AreEqual("mytarget", assignmentStatment.Targets[0].As<LiteralExpression>().Value);
        }

		[Test]
		public void VariableOnTargetBeforeMaybeAssignment()
		{
			//the tricky part here is that we don't misqualify the "on" as a on keyword like in "myvar on target = bla"
			var variableOnTarget = ParseStatement<AssignmentStatement>("myvar on mytarget ?= 3 ;").Left.As<VariableOnTargetExpression>();
			Assert.AreEqual("mytarget", variableOnTarget.Targets.Single().As<LiteralExpression>().Value);
		}

	    [Test]
        public void OnStatement()
        {
            //the tricky part here is that we don't misqualify the "on" as a on keyword like in "myvar on target = bla"
            var onStatement = ParseStatement<OnStatement>("on a { } ");
            
            Assert.AreEqual("a", onStatement.Target.As<LiteralExpression>().Value);
            
            Assert.AreEqual(0, onStatement.Body.As<BlockStatement>().Statements.Length);
        }

        [Test]
        public void OnStatementWithoutBlockStatement()
        {
            //the tricky part here is that we don't misqualify the "on" as a on keyword like in "myvar on target = bla"
            var onStatement = ParseStatement<OnStatement>("on $(TARGET) linkFlags += -shared - fPIC ; ");

            Assert.AreEqual("TARGET", onStatement.Target.As<VariableDereferenceExpression>().VariableExpression.As<LiteralExpression>().Value);
            onStatement.Body.As<AssignmentStatement>();
        }

        [Test]
        public void WhileStatement()
        {
            var whileStatement = ParseStatement<WhileStatement>("while $(mylist) { } ");
            Assert.IsTrue(whileStatement.Condition is VariableDereferenceExpression);
            Assert.AreEqual(0, whileStatement.Body.Statements.Length);            
        }

        [Test]
        public void ForLoop()
        {
            var forStatement = ParseStatement<ForStatement>("for myvar in $(mylist) { } ");
            Assert.AreEqual("myvar",forStatement.LoopVariable.As<LiteralExpression>().Value);
            Assert.IsTrue(forStatement.List[0] is VariableDereferenceExpression);
            Assert.AreEqual(0, forStatement.Body.Statements.Length);
        }

        [Test]
        public void BreakStatement()
        {
            ParseStatement<BreakStatement>("break ;");
        }

        [Test]
        public void ContinueStatement()
        {
            ParseStatement<ContinueStatement>("continue ;");
        }

        [Test]
        public void DoubleDereference()
        {
            var deref = ParseExpression<VariableDereferenceExpression>("$($(myvar))");

            Assert.AreEqual("myvar", deref.VariableExpression.As<VariableDereferenceExpression>().VariableExpression.As<LiteralExpression>().Value);
        }

        [Test]
        public void AssignToDynamicVar()
        {
            var assignmentStatement = ParseStatement<AssignmentStatement>("$(myvar) = 1 2 3 ;");
            Assert.AreEqual("myvar", assignmentStatement.Left.As<VariableDereferenceExpression>().VariableExpression.As<LiteralExpression>().Value);
        }

		[Test]
		public void LocalVariableDeclaration()
		{
			var localStatement = ParseStatement<LocalStatement>("local a ;");
			Assert.AreEqual("a", localStatement.Variable.Value);
			Assert.AreEqual(0, localStatement.Value.Length);
		}

		[Test]
		public void LocalVariableDeclaration2()
		{
			var localStatement = ParseStatement<IfStatement>(@"if [ GetListCount ] > 1 { }");
		}
		
		[Test]
		public void LocalVariableDeclarationWithValue()
		{
			var localStatement = ParseStatement<LocalStatement>("local a = 3 ;");
			Assert.AreEqual("a", localStatement.Variable.Value);
			Assert.AreEqual("3", localStatement.Value[0].As<LiteralExpression>().Value);
		}

	    [Test]
	    public void RuleInvocationWithArgumentContainingColon()
	    {
		    var expressionStatement = ParseStatement<ExpressionStatement>("MyRule my:funky:arg0 ;");
		    var invocationExpression = expressionStatement.Expression.As<InvocationExpression>();

			Assert.AreEqual(1, invocationExpression.Arguments.Length);
			Assert.AreEqual("my:funky:arg0", invocationExpression.Arguments[0].Single().As<LiteralExpression>().Value);
	    }

	    [Test]
	    public void LiteralExpansionExpression()
	    {
		    var result = ParseExpression<LiteralExpansionExpression>("@(myname:S=exe)");
			Assert.AreEqual("myname", result.VariableExpression.As<LiteralExpression>().Value);
	    }

		[Test]
		public void ForwardToBackslashExpansionModifier()
		{
			var jam = @"$(winRTLegacyDll:\\\\:C) ;";
			ParseExpression<VariableDereferenceExpression>(jam);
		}

		static TExpected ParseStatement<TExpected>(string jamCode) where TExpected : Statement
        {
            var parser = new Parser(jamCode);
            var node = parser.ParseStatement();
            Assert.IsNull(parser.ParseExpression());
            var returnValue = node as TExpected;
            if (returnValue == null)
                throw new ArgumentException($"Expected parser to return type: {typeof(TExpected).Name} but got {node.GetType().Name}");
            return returnValue;
        }


        static TExpected ParseExpression<TExpected>(string jamCode) where TExpected : Expression
        {
            var parser = new Parser(jamCode);
            var node = parser.ParseExpression();
            Assert.IsNull(parser.ParseExpression());
            var returnValue = node as TExpected;
            if (returnValue == null)
                throw new ArgumentException($"Expected parser to return type: {typeof(TExpected).Name} but got {node.GetType().Name}");
            return returnValue;
        }

        static NodeList<Expression> ParseExpressionList(string jamCode) 
        {
            var parser = new Parser(jamCode);
            var expressionList = parser.ParseExpressionList();
            Assert.IsNull(parser.ParseExpression());
            return expressionList;
        }
    }
}
