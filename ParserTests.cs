using System.Linq;
using jamconverter.AST;
using NUnit.Framework;

namespace jamconverter
{
    [TestFixture()]
    public class ParserTests
    {
        [Test]
        public void SimpleInvocationExpression()
        {
            var parser = new Parser("somerule ;");
            var node = parser.Parse();
           
            var invocationExpression = (InvocationExpression)((ExpressionStatement)node).Expression;

            Assert.IsTrue(invocationExpression.RuleExpression is LiteralExpression);
            var literalExpression = invocationExpression.RuleExpression as LiteralExpression;
            Assert.AreEqual("somerule", literalExpression.Value);

            Assert.IsNull(parser.Parse());
        }

        [Test]
        public void SimpleInvocationWithOneLiteralArguments()
        {
            var parser = new Parser("input a ;");
            var node = parser.Parse();

            var invocationExpression = (InvocationExpression)((ExpressionStatement)node).Expression;

            Assert.AreEqual(1, invocationExpression.Arguments.Length);

            var expressionListExpression = (ExpressionListExpression) invocationExpression.Arguments[0];

            Assert.AreEqual(1, expressionListExpression.Expressions.Length);
            var arg1 = (LiteralExpression) expressionListExpression.Expressions[0];
            Assert.AreEqual("a", arg1.Value);
        }


        [Test]
        public void SimpleInvocationWithTwoLiteralArguments()
        {
            var parser = new Parser("input a : b ;");
            var node = parser.Parse();

            var invocationExpression = (InvocationExpression)((ExpressionStatement)node).Expression;

            Assert.AreEqual(2, invocationExpression.Arguments.Length);

            var arg1 = (LiteralExpression) ((ExpressionListExpression) invocationExpression.Arguments[0]).Expressions[0];
            Assert.AreEqual("a", arg1.Value);

            var arg2 = (LiteralExpression) ((ExpressionListExpression) invocationExpression.Arguments[1]).Expressions[0];
            Assert.AreEqual("b", arg2.Value);
        }

        [Test]
        public void SimpleInvocationWithMultiValueArgument()
        {
            var parser = new Parser("input a b c ;");
            var node = parser.Parse();

            var invocationExpression = (InvocationExpression)((ExpressionStatement)node).Expression;

            Assert.AreEqual(1, invocationExpression.Arguments.Length);

            var expressionListExpression = (ExpressionListExpression) invocationExpression.Arguments[0];
            Assert.AreEqual(3, expressionListExpression.Expressions.Length);
            Assert.AreEqual("a", ((LiteralExpression) expressionListExpression.Expressions[0]).Value);
            Assert.AreEqual("b", ((LiteralExpression)expressionListExpression.Expressions[1]).Value);
            Assert.AreEqual("c", ((LiteralExpression)expressionListExpression.Expressions[2]).Value);
        }

        [Test]
        public void VariableDereference()
        {
            var parser = new Parser("input $(myvar) ;");
            var node = parser.Parse();

            var invocationExpression = (InvocationExpression)((ExpressionStatement)node).Expression;
            Assert.AreEqual(1, invocationExpression.Arguments.Length);

            var variableDereferenceExpression =
                (VariableDereferenceExpression)
                    ((ExpressionListExpression) invocationExpression.Arguments[0]).Expressions[0];
            Assert.AreEqual("myvar",((LiteralExpression) variableDereferenceExpression.VariableExpression).Value);
        }

        [Test]
        public void NestedVariableDereference()
        {
            var parser = new Parser("input $($(myvar)) ;");
            var node = parser.Parse();

            var invocationExpression = (InvocationExpression)((ExpressionStatement)node).Expression;
            Assert.AreEqual(1, invocationExpression.Arguments.Length);

            var variableDereferenceExpression =
                (VariableDereferenceExpression)
                    ((ExpressionListExpression) invocationExpression.Arguments[0]).Expressions[0];

            var nestedVariableDereferenceExpression = (VariableDereferenceExpression) variableDereferenceExpression.VariableExpression;

            Assert.AreEqual("myvar", ((LiteralExpression)nestedVariableDereferenceExpression.VariableExpression).Value);
        }

        [Test]
        public void Assignment()
        {
            var parser = new Parser("a = b ;");
            var node = parser.Parse();

            var assignmentExpression = (AssignmentExpression)((ExpressionStatement)node).Expression;

            var left = (LiteralExpression) assignmentExpression.Left;
            Assert.AreEqual("a", left.Value);

            var right = (ExpressionListExpression) assignmentExpression.Right;
            Assert.AreEqual(1, right.Expressions.Length);
            Assert.AreEqual("b", ((LiteralExpression)right.Expressions[0]).Value);
            Assert.AreEqual(Operator.Assignment, assignmentExpression.Operator);
        }
        
        [Test]
        public void BlockStatement()
        {
            var parser = new Parser("{ Echo ; }");
            var node = parser.Parse();

            var blockStatement = (BlockStatement) node;

            Assert.AreEqual(1, blockStatement.Statements.Length);

            var invocationExpression =(InvocationExpression) ((ExpressionStatement) blockStatement.Statements[0]).Expression;
            Assert.AreEqual("Echo", ((LiteralExpression)invocationExpression.RuleExpression).Value);
            Assert.IsNull(parser.Parse());
        }


        [Test]
        public void EmptyBlockStatement()
        {
            var parser = new Parser("{ }");
            var node = parser.Parse();

            var blockStatement = (BlockStatement)node;

            Assert.AreEqual(0, blockStatement.Statements.Length);
            Assert.IsNull(parser.Parse());
        }


        [Test]
        public void IfStatement()
        {
            var parser = new Parser("if $(somevar) {}");
            var ifStatement = (IfStatement) parser.Parse();
            Assert.IsTrue(ifStatement.Condition is VariableDereferenceExpression);
            Assert.AreEqual(0, ifStatement.Body.Statements.Length);
            Assert.IsNull(parser.Parse());
        }

        [Test]
        public void TwoStatements()
        {
            var parser = new Parser("myvar = 123 ; Echo $(myvar); ");
            parser.Parse();
        }

        [Test]
        public void CombineExpression()
        {
            var parser = new Parser("a = $(b)c$(d) ;");
            var node = parser.Parse();

            var right = ((AssignmentExpression) ((ExpressionStatement) node).Expression).Right;

            var combineExpression = (CombineExpression) ((ExpressionListExpression) right).Expressions[0];
            
            Assert.AreEqual(3, combineExpression.Elements.Length);
            Assert.IsTrue(combineExpression.Elements[0] is VariableDereferenceExpression);
            Assert.IsTrue(combineExpression.Elements[1] is LiteralExpression);
            Assert.IsTrue(combineExpression.Elements[2] is VariableDereferenceExpression);

            Assert.IsNull(parser.Parse());
        }


        [Test]
        public void RuleDeclaration()
        {
            var parser = new Parser("rule myrule arg1 : arg2 { Echo hello ; }");
            var node = parser.Parse();

            Assert.IsTrue(node is RuleDeclaration);
            var ruleDeclaration = (RuleDeclaration)node;

            Assert.AreEqual("myrule", ruleDeclaration.Name);

            CollectionAssert.AreEqual(new[] { "arg1", "arg2"}, ruleDeclaration.Arguments);

            Assert.AreEqual(1, ruleDeclaration.Body.Statements.Length);
            Assert.IsTrue(ruleDeclaration.Body.Statements[0] is ExpressionStatement);

            Assert.IsNull(parser.Parse());
        }

        [Test]
        public void VariableExpansionModifiers()
        {
            var parser = new Parser("$(harry:BS)");
            var node = parser.Parse(ParseMode.SingleExpression);

            var variableDereferenceExpression = (VariableDereferenceExpression)node;

            Assert.AreEqual("harry", ((LiteralExpression) variableDereferenceExpression.VariableExpression).Value);

            Assert.AreEqual(2, variableDereferenceExpression.Modifiers.Length);
            Assert.AreEqual('B', variableDereferenceExpression.Modifiers[0].Command);
            Assert.AreEqual('S', variableDereferenceExpression.Modifiers[1].Command);

            Assert.IsNull(parser.Parse());
        }

        [Test]
        public void VariableExpansionModifiersWithValue()
        {
            var parser = new Parser("$(harry:B=value:S)");
            var node = parser.Parse(ParseMode.SingleExpression);

            var variableDereferenceExpression = (VariableDereferenceExpression)node;

            Assert.AreEqual("harry", ((LiteralExpression)variableDereferenceExpression.VariableExpression).Value);

            Assert.AreEqual(2, variableDereferenceExpression.Modifiers.Length);
            Assert.AreEqual('B', variableDereferenceExpression.Modifiers[0].Command);
            Assert.AreEqual("value", ((LiteralExpression)variableDereferenceExpression.Modifiers[0].Value).Value);

            Assert.AreEqual('S', variableDereferenceExpression.Modifiers[1].Command);
            Assert.IsNull(variableDereferenceExpression.Modifiers[1].Value);

            Assert.IsNull(parser.Parse());
        }

        [Test]
        public void VariableExpansionModifiersWithEmptyValue()
        {
            var parser = new Parser("$(harry:B=)");
            var node = parser.Parse(ParseMode.SingleExpression);

            var variableDereferenceExpression = (VariableDereferenceExpression)node;

            Assert.AreEqual("harry", ((LiteralExpression)variableDereferenceExpression.VariableExpression).Value);

            Assert.AreEqual(1, variableDereferenceExpression.Modifiers.Length);
            Assert.AreEqual('B', variableDereferenceExpression.Modifiers[0].Command);
            Assert.AreEqual(null, variableDereferenceExpression.Modifiers[0].Value);

            Assert.IsNull(parser.Parse());
        }

        [Test]
        public void VariableExpansionModifiersWithNonLiteralValue()
        {
            var parser = new Parser("$(harry:B=$(pietje))");
            var node = parser.Parse(ParseMode.SingleExpression);

            var variableDereferenceExpression = (VariableDereferenceExpression)node;

            Assert.AreEqual("harry", ((LiteralExpression)variableDereferenceExpression.VariableExpression).Value);

            Assert.AreEqual(1, variableDereferenceExpression.Modifiers.Length);
            Assert.AreEqual('B', variableDereferenceExpression.Modifiers[0].Command);

            var value = ((VariableDereferenceExpression) variableDereferenceExpression.Modifiers[0].Value);
            Assert.AreEqual("pietje", ((LiteralExpression) value.VariableExpression).Value);

            Assert.IsNull(parser.Parse());
        }

        [Test]
        public void InvocationExpressionWithBrackets()
        {
            var parser = new Parser("[ MyRule myarg ]");
            var node = parser.Parse(ParseMode.SingleExpression);

            var invocationExpression = (InvocationExpression)node;

            Assert.AreEqual("MyRule", ((LiteralExpression)invocationExpression.RuleExpression).Value);

            Assert.AreEqual(1, invocationExpression.Arguments.Length);
            Assert.AreEqual("myarg", ((LiteralExpression)((ExpressionListExpression)invocationExpression.Arguments[0]).Expressions[0]).Value);

            Assert.IsNull(parser.Parse());
        }

        [Test]
        public void ReturnStatement()
        {
            var parser = new Parser("return 123 ;");
            var node = parser.Parse(ParseMode.Statement);

            var returnStatement = (ReturnStatement)node;
            
            Assert.AreEqual("123", ((LiteralExpression)((ExpressionListExpression)returnStatement.ReturnExpression).Expressions[0]).Value);

            Assert.IsNull(parser.Parse());
        }

        [Test]
        public void ReturnStatementWithMultipleValues()
        {
            var parser = new Parser("return 123 harry ;");
            var node = parser.Parse(ParseMode.Statement);

            var returnStatement = (ReturnStatement)node;

            var expressions = ((ExpressionListExpression) returnStatement.ReturnExpression).Expressions.OfType<LiteralExpression>().ToArray();
            Assert.AreEqual("123", expressions[0].Value);
            Assert.AreEqual("harry", expressions[1].Value);
            
            Assert.IsNull(parser.Parse());
        }
    }
}
