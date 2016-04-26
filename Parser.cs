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

        public ExpressionList ParseExpressionList()
        {
            var expressions = new List<Expression>();
            while (true)
            {
                var expression = ParseExpression();
                if (expression == null)
                    break;
                expressions.Add(expression);
            }
            return new ExpressionList {Expressions = expressions.ToArray()};
        }

        public Statement ParseStatement()
        {
            var sr = _scanner.ScanSkippingWhiteSpace();

            if (sr == null)
                return null;

            if (sr.tokenType == TokenType.If)
                return ParseIfStatement();

            if (sr.tokenType == TokenType.AccoladeOpen)
                return ParseBlockStatement();

            if (sr.tokenType == TokenType.Rule)
                return ParseRuleDeclarationStatement();

            if (sr.tokenType == TokenType.Actions)
                return ParseActionsDeclarationStatement();

            if (sr.tokenType == TokenType.Return)
                return ParseReturnStatement();

            if (sr.tokenType == TokenType.Literal)
                return ParseExpressionStatement(sr);

            throw new ParsingException();
        }

        private Statement ParseExpressionStatement(ScanResult sr)
        {
            var sr2 = _scanner.ScanSkippingWhiteSpace();
            if (sr2 != null && (sr2.tokenType == TokenType.Assignment || sr2.tokenType == TokenType.AppendOperator))
            {
                var right = ParseExpressionList();
                var terminator = _scanner.ScanSkippingWhiteSpace();
                if (terminator.tokenType != TokenType.Terminator)
                    throw new ParsingException();

                return new ExpressionStatement {Expression = new BinaryOperatorExpression {Left = new LiteralExpression {Value = sr.literal}, Right = right, Operator = OperatorFor(sr2.tokenType)}};
            }
            _scanner.UnScan(sr2);

            var arguments = ParseArgumentList().ToArray();
            var invocationExpression = new InvocationExpression {RuleExpression = new LiteralExpression {Value = sr.literal}, Arguments = arguments};

            ScanTerminator();

            return new ExpressionStatement {Expression = invocationExpression};
        }

        private Statement ParseReturnStatement()
        {
            var returnExpression = ParseExpressionList();
            var terminator = _scanner.ScanSkippingWhiteSpace();
            if (terminator.tokenType != TokenType.Terminator)
                throw new ParsingException();

            return new ReturnStatement {ReturnExpression = returnExpression};
        }

        private Statement ParseActionsDeclarationStatement()
        {
            var expressionList = ParseExpressionList();

            var accoladeOpen = _scanner.Scan();
            if (accoladeOpen.tokenType != TokenType.AccoladeOpen)
                throw new ParsingException();
            _scanner.ScanStringUntilEndOfLine();
            var actions = new List<string>();
            while (true)
            {
                var action = _scanner.ScanStringUntilEndOfLine();
                if (action.Trim() == "}")
                    break;
                actions.Add(action);
            }
            return new ActionsDeclarationStatement()
            {
                Name = expressionList.Expressions.Last().As<LiteralExpression>().Value,
                Modifiers = new ExpressionList() {Expressions = expressionList.Expressions.Take(expressionList.Expressions.Length - 1).ToArray()},
                Actions = actions.ToArray()
            };
        }

        private Statement ParseRuleDeclarationStatement()
        {
            var ruleName = ParseExpression();
            var arguments = ParseArgumentList().ToArray();
            var body = ParseStatement();

            var ruleNameStr = ruleName.As<LiteralExpression>().Value;
            return new RuleDeclarationStatement
            {
                Name = ruleNameStr,
                Arguments = arguments.SelectMany(ele => ele.Expressions.Cast<LiteralExpression>()).Select(le => le.Value).ToArray(),
                Body = (BlockStatement) body
            };
        }

        private Statement ParseBlockStatement()
        {
            var statements = new List<Statement>();
            while (true)
            {
                var peek = _scanner.ScanSkippingWhiteSpace();
                if (peek.tokenType == TokenType.AccoladeClose)
                    return new BlockStatement {Statements = statements.ToArray()};
                _scanner.UnScan(peek);
                statements.Add(ParseStatement());
            }
        }

        private Statement ParseIfStatement()
        {
            var expression = ParseExpression();
            var peek = _scanner.ScanSkippingWhiteSpace();
            var ifStatement = new IfStatement();

            switch (peek.tokenType)
            {
                case TokenType.Assignment:
                    ifStatement.Condition = new BinaryOperatorExpression {Left = expression, Right = ParseExpressionList()};
                    break;
                case TokenType.AccoladeOpen:
                    ifStatement.Condition = expression;
                    _scanner.UnScan(peek);
                    break;
                default:
                    throw new ParsingException();
            }

            var body = ParseStatement();

            var blockStatement = body as BlockStatement;
            if (blockStatement == null)
                throw new ParsingException("if statements always need to be followed by a blockstatment: {}");

            ifStatement.Body = blockStatement;
            return ifStatement;
        }

        public Expression ParseExpression()
        {
            var sr = _scanner.ScanSkippingWhiteSpace();

            if (sr == null)
                return null;

            if (sr.tokenType == TokenType.Colon || sr.tokenType == TokenType.Terminator ||
                sr.tokenType == TokenType.ParenthesisClose || sr.tokenType == TokenType.BracketClose)
            {
                _scanner.UnScan(sr);
                return null;
            }

            if (sr.tokenType == TokenType.VariableDereferencer)
                return ParseVariableDereferenceExpression();

            if (sr.tokenType == TokenType.AccoladeOpen)
            {
                _scanner.UnScan(sr);
                return null;
            }

            if (sr.tokenType == TokenType.BracketOpen)
                return ParseInvocationExpression();

            if (sr.tokenType == TokenType.Literal)
                return ScanForCombineExpression(new LiteralExpression {Value = sr.literal});

            throw new ParsingException("expected Value, got: " + sr.tokenType);
        }

        private Expression ParseInvocationExpression()
        {
            var ruleExpression = ParseExpression();
            var arguments = ParseArgumentList().ToArray();
            var closeBracket = _scanner.ScanSkippingWhiteSpace();
            if (closeBracket.tokenType != TokenType.BracketClose)
                throw new ParsingException();
            return new InvocationExpression {RuleExpression = ruleExpression, Arguments = arguments};
        }

        private Expression ParseVariableDereferenceExpression()
        {
            var open = _scanner.Scan();
            if (open.tokenType != TokenType.ParenthesisOpen)
                throw new ParsingException("All $ should be followed by ( but got: " + open.tokenType);

            var variableDereferenceExpression = new VariableDereferenceExpression {VariableExpression = ParseExpression()};

            var next = _scanner.Scan();
            var modifiers = new List<VariableDereferenceModifier>();

            if (next.tokenType == TokenType.BracketOpen)
            {
                variableDereferenceExpression.IndexerExpression = ParseExpression();
                var peek = _scanner.Scan();
                if (peek.tokenType != TokenType.BracketClose)
                    throw new ParsingException("Expected bracket close while parsing variable dereference expressions' indexer");
                next = _scanner.Scan();
            }

            if (next.tokenType == TokenType.Colon)
            {
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
                    Expression modifierValue = null;
                    if (peek.tokenType == TokenType.Assignment)
                    {
                        modifierValue = ParseExpression();
                    }
                    else
                        _scanner.UnScan(peek);

                    modifiers.Add(new VariableDereferenceModifier {Command = modifier.literal[0], Value = modifierValue});
                }
            }
            variableDereferenceExpression.Modifiers = modifiers.ToArray();
            if (next.tokenType != TokenType.ParenthesisClose)
                throw new ParsingException("All $(something should be followed by ) but got: " + open.tokenType);

            return ScanForCombineExpression(variableDereferenceExpression);
        }

        private static Operator OperatorFor(TokenType tokenType)
        {
            return tokenType == TokenType.AppendOperator ? Operator.Append : Operator.Assignment;
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
            if (peek == null ||
                (peek.tokenType != TokenType.Literal && peek.tokenType != TokenType.VariableDereferencer))
                return firstExpression;

            var tail = ParseExpression();
            var combineExpressionTail = tail as CombineExpression;

            var tailElements = combineExpressionTail != null
                ? combineExpressionTail.Elements
                : new[] {tail};

            return new CombineExpression
            {
                Elements = tailElements.Prepend(firstExpression).ToArray()
            };
        }

        private ExpressionList[] ParseArgumentList()
        {
            var expressionLists = new List<ExpressionList>();
            while (true)
            {
                expressionLists.Add(ParseExpressionList());

                var nextToken = _scanner.ScanSkippingWhiteSpace();
                if (nextToken.tokenType == TokenType.Colon)
                    continue;
                _scanner.UnScan(nextToken);

                break;
            }

            return expressionLists.ToArray();
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