﻿using System;
using System.CodeDom;
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
                var expression = (Expression)Parse(ParseMode.SingleExpression);
                if (expression == null)
                    break;
                expressions.Add(expression);
            }
            return new ExpressionList() {Expressions = expressions.ToArray()};
        }

        public Node Parse(ParseMode parseMode = ParseMode.Statement)
        {
            var sr = _scanner.ScanSkippingWhiteSpace();

            if (sr == null)
                return null;

            if (sr.tokenType == TokenType.Comment)
                return Parse(parseMode);

            if (sr.tokenType == TokenType.Colon || sr.tokenType == TokenType.Terminator || sr.tokenType == TokenType.ParenthesisClose || sr.tokenType == TokenType.BracketClose)
            {
                _scanner.UnScan(sr);
                return null;
            }

            if (sr.tokenType == TokenType.If)
            {
                var expression = (Expression)Parse(ParseMode.SingleExpression);
                var peek = _scanner.ScanSkippingWhiteSpace();
                var ifStatement = new IfStatement();

                switch (peek.tokenType)
                {
                    case TokenType.Assignment:
                        ifStatement.Condition = new BinaryOperatorExpression
                        {
                            Left = expression,
                            Right = ParseExpressionList()
                        };
                        break;
                    case TokenType.AccoladeOpen:
                        ifStatement.Condition = expression;
                        _scanner.UnScan(peek);
                        break;
                    default:
                        throw new ParsingException();
                }

                var body = Parse(ParseMode.Statement);

                var blockStatement = body as BlockStatement;
                if (blockStatement == null)
                    throw new ParsingException("if statements always need to be followed by a blockstatment: {}");

                ifStatement.Body = blockStatement;
                return ifStatement;
            }

            if (sr.tokenType == TokenType.VariableDereferencer)
            {
                var open = _scanner.Scan();
                if (open.tokenType != TokenType.ParenthesisOpen)
                    throw new ParsingException("All $ should be followed by ( but got: "+open.tokenType);

                var variableDereferenceExpression = new VariableDereferenceExpression() {VariableExpression = (Expression)Parse(ParseMode.SingleExpression)};
                
                var next = _scanner.Scan();
                var modifiers = new List<VariableDereferenceModifier>();

                if (next.tokenType == TokenType.BracketOpen)
                {
                    variableDereferenceExpression.IndexerExpression = (Expression) Parse(ParseMode.SingleExpression);
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
                            modifierValue = (Expression)Parse(ParseMode.SingleExpression);
                        }
                        else
                            _scanner.UnScan(peek);

                        modifiers.Add(new VariableDereferenceModifier() {Command = modifier.literal[0], Value = modifierValue});
                    }
                }
                variableDereferenceExpression.Modifiers = modifiers.ToArray();
                if (next.tokenType != TokenType.ParenthesisClose)
                    throw new ParsingException("All $(something should be followed by ) but got: " + open.tokenType);

                var resultExpression = ScanForCombineExpression(variableDereferenceExpression);

                if (parseMode == ParseMode.SingleExpression)
                    return resultExpression;

                var tailExpressionList = ParseExpressionList();
   
                return new ExpressionList() { Expressions = tailExpressionList.Expressions.Prepend(resultExpression).ToArray() };
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
                return new RuleDeclaration {Name = ruleNameStr, Arguments = arguments.OfType<ExpressionList>().SelectMany(ele=>ele.Expressions.OfType<LiteralExpression>()).Select(le=>le.Value).ToArray(), Body = (BlockStatement) body};
            }

            if (sr.tokenType == TokenType.BracketOpen)
            {
                var ruleExpression = Parse(ParseMode.SingleExpression);
                var arguments = ParseArgumentList().ToArray();
                var closeBracket = _scanner.ScanSkippingWhiteSpace();
                if (closeBracket.tokenType != TokenType.BracketClose)
                    throw new ParsingException();
                return new InvocationExpression {RuleExpression = ruleExpression, Arguments = arguments};
            }

            if (sr.tokenType == TokenType.Return)
            {
                var returnExpression = ParseExpressionList();
                var terminator = _scanner.ScanSkippingWhiteSpace();
                if (terminator.tokenType != TokenType.Terminator)
                    throw new ParsingException();

                return new ReturnStatement() {ReturnExpression = returnExpression};
            }

            if (sr.tokenType == TokenType.Literal)
            {
                if (parseMode == ParseMode.Statement)
                {
                    var sr2 = _scanner.ScanSkippingWhiteSpace();
                    if (sr2 != null && (sr2.tokenType == TokenType.Assignment || sr2.tokenType == TokenType.AppendOperator))
                    {
                        var right = ParseExpressionList();
                        var terminator = _scanner.ScanSkippingWhiteSpace();
                        if (terminator.tokenType != TokenType.Terminator)
                            throw new ParsingException();

                        return new ExpressionStatement {Expression = new BinaryOperatorExpression
                        {
                            Left = new LiteralExpression { Value =  sr.literal},
                            Right = right,
                            Operator = OperatorFor(sr2.tokenType)
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

                var tailExpressionList = ParseExpressionList();
                
                return new ExpressionList()
                {
                    Expressions = tailExpressionList.Expressions.Prepend(resultExpression).ToArray()
                };
            }

            throw new ParsingException("expected Value, got: " + sr.tokenType);
        }

        private static Operator OperatorFor(TokenType tokenType)
        {
            return tokenType == TokenType.AppendOperator ?  Operator.Append : Operator.Assignment;
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
