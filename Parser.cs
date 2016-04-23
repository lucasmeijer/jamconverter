using System;
using System.Collections.Generic;
using System.Linq;
using jamconverter.AST;

namespace jamconverter
{
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
                return Parse(ParseMode.SingleExpression);

            var sr = _scanner.ScanSkippingWhiteSpace();

            if (sr == null)
                return null;
            
            if (sr.tokenType == TokenType.Colon || sr.tokenType == TokenType.Terminator || sr.tokenType == TokenType.ParenthesisClose)
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

                return new IfStatement { Condition = (Expression)condition, Body = (BlockStatement) body};
            }

            if (sr.tokenType == TokenType.VariableDereferencer)
            {
                var open = _scanner.Scan();
                if (open.tokenType != TokenType.ParenthesisOpen)
                    throw new ParsingException("All $ should be followed by ( but got: "+open.tokenType);

                var variableDereferenceExpression = new VariableDereferenceExpression() {VariableExpression = (Expression)Parse(ParseMode.SingleExpression)};
                
                var next = _scanner.Scan();

                if (next.tokenType == TokenType.Colon)
                {
                    var modifiers = new List<VariableDereferenceModifier>();
                    while (true)
                    {
                        var modifier = _scanner.Scan();
                        if (modifier.tokenType == TokenType.Colon)
                            continue;

                        if (modifier.tokenType == TokenType.ParenthesisClose)
                        {
                            next = modifier;
                            break;
                        }
                        
                        if (modifier.tokenType != TokenType.VariableExpansionModifier)
                            throw new ParsingException();

                        var peek = _scanner.Scan();
                        string modifierValue = null;
                        if (peek.tokenType == TokenType.Assignment)
                        {
                            var valueLiteral = _scanner.Scan();
                            if (valueLiteral.tokenType != TokenType.Literal)
                                throw new ParsingException();
                            modifierValue = valueLiteral.literal;
                        }
                        else
                            _scanner.UnScan(peek);

                        modifiers.Add(new VariableDereferenceModifier() {Command = modifier.literal[0], Value = modifierValue});
                    }
                    variableDereferenceExpression.Modifiers = modifiers.ToArray();
                }

                if (next.tokenType != TokenType.ParenthesisClose)
                    throw new ParsingException("All $(something should be followed by ) but got: " + open.tokenType);

                var resultExpression = ScanForCombineExpression(variableDereferenceExpression);

                if (parseMode == ParseMode.SingleExpression)
                    return resultExpression;

                var additional = Parse(ParseMode.ExpressionList);
                if (additional == null)
                    return new ExpressionListExpression() {Expressions = new[] { resultExpression } };
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

                        return new ExpressionStatement {Expression = new AssignmentExpression
                        {
                            Left = new LiteralExpression { Value =  sr.literal},
                            Right = right,
                            Operator = Operator.Assignment
                        }};
                    }
                    _scanner.UnScan(sr2);

                    var arguments = ParseArgumentList().ToArray();
                    var invocationExpression = new InvocationExpression
                    {
                        RuleExpression = new LiteralExpression { Value = sr.literal },
                        Arguments = arguments
                    };

                    ScanTerminator();

                    return new ExpressionStatement() {Expression = invocationExpression};
                }

                var resultExpression = ScanForCombineExpression(new LiteralExpression { Value = sr.literal });

                if (parseMode == ParseMode.SingleExpression)
                    return resultExpression;
                
                var additional = Parse(ParseMode.ExpressionList);
                if (additional == null)
                    return new ExpressionListExpression { Expressions = new[] { resultExpression } };

                var tailExpressionList = additional as ExpressionListExpression;
                if (tailExpressionList != null)
                {
                    return new ExpressionListExpression()
                    {
                        Expressions = tailExpressionList.Expressions.Prepend(resultExpression).ToArray()
                    };
                }
            }

            throw new ParsingException("expected Value, got: " + sr.tokenType);
        }

        void ScanTerminator()
        {
            var sr = _scanner.ScanSkippingWhiteSpace();
            if (sr.tokenType != TokenType.Terminator)
                throw new ParsingException();
        }

        Expression ScanForCombineExpression(Expression firstExpression)
        {
            var peek = _scanner.Scan();
            _scanner.UnScan(peek);
            if (peek == null || (peek.tokenType != TokenType.Literal && peek.tokenType != TokenType.VariableDereferencer))
                return firstExpression;

            var tail = Parse(ParseMode.SingleExpression);
            var combineExpressionTail = tail as CombineExpression;

            var tailElements = (combineExpressionTail != null)
                ? combineExpressionTail.Elements
                : new[] {(Expression) tail};

            return new CombineExpression
            {
                Elements = tailElements.Prepend(firstExpression).ToArray()
            };
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
                if (nextToken.tokenType == TokenType.Colon)
                    continue;
                _scanner.UnScan(nextToken);
                yield break;
            }
        }
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

}
