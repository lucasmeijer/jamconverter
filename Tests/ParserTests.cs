using System;
using System.Linq;
using jamconverter.AST;
using NUnit.Framework;

namespace jamconverter
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
            Assert.AreEqual(1, expressionList.Expressions.Length);
            Assert.AreEqual("a", expressionList.Expressions[0].As<LiteralExpression>().Value);
        }

        [Test]
        public void SimpleInvocationWithTwoLiteralArguments()
        {
              var node = ParseStatement<ExpressionStatement>("input a : b ;");
         
            var invocationExpression = (InvocationExpression)node.Expression;

            Assert.AreEqual(2, invocationExpression.Arguments.Length);
            
            Assert.AreEqual("a", invocationExpression.Arguments[0].Expressions[0].As<LiteralExpression>().Value);
            Assert.AreEqual("b", invocationExpression.Arguments[1].Expressions[0].As<LiteralExpression>().Value);
        }

        [Test]
        public void SimpleInvocationWithMultiValueArgument()
        {
            var expressionStatement = ParseStatement<ExpressionStatement>("input a b c ;");

            var invocationExpression = expressionStatement.Expression.As<InvocationExpression>();

            Assert.AreEqual(1, invocationExpression.Arguments.Length);

            var expressionList = invocationExpression.Arguments[0];
            Assert.AreEqual(3, expressionList.Expressions.Length);

            var literalExpressions = expressionList.Expressions.Cast<LiteralExpression>().ToArray();
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
            var assignmentExpression = ParseStatement<ExpressionStatement>("a = b ;").Expression.As<BinaryOperatorExpression>();

            AssertLeftIsA_and_RightIsB(assignmentExpression);
            Assert.AreEqual(Operator.Assignment, assignmentExpression.Operator);
        }

        [Test]
        public void Subtract()
        {
            var assignmentExpression = ParseStatement<ExpressionStatement>("a -= b ;").Expression.As<BinaryOperatorExpression>();

            AssertLeftIsA_and_RightIsB(assignmentExpression);
            Assert.AreEqual(Operator.Subtract, assignmentExpression.Operator);
        }


        private static void AssertLeftIsA_and_RightIsB(BinaryOperatorExpression assignmentExpression)
        {
            var left = (LiteralExpression) assignmentExpression.Left;
            Assert.AreEqual("a", left.Value);

            var right = assignmentExpression.Right;
            Assert.AreEqual(1, right.Expressions.Length);
            Assert.AreEqual("b", right.Expressions[0].As<LiteralExpression>().Value);
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
            var ifStatement = ParseStatement<IfStatement>("if $(somevar) {}");
            Assert.AreEqual(0, ifStatement.Body.Statements.Length);
        }

        [Test]
        public void IfStatementWithBinaryOperatorCondition()
        {
            var ifStatement = ParseStatement<IfStatement>("if $(somevar) = 3 {}");
            Assert.AreEqual(Operator.Assignment, ifStatement.Condition.Operator);
            Assert.AreEqual("3", ifStatement.Condition.Right.Expressions[0].As<LiteralExpression>().Value);

            Assert.AreEqual(0, ifStatement.Body.Statements.Length);
        }

        [Test]
        public void IfStatementWithNegatedCondition()
        {
            var ifStatement = ParseStatement<IfStatement>("if ! $(somevar) {}");
            Assert.IsTrue(ifStatement.Condition.Left is VariableDereferenceExpression);
            Assert.IsTrue(ifStatement.Condition.Negated);
        }

        [Test]
        public void IfStatementWithNegationAndRightSide()
        {
            //jam is crazy.  if you do:
            //
            //myvar = 123 ;
            //if ! $(myvar) = 321 {}
            //
            //that condition is not considered true. the ! does not apply to the equals expression somehow.  for now we're just going to make the parser throw on any case
            //where ! is used with a non trivial condition, and hope our jam program doesn't actually use this construct.

            Assert.Throws<ParsingException>(()=>ParseStatement<IfStatement>("if ! $(somevar) = 321 {}"));
        }

        [Test]
        public void IfWithElseStatement()
        {
            var ifStatement = ParseStatement<IfStatement>("if $(somevar) {} else { Echo ; }");
            Assert.AreEqual("Echo", ifStatement.Else.Statements[0].As<ExpressionStatement>().Expression.As<InvocationExpression>().RuleExpression.As<LiteralExpression>().Value);
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
        public void InvocationExpressionWithBrackets()
        {
            var invocationExpression = ParseExpression<InvocationExpression>("[ MyRule myarg ]");

            Assert.AreEqual("MyRule", invocationExpression.RuleExpression.As<LiteralExpression>().Value);

            Assert.AreEqual(1, invocationExpression.Arguments.Length);
            Assert.AreEqual("myarg", (invocationExpression.Arguments[0].Expressions[0]).As<LiteralExpression>().Value);
        }

        [Test]
        public void ReturnStatement()
        {
            var node = ParseStatement<ReturnStatement>("return 123 ;");

            Assert.AreEqual("123", node.ReturnExpression.Expressions[0].As<LiteralExpression> ().Value);
        }

        [Test]
        public void ReturnStatementWithMultipleValues()
        {
            var returnStatement = ParseStatement<ReturnStatement>("return 123 harry ;");

            var expressions = returnStatement.ReturnExpression.Expressions.Cast<LiteralExpression>().ToArray();
            Assert.AreEqual("123", expressions[0].Value);
            Assert.AreEqual("harry", expressions[1].Value);
        }

        [Test]
        public void AppendOperator()
        {
            var expressionStatement = ParseStatement<ExpressionStatement>("a += 3 ;");
            Assert.IsTrue(expressionStatement.Expression.As<BinaryOperatorExpression>().Operator == Operator.Append);
        }


        [Test]
        public void Comment()
        {
            var expressionStatement = ParseStatement<ExpressionStatement>(
@"#mycomment
a = 3 ;
");
            Assert.IsTrue(expressionStatement.Expression.As<BinaryOperatorExpression>().Operator == Operator.Assignment);
        }

        [Test]
        public void ParseExpressionListTest()
        {
            ExpressionList expressionList = ParseExpressionList("[ MD5 myvalue] harry");
            Assert.AreEqual(2, expressionList.Expressions.Length);
            Assert.IsTrue(expressionList.Expressions[0] is InvocationExpression);
            Assert.IsTrue(expressionList.Expressions[1] is LiteralExpression);
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
            CollectionAssert.AreEqual(new[] { "response"}, actionsDeclarationStatement.Modifiers.Expressions.Cast<LiteralExpression>().Select(le => le.Value));

            Assert.AreEqual(2, actionsDeclarationStatement.Actions.Length);
            Assert.AreEqual("echo something", actionsDeclarationStatement.Actions[0].TrimStart());
            Assert.AreEqual("echo somethingelse", actionsDeclarationStatement.Actions[1].TrimStart());
        }

        [Test]
        public void SingleExpressionCondition()
        {
            var condition = ParseCondition("myliteral");

            Assert.AreEqual("myliteral", condition.Left.As<LiteralExpression>().Value);
            Assert.IsFalse(condition.Negated);
            Assert.IsNull(condition.Right);
        }

        [Test]
        public void EqualsCondition()
        {
            var condition = ParseCondition("myliteral = $(harry)");

            Assert.AreEqual("myliteral", condition.Left.As<LiteralExpression>().Value);
            Assert.IsFalse(condition.Negated);
            Assert.IsTrue(condition.Right.Expressions[0] is VariableDereferenceExpression);
            Assert.AreEqual(Operator.Assignment, condition.Operator );
        }

        [Test]
        public void NegatedCondition()
        {
            var condition = ParseCondition("! $(myvar)");

            Assert.IsTrue(condition.Negated);
            Assert.AreEqual("myvar", condition.Left.As<VariableDereferenceExpression>().VariableExpression.As<LiteralExpression>().Value);
        }

        private static Condition ParseCondition(string jamCode)
        {
            return new Parser(jamCode).ParseCondition();
        }


        [Test]
        public void ExpressionListWithOnLiteral()
        {
            //the tricky part here is that we don't misqualify the "on" as a on keyword like in "myvar on target = bla"
            CollectionAssert.AreEqual(new[] { "I","am","on","else","boat"}, ParseExpressionList("I am on else boat").Expressions.Cast<LiteralExpression>().Select(le => le.Value));
        }

        [Test]
        public void VariableOnTargetAssignment()
        {
            //the tricky part here is that we don't misqualify the "on" as a on keyword like in "myvar on target = bla"
            var variableOnTargetExpression = ParseStatement<ExpressionStatement>("myvar on mytarget = 3 ;").Expression.As<BinaryOperatorExpression>().Left.As<VariableOnTargetExpression>();

            Assert.AreEqual("myvar", variableOnTargetExpression.Variable.As<LiteralExpression>().Value);
            Assert.AreEqual("mytarget", variableOnTargetExpression.Targets.Expressions[0].As<LiteralExpression>().Value);
        }

        [Test]
        public void OnStatement()
        {
            //the tricky part here is that we don't misqualify the "on" as a on keyword like in "myvar on target = bla"
            var onStatement = ParseStatement<OnStatement>("on a b {} ");

            var literals = onStatement.Targets.Expressions.Cast<LiteralExpression>().Select(le => le.Value).ToArray();

            CollectionAssert.AreEqual(new[] { "a", "b"}, literals);
            Assert.AreEqual(0, onStatement.Body.Statements.Length);
        }

        [Test]
        public void WhileStatement()
        {
            var whileStatement = ParseStatement<WhileStatement>("while $(mylist) {} ");
            Assert.IsTrue(whileStatement.Condition.Left is VariableDereferenceExpression);
            Assert.AreEqual(0, whileStatement.Body.Statements.Length);            
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

        static ExpressionList ParseExpressionList(string jamCode) 
        {
            var parser = new Parser(jamCode);
            var expressionList = parser.ParseExpressionList();
            Assert.IsNull(parser.ParseExpression());
            return expressionList;
        }
    }
}
