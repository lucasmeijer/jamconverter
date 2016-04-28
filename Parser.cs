using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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

            switch (scanToken.tokenType)
            {
                case TokenType.EOF:
                    return null;
                case TokenType.If:
                    return ParseIfStatement();
                case TokenType.AccoladeOpen:
                    return ParseBlockStatement();
                case TokenType.Rule:
                    return ParseRuleDeclarationStatement();
                case TokenType.Actions:
                    return ParseActionsDeclarationStatement();
                case TokenType.Return:
                    return ParseReturnStatement();
                case TokenType.Literal:
                    return ParseExpressionStatement();
                case TokenType.On:
                    return ParseOnStatement();
                case TokenType.While:
                    return ParseWhileStatement();
                case TokenType.For:
                    return ParseForStatement();
                case TokenType.Break:
                    _scanResult.Next();
                    _scanResult.Next().Is(TokenType.Terminator);
                    return new BreakStatement();
                case TokenType.Continue:
                    _scanResult.Next();
                    _scanResult.Next().Is(TokenType.Terminator);
                    return new ContinueStatement();
                default:
                    throw new ParsingException();
            }
        }

        private Statement ParseForStatement()
        {
            _scanResult.Next().Is(TokenType.For);
            var loopVariable = ParseExpression();
            _scanResult.Next().Is(TokenType.In);

            return new ForStatement() {LoopVariable = loopVariable.As<LiteralExpression>(), List = ParseExpressionList(), Body = ParseBlockStatement()};
        }

        private OnStatement ParseOnStatement()
        {
            _scanResult.Next().Is(TokenType.On);
            return new OnStatement() {Targets = ParseExpressionList(), Body = ParseBlockStatement()};
        }

        private WhileStatement ParseWhileStatement()
        {
            _scanResult.Next().Is(TokenType.While);
            return new WhileStatement() { Condition = ParseCondition(), Body = ParseBlockStatement() };
        }

        private Statement ParseExpressionStatement()
        {
            if (IsNextStatementAssignment())
            {
                var leftSideOfAssignment = ParseLeftSideOfAssignment();

                var assignmentToken = _scanResult.Next();
                var right = ParseExpressionList();
                _scanResult.Next().Is(TokenType.Terminator);

                return new ExpressionStatement { Expression = new BinaryOperatorExpression { Left = leftSideOfAssignment, Right = right, Operator = OperatorFor(assignmentToken.tokenType) } };
            }
            
            var invocationExpression = new InvocationExpression {RuleExpression = ParseExpression(), Arguments = ParseArgumentList().ToArray()};

            _scanResult.Next().Is(TokenType.Terminator);

            return new ExpressionStatement {Expression = invocationExpression};
        }

        private Expression ParseLeftSideOfAssignment()
        {
            var cursor = _scanResult.GetCursor();
            _scanResult.Next();
            var nextScanToken = _scanResult.Next();
            _scanResult.SetCursor(cursor);

            if (nextScanToken.tokenType == TokenType.On)
            {
                var variable = ParseExpression();
                _scanResult.Next().Is(TokenType.On);
                var targets = ParseExpressionList();
                return new VariableOnTargetExpression() { Variable = variable, Targets = targets };
            }

            if (nextScanToken.tokenType == TokenType.Assignment || nextScanToken.tokenType == TokenType.AppendOperator || nextScanToken.tokenType == TokenType.SubtractOperator)
            {
                return ParseExpression();
            }

            throw new ParsingException();
        }

        private bool IsNextStatementAssignment()
        {
            var cursor = _scanResult.GetCursor();
            _scanResult.Next();
            var nextScanToken = _scanResult.Next();
            _scanResult.SetCursor(cursor);

            switch (nextScanToken.tokenType)
            {
                case TokenType.Assignment:
                case TokenType.AppendOperator:
                case TokenType.SubtractOperator:
                case TokenType.On:

                    return true;
            }
            return false;
        }

        private Statement ParseReturnStatement()
        {
            _scanResult.Next().Is(TokenType.Return);
            var returnExpression = ParseExpressionList();
            _scanResult.Next().Is(TokenType.Terminator);

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

        private BlockStatement ParseBlockStatement()
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

            var ifStatement = new IfStatement {Condition = ParseCondition(), Body = ParseBlockStatement()};

            var peek = _scanResult.Peek();
            if (peek.tokenType == TokenType.Else)
            {
                _scanResult.Next();
                ifStatement.Else = ParseStatement();
            }

            return ifStatement;
        }

        public Expression ParseExpression()
        {
            switch (_scanResult.Peek().tokenType)
            {
                case TokenType.EOF:
                case TokenType.Colon:
                case TokenType.Terminator:
                case TokenType.ParenthesisClose:
                case TokenType.BracketClose:
                case TokenType.Assignment:
                case TokenType.AppendOperator:
                case TokenType.SubtractOperator:
                    return null;
                case TokenType.VariableDereferencer:
                    return ParseVariableDereferenceExpression();
                case TokenType.AccoladeOpen:
                    return null;
                case TokenType.BracketOpen:
                    return ParseInvocationExpression();
                default:
                    return ScanForCombineExpression(new LiteralExpression { Value = _scanResult.Next().literal });
            }
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

                    modifier.Is(TokenType.VariableExpansionModifier);

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

            next.Is(TokenType.ParenthesisClose);
            
            return ScanForCombineExpression(variableDereferenceExpression);
        }

        private static Operator OperatorFor(TokenType tokenType)
        {
            switch (tokenType)
            {
                case TokenType.AppendOperator:
                    return Operator.Append;
                case TokenType.Assignment:
                    return Operator.Assignment;
                case TokenType.SubtractOperator:
                    return Operator.Subtract;
                case TokenType.In:
                    return Operator.In;
                default:
                    throw new NotSupportedException("Unknown operator tokentype: " + tokenType);
            }
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

        public Condition ParseCondition()
        {
            var condition = new Condition();

            if (_scanResult.Peek().tokenType == TokenType.Not)
            {
                condition.Negated = true;
                _scanResult.Next();
            }

            condition.Left = ParseExpression();
            var peek = _scanResult.Peek();
            if (peek.tokenType == TokenType.AccoladeOpen || peek.tokenType == TokenType.EOF)
                return condition;

            if (condition.Negated)
                throw new ParsingException("We do not support conditions that use the ! negation, and also have an operator and a right side.  jam has very weird behaviour in that case");

            _scanResult.Next();
            condition.Operator = OperatorFor(peek.tokenType);
            condition.Right = ParseExpressionList();

            return condition;
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