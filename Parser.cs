using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using NUnit.Framework.Constraints;

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

            var arg1 = invocationExpression.Arguments[0] as LiteralExpression;
            Assert.AreEqual("a", arg1.Value);
        }


        [Test]
        public void SimpleInvocationWithTwoLiteralArguments()
        {
            var parser = new Parser("input a : b ;");
            var node = parser.Parse();

            var invocationExpression = (InvocationExpression)((ExpressionStatement)node).Expression;

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

            var variableDereferenceExpression = (VariableDereferenceExpression) invocationExpression.Arguments[0];
            Assert.AreEqual("myvar",((LiteralExpression) variableDereferenceExpression.VariableExpression).Value);
        }

        [Test]
        public void NestedVariableDereference()
        {
            var parser = new Parser("input $($(myvar)) ;");
            var node = parser.Parse();

            var invocationExpression = (InvocationExpression)((ExpressionStatement)node).Expression;
            Assert.AreEqual(1, invocationExpression.Arguments.Length);

            var variableDereferenceExpression = (VariableDereferenceExpression)invocationExpression.Arguments[0];

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
            var right = (LiteralExpression) assignmentExpression.Right;

            Assert.AreEqual("a", left.Value);
            Assert.AreEqual("b", right.Value);
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
        }


        [Test]
        public void EmptyBlockStatement()
        {
            var parser = new Parser("{ }");
            var node = parser.Parse();

            var blockStatement = (BlockStatement)node;

            Assert.AreEqual(0, blockStatement.Statements.Length);
         }


        [Test]
        public void IfStatement()
        {
            var parser = new Parser("if $(somevar) {}");
            var ifStatement = (IfStatement) parser.Parse(true);
        }
    }

    public class IfStatement : Statement
    {
        public Expression Condition { get; set; }
        public BlockStatement Body { get; set; }
    }

    public class BlockStatement : Statement
    {
        public Statement[] Statements { get; set; }
    }

    public class Statement : Node
    {
    }

    public class AssignmentExpression : Expression
    {
        public Expression Left { get; set; }
        public Expression Right { get; set; }
        public Operator Operator { get; set; }
    }

    public enum Operator
    {
        Assignment
    }

    public class VariableDereferenceExpression : Expression
    {
        public Expression VariableExpression { get; set; }
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
        private readonly Scanner _scanner;

        public Parser(string input)
        {
            _scanner = new Scanner(input);
        }

        public Node Parse(bool parseStatement = true)
        {
            var sr = _scanner.ScanSkippingWhiteSpace();

            if (sr == null)
                return null;

            if (sr.tokenType == TokenType.ArgumentSeperator || sr.tokenType == TokenType.Terminator || sr.tokenType == TokenType.ParenthesisClose)
            {
                _scanner.UnScan(sr);
                 return new EmptyExpression();
            }

            if (sr.tokenType == TokenType.If)
            {
                var condition = Parse(false);
                var body = Parse(true);

                if (!(condition is Expression))
                    throw new ParsingException("if keyword always needs to be followed by an expression");
                if (!(body is BlockStatement))
                    throw new ParsingException("if statements always need to be followed by a blockstatment: {}");

                return new IfStatement() { Condition = (Expression)condition, Body = (BlockStatement) body};
            }

            if (sr.tokenType == TokenType.VariableDereferencer)
            {
                var open = _scanner.Scan();
                if (open.tokenType != TokenType.ParenthesisOpen)
                    throw new ParsingException("All $ should be followed by ( but got: "+open.tokenType);

                var result = new VariableDereferenceExpression() {VariableExpression = (Expression)Parse(false)};

                var close = _scanner.Scan();
                if (close.tokenType != TokenType.ParenthesisClose)
                    throw new ParsingException("All $(something should be followed by ) but got: " + open.tokenType);

                return result;
            }

            if (sr.tokenType == TokenType.AccoladeOpen)
            {
                var statements = new List<Statement>();
                while (true)
                {
                    var peek = _scanner.ScanSkippingWhiteSpace();
                    if (peek.tokenType == TokenType.AccoladeClose)
                        return new BlockStatement() {Statements = statements.ToArray()};
                    _scanner.UnScan(peek);
                    statements.Add((Statement)Parse(parseStatement=true));
                }
            }

            if (sr.tokenType == TokenType.Literal)
            {
                if (parseStatement)
                {
                    var sr2 = _scanner.ScanSkippingWhiteSpace();
                    if (sr2.tokenType == TokenType.Assignment)
                    {
                        return new ExpressionStatement() {Expression = new AssignmentExpression()
                        {
                            Left = new LiteralExpression(sr.literal),
                            Right = (Expression) Parse(false),
                            Operator = Operator.Assignment
                        }};
                    }
                    _scanner.UnScan(sr2);

                    var arguments = ParseArgumentList().ToArray();
                    var invocationExpression = new InvocationExpression
                    {
                        RuleExpression = new LiteralExpression(sr.literal),
                        Arguments = arguments
                    };

                    return new ExpressionStatement() {Expression = invocationExpression};
                }

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

            throw new NotSupportedException("expected Value, got: " + sr.tokenType);
        }

        private IEnumerable<Expression> ParseArgumentList()
        {
            while (true)
            {
                var node = Parse(parseStatement: false);
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
    }

    public class ParsingException : Exception
    {
        public ParsingException(string s) : base(s)
        {
        }
    }

    public class Node
    {
    }

    public class Expression : Node
    {
    }

    public class ExpressionStatement : Statement
    {
        public Expression Expression { get; set; }
    }

    public class EmptyExpression : Expression
    {
        
    }

    public class InvocationExpression : Expression
    {
        public Node RuleExpression { get; set; }
        public Expression[] Arguments { get; set; }
    }
}
