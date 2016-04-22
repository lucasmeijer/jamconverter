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
            var ifStatement = (IfStatement) parser.Parse();
            Assert.IsTrue(ifStatement.Condition is VariableDereferenceExpression);
            Assert.AreEqual(0, ifStatement.Body.Statements.Length);
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
        }
    }


    public class CombineExpression : Expression
    {
        public Expression[] Elements { get; set; }
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

   public  enum ParseMode
    {
        SingleExpression,
        ExpressionList,
        Statement,
        Condition
    }

    public class Parser
    {
        private readonly Scanner _scanner;

        public Parser(string input)
        {
            _scanner = new Scanner(input);
        }

        public Node Parse(ParseMode parseMode = ParseMode.Statement)
        {
            if (parseMode == ParseMode.Condition)
            {
                return Parse(ParseMode.SingleExpression);
            }

            var sr = _scanner.ScanSkippingWhiteSpace();

            if (sr == null)
                return null;

            
            if (sr.tokenType == TokenType.ArgumentSeperator || sr.tokenType == TokenType.Terminator || sr.tokenType == TokenType.ParenthesisClose)
            {
                _scanner.UnScan(sr);
                return null;
            }

            if (sr.tokenType == TokenType.If)
            {
                var condition = Parse(ParseMode.Condition);
                var body = Parse(ParseMode.Statement);

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

                var variableDereferenceExpression = new VariableDereferenceExpression() {VariableExpression = (Expression)Parse(ParseMode.SingleExpression)};

                
                var close = _scanner.Scan();
                if (close.tokenType != TokenType.ParenthesisClose)
                    throw new ParsingException("All $(something should be followed by ) but got: " + open.tokenType);

                var resultExpression = ScanForCombineExpression(variableDereferenceExpression);

                if (parseMode == ParseMode.SingleExpression)
                    return resultExpression;

                var additional = Parse(ParseMode.ExpressionList);
                if (additional == null || additional is EmptyExpression)
                    return new ExpressionListExpression() {Expressions = new Expression[] { resultExpression } };
                var tailExpressionList = additional as ExpressionListExpression;
                if (tailExpressionList != null)
                    return new ExpressionListExpression() { Expressions = tailExpressionList.Expressions.Prepend(resultExpression).ToArray() };

                throw new ParsingException();
            }

            if (sr.tokenType == TokenType.AccoladeOpen)
            {
                if (parseMode != ParseMode.Statement)
                {
                    _scanner.UnScan(sr);
                    return null;
                }

                var statements = new List<Statement>();
                while (true)
                {
                    var peek = _scanner.ScanSkippingWhiteSpace();
                    if (peek.tokenType == TokenType.AccoladeClose)
                        return new BlockStatement() {Statements = statements.ToArray()};
                    _scanner.UnScan(peek);
                    statements.Add((Statement)Parse(ParseMode.Statement));
                }
            }

            if (sr.tokenType == TokenType.Rule)
            {
                var ruleName = Parse(ParseMode.SingleExpression);
                var arguments = ParseArgumentList().ToArray();
                var body = Parse(ParseMode.Statement);

                var ruleNameStr = ((LiteralExpression) ruleName).Value;
                return new RuleDeclaration {Name = ruleNameStr, Arguments = arguments.OfType<ExpressionListExpression>().SelectMany(ele=>ele.Expressions.OfType<LiteralExpression>()).Select(le=>le.Value).ToArray(), Body = (BlockStatement) body};
            }

            if (sr.tokenType == TokenType.Literal)
            {
                if (parseMode == ParseMode.Statement)
                {
                    var sr2 = _scanner.ScanSkippingWhiteSpace();
                    if (sr2.tokenType == TokenType.Assignment)
                    {
                        var right = (Expression) Parse(ParseMode.ExpressionList);
                        var terminator = _scanner.ScanSkippingWhiteSpace();
                        if (terminator.tokenType != TokenType.Terminator)
                            throw new ParsingException();

                        return new ExpressionStatement() {Expression = new AssignmentExpression()
                        {
                            Left = new LiteralExpression(sr.literal),
                            Right = right,
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

                    ScanTerminator();

                    return new ExpressionStatement() {Expression = invocationExpression};
                }

                var resultExpression = ScanForCombineExpression(new LiteralExpression(sr.literal));

                if (parseMode == ParseMode.SingleExpression)
                    return resultExpression;
                
                var additional = Parse(ParseMode.ExpressionList);
                if (additional == null || additional is EmptyExpression)
                    return new ExpressionListExpression { Expressions = new[] { resultExpression } };

                var tailExpressionList = additional as ExpressionListExpression;
                if (tailExpressionList != null)
                {
                    return new ExpressionListExpression()
                    {
                        Expressions = tailExpressionList.Expressions.Prepend(resultExpression).ToArray()
                    };
                }
                
                var expressionListExpression = additional as ExpressionListExpression;
                expressionListExpression.Expressions = expressionListExpression.Expressions.Prepend(resultExpression).ToArray();
                return expressionListExpression;
            }

            throw new NotSupportedException("expected Value, got: " + sr.tokenType);
        }

        private void ScanTerminator()
        {
            var sr = _scanner.ScanSkippingWhiteSpace();
            if (sr.tokenType != TokenType.Terminator)
                throw new ParsingException();
        }

        private Expression ScanForCombineExpression(Expression firstExpression)
        {
            var peek = _scanner.Scan();
            _scanner.UnScan(peek);
            if (peek.tokenType == TokenType.Literal || peek.tokenType == TokenType.VariableDereferencer)
            {
                var tail = Parse(ParseMode.SingleExpression);
                var combineExpressionTail = tail as CombineExpression;

                var tailElements = (combineExpressionTail != null)
                    ? combineExpressionTail.Elements
                    : new[] {(Expression) tail};

                {
                    return new CombineExpression()
                    {
                        Elements = tailElements.Prepend(firstExpression).ToArray()
                    };
                }
            }
            
            return firstExpression;
        }

        private IEnumerable<Expression> ParseArgumentList()
        {
            while (true)
            {
                var node = Parse(ParseMode.ExpressionList);
                if (node == null)
                    yield break;

                var expression = node as Expression;
                if (expression == null)
                    throw new ArgumentException("Expected expression, got: " + node);
                yield return expression;

                var nextToken = _scanner.ScanSkippingWhiteSpace();
                if (nextToken.tokenType == TokenType.ArgumentSeperator)
                    continue;
                _scanner.UnScan(nextToken);
                yield break;
            }
        }
    }

    public class RuleDeclaration : Node
    {
        public string Name { get; set; }
        public BlockStatement Body { get; set; }
        public string[] Arguments { get; set; }
    }

    public class ParsingException : Exception
    {
        public ParsingException(string s) : base(s)
        {
        }

        public ParsingException()
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
