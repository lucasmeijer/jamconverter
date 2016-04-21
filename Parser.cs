using System;
using System.Collections.Generic;
using System.Linq;
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
           
            Assert.IsTrue(node is InvocationExpression);
            var invocationExpression = node as InvocationExpression;

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

            var invocationExpression = node as InvocationExpression;

            Assert.AreEqual(1, invocationExpression.Arguments.Length);

            var arg1 = invocationExpression.Arguments[0] as LiteralExpression;
            Assert.AreEqual("a", arg1.Value);
        }


        [Test]
        public void SimpleInvocationWithTwoLiteralArguments()
        {
            var parser = new Parser("input a : b ;");
            var node = parser.Parse();

            var invocationExpression = node as InvocationExpression;

            Assert.AreEqual(2, invocationExpression.Arguments.Length);

            var arg1 = invocationExpression.Arguments[0] as LiteralExpression;
            Assert.AreEqual("a", arg1.Value);

            var arg2 = invocationExpression.Arguments[1] as LiteralExpression;
            Assert.AreEqual("b", arg2.Value);
        }

        [Test]
        public void SimpleInvocationWithMultiValueArgument()
        {
            var parser = new Parser("input a b c ;");
            var node = parser.Parse();

            var invocationExpression = node as InvocationExpression;

            Assert.AreEqual(1, invocationExpression.Arguments.Length);

            var expressionListExpression = (ExpressionListExpression) invocationExpression.Arguments[0];
            Assert.AreEqual(3, expressionListExpression.Expressions.Length);
            Assert.AreEqual("a", ((LiteralExpression) expressionListExpression.Expressions[0]).Value);
            Assert.AreEqual("b", ((LiteralExpression)expressionListExpression.Expressions[1]).Value);
            Assert.AreEqual("c", ((LiteralExpression)expressionListExpression.Expressions[2]).Value);
        }

    }

    public class ExpressionListExpression : Expression
    {
        public Expression[] Expressions { get; set; }
    }

    public class LiteralExpression : Expression
    {
        public string Value { get; set; }

        public LiteralExpression(string value)
        {
            this.Value = value;
        }
    }

    public class Parser
    {
        private Scanner _scanner;

        public Parser(string input)
        {
            _scanner = new Scanner(input);
        }

        public Node Parse(bool topLevel = true)
        {
            var sr = _scanner.ScanSkippingWhiteSpace();

            if (sr == null)
                return null;

            if (sr.tokenType == TokenType.ArgumentSeperator || sr.tokenType == TokenType.Terminator)
            {
                _scanner.UnScan(sr);
                 return new EmptyExpression();
            }

            if (sr.tokenType == TokenType.Literal)
            {
                if (topLevel)
                {
                    var arguments = ParseArgumentList();
                    return new InvocationExpression
                    {
                        RuleExpression = new LiteralExpression(sr.literal),
                        Arguments = arguments.ToArray()
                    };
                }
                else
                {
                    var literalExpression = new LiteralExpression(sr.literal);
                    var additional = Parse(false);
                    if (additional == null)
                        return literalExpression;

                    if (additional is LiteralExpression)
                    {
                        return new ExpressionListExpression()
                        {
                            Expressions = new[] {literalExpression, (LiteralExpression) additional}
                        };
                    }

                    if (additional is EmptyExpression)
                        return literalExpression;
                    
                    var expressionListExpression = additional as ExpressionListExpression;
                    expressionListExpression.Expressions = expressionListExpression.Expressions.Prepend(literalExpression).ToArray();
                    return expressionListExpression;
                }
            }

            throw new NotSupportedException("expected Value, got: " + sr.tokenType);
        }

        private IEnumerable<Expression> ParseArgumentList()
        {
            while (true)
            {
                var node = Parse(topLevel: false);
                if (node == null)
                    yield break;

                var expression = node as Expression;
                if (expression == null)
                    throw new ArgumentException("Expected expression, got: " + node);
                yield return expression;

                var nextToken = _scanner.ScanSkippingWhiteSpace();
                if (nextToken.tokenType == TokenType.Terminator)
                    yield break;
            }
        }

        private IEnumerable<ScanResult> ScanUntil(TokenType untilTokenType)
        {
            while (true)
            {
                var sr = _scanner.ScanSkippingWhiteSpace();
                yield return sr;
                if (sr.tokenType == untilTokenType)
                    yield break;
            }
        }
    }

    public class Node
    {
    }

    public class Expression : Node
    {
    }

    public class EmptyExpression : Expression
    {
        
    }

    public class InvocationExpression : Node
    {
        public Node RuleExpression { get; set; }
        public Expression[] Arguments { get; set; }
    }
}
