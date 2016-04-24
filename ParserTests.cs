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
            var node = Parse<ExpressionStatement>("somerule ;");

            var invocationExpression = (InvocationExpression)node.Expression;

            Assert.IsTrue(invocationExpression.RuleExpression is LiteralExpression);
            var literalExpression = invocationExpression.RuleExpression as LiteralExpression;
            Assert.AreEqual("somerule", literalExpression.Value);
        }

        [Test]
        public void SimpleInvocationWithOneLiteralArguments()
        {
            var node = Parse<ExpressionStatement>("input a ;");

            var invocationExpression = (InvocationExpression)node.Expression;

            Assert.AreEqual(1, invocationExpression.Arguments.Length);

            var expressionList = invocationExpression.Arguments[0];

            Assert.AreEqual(1, expressionList.Expressions.Length);
            var arg1 = (LiteralExpression) expressionList.Expressions[0];
            Assert.AreEqual("a", arg1.Value);
        }


        [Test]
        public void SimpleInvocationWithTwoLiteralArguments()
        {
              var node = Parse<ExpressionStatement>("input a : b ;");
         
            var invocationExpression = (InvocationExpression)node.Expression;

            Assert.AreEqual(2, invocationExpression.Arguments.Length);

            var arg1 = (LiteralExpression) invocationExpression.Arguments[0].Expressions[0];
            Assert.AreEqual("a", arg1.Value);

            var arg2 = (LiteralExpression) invocationExpression.Arguments[1].Expressions[0];
            Assert.AreEqual("b", arg2.Value);
        }

        [Test]
        public void SimpleInvocationWithMultiValueArgument()
        {
            var node = Parse<ExpressionStatement>("input a b c ;");

            var invocationExpression = (InvocationExpression)node.Expression;

            Assert.AreEqual(1, invocationExpression.Arguments.Length);

            var expressionListExpression = (ExpressionList) invocationExpression.Arguments[0];
            Assert.AreEqual(3, expressionListExpression.Expressions.Length);
            Assert.AreEqual("a", ((LiteralExpression) expressionListExpression.Expressions[0]).Value);
            Assert.AreEqual("b", ((LiteralExpression)expressionListExpression.Expressions[1]).Value);
            Assert.AreEqual("c", ((LiteralExpression)expressionListExpression.Expressions[2]).Value);
        }

        [Test]
        public void VariableDereference()
        {
            var variableDereferenceExpression = ParseExpression<VariableDereferenceExpression>("$(myvar)");
            Assert.AreEqual("myvar",((LiteralExpression) variableDereferenceExpression.VariableExpression).Value);
        }

        [Test]
        public void NestedVariableDereference()
        {
            var variableDereferenceExpression = ParseExpression<VariableDereferenceExpression>("$($(myvar))");
            var nestedVariableDereferenceExpression = (VariableDereferenceExpression) variableDereferenceExpression.VariableExpression;
            Assert.AreEqual("myvar", ((LiteralExpression)nestedVariableDereferenceExpression.VariableExpression).Value);
        }

        [Test]
        public void VariableDereferenceWithIndexer()
        {
            var variableDereferenceExpression = ParseExpression<VariableDereferenceExpression>("$(myvar[123])");
            Assert.AreEqual("myvar", ((LiteralExpression)variableDereferenceExpression.VariableExpression).Value);
            Assert.AreEqual("123", ((LiteralExpression) variableDereferenceExpression.IndexerExpression).Value);
        }

        [Test]
        public void Assignment()
        {
            var assignmentExpression = (BinaryOperatorExpression)Parse<ExpressionStatement>("a = b ;").Expression;

            var left = (LiteralExpression) assignmentExpression.Left;
            Assert.AreEqual("a", left.Value);

            var right = (ExpressionList) assignmentExpression.Right;
            Assert.AreEqual(1, right.Expressions.Length);
            Assert.AreEqual("b", ((LiteralExpression)right.Expressions[0]).Value);
            Assert.AreEqual(Operator.Assignment, assignmentExpression.Operator);
        }

        [Test]
        public void BlockStatement()
        {
            var blockStatement = Parse<BlockStatement>("{ Echo ; }");

            Assert.AreEqual(1, blockStatement.Statements.Length);

            var invocationExpression =(InvocationExpression) ((ExpressionStatement) blockStatement.Statements[0]).Expression;
            Assert.AreEqual("Echo", ((LiteralExpression)invocationExpression.RuleExpression).Value);
        }


        [Test]
        public void EmptyBlockStatement()
        {
            Assert.AreEqual(0, Parse<BlockStatement>("{ }").Statements.Length);
        }


        [Test]
        public void IfStatement()
        {
            var ifStatement = Parse<IfStatement>("if $(somevar) {}");
            Assert.IsTrue(ifStatement.Condition is VariableDereferenceExpression);
            Assert.AreEqual(0, ifStatement.Body.Statements.Length);
        }

        [Test]
        public void IfStatementWithBinaryOperatorCondition()
        {
            var ifStatement = Parse<IfStatement>("if $(somevar) = 3 {}");
            Assert.IsTrue(ifStatement.Condition is BinaryOperatorExpression);
            var boe = (BinaryOperatorExpression) ifStatement.Condition;

            Assert.AreEqual(Operator.Assignment, boe.Operator);
            Assert.AreEqual("3", ((LiteralExpression)((ExpressionList)  boe.Right).Expressions[0]).Value);

            Assert.AreEqual(0, ifStatement.Body.Statements.Length);
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
            var ruleDeclaration = Parse<RuleDeclaration>("rule myrule arg1 : arg2 { Echo hello ; }");

            Assert.AreEqual("myrule", ruleDeclaration.Name);

            CollectionAssert.AreEqual(new[] { "arg1", "arg2"}, ruleDeclaration.Arguments);

            Assert.AreEqual(1, ruleDeclaration.Body.Statements.Length);
            Assert.IsTrue(ruleDeclaration.Body.Statements[0] is ExpressionStatement);
        }

        [Test]
        public void VariableExpansionModifiers()
        {
            var variableDereferenceExpression = ParseExpression<VariableDereferenceExpression>("$(harry:BS)");

            Assert.AreEqual("harry", ((LiteralExpression) variableDereferenceExpression.VariableExpression).Value);

            Assert.AreEqual(2, variableDereferenceExpression.Modifiers.Length);
            Assert.AreEqual('B', variableDereferenceExpression.Modifiers[0].Command);
            Assert.AreEqual('S', variableDereferenceExpression.Modifiers[1].Command);
        }

        [Test]
        public void VariableExpansionModifiersWithValue()
        {
            var variableDereferenceExpression = ParseExpression<VariableDereferenceExpression>("$(harry:B=value:S)");

            Assert.AreEqual("harry", ((LiteralExpression)variableDereferenceExpression.VariableExpression).Value);

            Assert.AreEqual(2, variableDereferenceExpression.Modifiers.Length);
            Assert.AreEqual('B', variableDereferenceExpression.Modifiers[0].Command);
            Assert.AreEqual("value", ((LiteralExpression)variableDereferenceExpression.Modifiers[0].Value).Value);

            Assert.AreEqual('S', variableDereferenceExpression.Modifiers[1].Command);
            Assert.IsNull(variableDereferenceExpression.Modifiers[1].Value);
        }

        [Test]
        public void VariableExpansionModifiersWithEmptyValue()
        {
            var variableDereferenceExpression = ParseExpression<VariableDereferenceExpression>("$(harry:B=)");

            Assert.AreEqual("harry", ((LiteralExpression)variableDereferenceExpression.VariableExpression).Value);

            Assert.AreEqual(1, variableDereferenceExpression.Modifiers.Length);
            Assert.AreEqual('B', variableDereferenceExpression.Modifiers[0].Command);
            Assert.AreEqual(null, variableDereferenceExpression.Modifiers[0].Value);
        }

        [Test]
        public void VariableExpansionModifiersWithNonLiteralValue()
        {
            var variableDereferenceExpression = ParseExpression<VariableDereferenceExpression>("$(harry:B=$(pietje))");

            Assert.AreEqual("harry", ((LiteralExpression)variableDereferenceExpression.VariableExpression).Value);

            Assert.AreEqual(1, variableDereferenceExpression.Modifiers.Length);
            Assert.AreEqual('B', variableDereferenceExpression.Modifiers[0].Command);

            var value = (VariableDereferenceExpression) variableDereferenceExpression.Modifiers[0].Value;
            Assert.AreEqual("pietje", ((LiteralExpression) value.VariableExpression).Value);
        }

        [Test]
        public void InvocationExpressionWithBrackets()
        {
            var invocationExpression = ParseExpression<InvocationExpression>("[ MyRule myarg ]");

            Assert.AreEqual("MyRule", ((LiteralExpression)invocationExpression.RuleExpression).Value);

            Assert.AreEqual(1, invocationExpression.Arguments.Length);
            Assert.AreEqual("myarg", ((LiteralExpression)((ExpressionList)invocationExpression.Arguments[0]).Expressions[0]).Value);
        }

        [Test]
        public void ReturnStatement()
        {
            var node = Parse<ReturnStatement>("return 123 ;");

            Assert.AreEqual("123", ((LiteralExpression)((ExpressionList)node.ReturnExpression).Expressions[0]).Value);
        }

        [Test]
        public void ReturnStatementWithMultipleValues()
        {
            var returnStatement = Parse<ReturnStatement>("return 123 harry ;");

            var expressions = ((ExpressionList) returnStatement.ReturnExpression).Expressions.OfType<LiteralExpression>().ToArray();
            Assert.AreEqual("123", expressions[0].Value);
            Assert.AreEqual("harry", expressions[1].Value);
        }

        [Test]
        public void AppendOperator()
        {
            var expressionStatement = Parse<ExpressionStatement>("a += 3 ;");
            var assignmentExpression = (BinaryOperatorExpression) expressionStatement.Expression;
            Assert.IsTrue(assignmentExpression.Operator == Operator.Append);
        }

        [Test]
        public void Comment()
        {
            var expressionStatement = Parse<ExpressionStatement>(
@"#mycomment
a = 3 ;
");
            var assignmentExpression = (BinaryOperatorExpression)expressionStatement.Expression;
            Assert.IsTrue(assignmentExpression.Operator == Operator.Assignment);
        }

        [Test]
        public void ParseExpressionListTest()
        {
            ExpressionList expressionList = ParseExpressionList("[ MD5 myvalue] harry");
            Assert.AreEqual(2, expressionList.Expressions.Length);
            Assert.IsTrue(expressionList.Expressions[0] is InvocationExpression);
            Assert.IsTrue(expressionList.Expressions[1] is LiteralExpression);
        }

        static TExpected Parse<TExpected>(string jamcode, ParseMode parseMode = ParseMode.Statement) where TExpected : Node
        {
            var parser = new Parser(jamcode);
            var node = parser.Parse(parseMode);
            Assert.IsNull(parser.Parse(ParseMode.SingleExpression));

            var returnValue = node as TExpected;
            if (returnValue == null)
                throw new ArgumentException($"Expected parser to return type: {typeof(TExpected).Name} but got {node.GetType().Name}");
            return returnValue;
        }

        static TExpected ParseExpression<TExpected>(string jamCode) where TExpected : Expression
        {
            return Parse<TExpected>(jamCode, ParseMode.SingleExpression);
        }

        static ExpressionList ParseExpressionList(string jamCode) 
        {
            var parser = new Parser(jamCode);
            var expressionList = parser.ParseExpressionList();
            Assert.IsNull(parser.Parse(ParseMode.SingleExpression));
            return expressionList;
        }
    }
}
