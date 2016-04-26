using System;
using System.Collections.Generic;
using System.Linq;
using jamconverter.AST;

namespace jamconverter
{
    public class Parser
    {
        private readonly ScanResult _scanResult;

        public Parser(string input)
        {
            _scanResult = new Scanner(input).Scan();
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
            var scanToken = _scanResult.Peek();

            if (scanToken.tokenType == TokenType.EOF)
                return null;

            if (scanToken.tokenType == TokenType.If)
                return ParseIfStatement();

            if (scanToken.tokenType == TokenType.AccoladeOpen)
                return ParseBlockStatement();

            if (scanToken.tokenType == TokenType.Rule)
                return ParseRuleDeclarationStatement();

            if (scanToken.tokenType == TokenType.Actions)
                return ParseActionsDeclarationStatement();

            if (scanToken.tokenType == TokenType.Return)
                return ParseReturnStatement();

            if (scanToken.tokenType == TokenType.Literal)
                return ParseExpressionStatement();

            throw new ParsingException();
        }

        private Statement ParseExpressionStatement()
        {
            var scanToken = _scanResult.Next();
            var nextScanToken = _scanResult.Peek();

            if (nextScanToken.tokenType != TokenType.EOF && (nextScanToken.tokenType == TokenType.Assignment || nextScanToken.tokenType == TokenType.AppendOperator))
            {
                _scanResult.Next();
                var right = ParseExpressionList();
                var terminator = _scanResult.Next();
                if (terminator.tokenType != TokenType.Terminator)
                    throw new ParsingException();

                return new ExpressionStatement {Expression = new BinaryOperatorExpression {Left = new LiteralExpression {Value = scanToken.literal}, Right = right, Operator = OperatorFor(nextScanToken.tokenType)}};
            }

            var arguments = ParseArgumentList().ToArray();
            var invocationExpression = new InvocationExpression {RuleExpression = new LiteralExpression {Value = scanToken.literal}, Arguments = arguments};

            ScanTerminator();

            return new ExpressionStatement {Expression = invocationExpression};
        }

        private Statement ParseReturnStatement()
        {
            _scanResult.Next().Is(TokenType.Return);
            var returnExpression = ParseExpressionList();
            ScanTerminator();

            return new ReturnStatement {ReturnExpression = returnExpression};
        }

        private Statement ParseActionsDeclarationStatement()
        {
            _scanResult.Next().Is(TokenType.Actions);
            var expressionList = ParseExpressionList();

            var accoladeOpen = _scanResult.Next();
            if (accoladeOpen.tokenType != TokenType.AccoladeOpen)
                throw new ParsingException();
            _scanResult.ProduceStringUntilEndOfLine();
            var actions = new List<string>();
            while (true)
            {
                var action = _scanResult.ProduceStringUntilEndOfLine();
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
            _scanResult.Next().Is(TokenType.Rule);
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
            _scanResult.Next().Is(TokenType.AccoladeOpen);
            var statements = new List<Statement>();
            while (true)
            {
                var peek = _scanResult.Peek();
                if (peek.tokenType == TokenType.AccoladeClose)
                {
                    _scanResult.Next();
                    return new BlockStatement {Statements = statements.ToArray()};
                }
                
                statements.Add(ParseStatement());
            }
        }

        private Statement ParseIfStatement()
        {
            _scanResult.Next().Is(TokenType.If);
            var expression = ParseExpression();
            var peek = _scanResult.Peek();
            var ifStatement = new IfStatement();

            switch (peek.tokenType)
            {
                case TokenType.Assignment:
                    _scanResult.Next();
                    ifStatement.Condition = new BinaryOperatorExpression {Left = expression, Right = ParseExpressionList()};
                    break;
                case TokenType.AccoladeOpen:
                    ifStatement.Condition = expression;
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
            var scanToken = _scanResult.Peek();

            if (scanToken.tokenType == TokenType.EOF)
                return null;

            if (scanToken.tokenType == TokenType.Colon || scanToken.tokenType == TokenType.Terminator ||
                scanToken.tokenType == TokenType.ParenthesisClose || scanToken.tokenType == TokenType.BracketClose)
            {
                return null;
            }

            if (scanToken.tokenType == TokenType.VariableDereferencer)
                return ParseVariableDereferenceExpression();

            if (scanToken.tokenType == TokenType.AccoladeOpen)
                return null;

            if (scanToken.tokenType == TokenType.BracketOpen)
                return ParseInvocationExpression();

            if (scanToken.tokenType == TokenType.Literal || /* on is the strangest keyword in that it is sometimes a keyword and sometimes a literal*/ scanToken.tokenType == TokenType.On)
                return ScanForCombineExpression(new LiteralExpression {Value = _scanResult.Next().literal});

            throw new ParsingException("expected Value, got: " + scanToken.tokenType);
        }

        private Expression ParseInvocationExpression()
        {
            var bracketOpen = _scanResult.Next();
            if (bracketOpen.tokenType != TokenType.BracketOpen)
                throw new ParsingException();

            var ruleExpression = ParseExpression();
            var arguments = ParseArgumentList().ToArray();
            var closeBracket = _scanResult.Next();
            if (closeBracket.tokenType != TokenType.BracketClose)
                throw new ParsingException();
            return new InvocationExpression {RuleExpression = ruleExpression, Arguments = arguments};
        }

        private Expression ParseVariableDereferenceExpression()
        {
            var dollar = _scanResult.Next();
            if (dollar.tokenType != TokenType.VariableDereferencer)
                throw new ParsingException();

            var open = _scanResult.Next();
            if (open.tokenType != TokenType.ParenthesisOpen)
                throw new ParsingException("All $ should be followed by ( but got: " + open.tokenType);

            var variableDereferenceExpression = new VariableDereferenceExpression {VariableExpression = ParseExpression()};

            var next = _scanResult.Next();

            if (next.tokenType == TokenType.BracketOpen)
            {
                variableDereferenceExpression.IndexerExpression = ParseExpression();
                if (_scanResult.Next().tokenType != TokenType.BracketClose)
                    throw new ParsingException("Expected bracket close while parsing variable dereference expressions' indexer");
                next = _scanResult.Next();
            }

            var modifiers = new List<VariableDereferenceModifier>();

            if (next.tokenType == TokenType.Colon)
            {
                while (true)
                {
                    var modifier = _scanResult.Next();
                    if (modifier.tokenType == TokenType.Colon)
                        continue;

                    if (modifier.tokenType == TokenType.ParenthesisClose)
                    {
                        next = modifier;
                        break;
                    }

                    if (modifier.tokenType != TokenType.VariableExpansionModifier)
                        throw new ParsingException();

                    var peek = _scanResult.Peek();
                    Expression modifierValue = null;
                    if (peek.tokenType == TokenType.Assignment)
                    {
                        _scanResult.Next();
                        modifierValue = ParseExpression();
                    }

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
            var sr = _scanResult.Next();
            if (sr.tokenType != TokenType.Terminator)
                throw new ParsingException();
        }

        Expression ScanForCombineExpression(Expression firstExpression)
        {
            var peek = _scanResult.Peek(false);
            
            if (peek.tokenType == TokenType.EOF || (peek.tokenType != TokenType.Literal && peek.tokenType != TokenType.VariableDereferencer))
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

                var nextToken = _scanResult.Peek();
                if (nextToken.tokenType == TokenType.Colon)
                {
                    _scanResult.Next();
                    continue;
                }

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