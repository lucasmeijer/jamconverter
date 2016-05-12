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

	    public TopLevel ParseTopLevel()
	    {
		    try
		    {
			    var result = new TopLevel() {Statements = new NodeList<Statement>()};
			    while (true)
			    {
				    var statement = ParseStatement();

				    if (statement == null)
					    break;
				    result.Statements.Add(statement);
			    }
			    return result;
		    }
		    catch (Exception)
		    {
			    Console.WriteLine("Parsing failed. Previous 20 tokens:");
			    var cursor = _scanResult.GetCursor();
				var firstToken = Math.Max(0, cursor - 20);
				_scanResult.SetCursor(firstToken);
			    for (int i = firstToken; i != cursor; i++)
			    {
					var token = _scanResult.Next();
				    Console.WriteLine(string.Format("cursor={0} type={1} literal='{2}'", i, token.tokenType, token.literal));
			    }
			    throw;
		    }
	    }

		public NodeList<Expression> ParseExpressionList()
        {
            var expressions = new List<Expression>();
            while (true)
            {
                var expression = ParseExpression();
                if (expression == null)
                    break;
                expressions.Add(expression);
            }
            return expressions.ToNodeList();
        }

        public Statement ParseStatement()
        {
            var scanToken = _scanResult.Peek();

            switch (scanToken.tokenType)
            {
                case TokenType.EOF:
                case TokenType.Case:
                case TokenType.AccoladeClose:
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
				case TokenType.VariableDereferencerOpen:
                    return ParseAssignmentOrExpressionStatement();
                case TokenType.On:
                    return ParseOnStatement();
                case TokenType.While:
                    return ParseWhileStatement();
				case TokenType.Include:
		            return ParseIncludeStatement();
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
                case TokenType.Switch:
                    return ParseSwitchStatement();
				case TokenType.Local:
		            return ParseLocalStatement();
                default:
                    throw new ParsingException("Unexpected token: "+scanToken.tokenType);
            }
        }

	    private IncludeStatement ParseIncludeStatement()
	    {
		    _scanResult.Next().Is(TokenType.Include);
		    var expression = ParseExpression();

		    _scanResult.Next().Is(TokenType.Terminator);
		    return new IncludeStatement() {Expression = expression};
	    }

	    private LocalStatement ParseLocalStatement()
	    {
			_scanResult.Next().Is(TokenType.Local);
		    var literalExpression = ParseExpression().As<LiteralExpression>();

		    var next = _scanResult.Next();
		    NodeList<Expression> value = new NodeList<Expression>();
		    if (next.tokenType != TokenType.Terminator)
		    {
			    next.Is(TokenType.Assignment);
			    value = ParseExpressionList();
			    _scanResult.Next().Is(TokenType.Terminator);
		    }
		    return new LocalStatement() {Variable = literalExpression, Value = value };
	    }

	    private SwitchStatement ParseSwitchStatement()
        {
            _scanResult.Next().Is(TokenType.Switch);

            var result = new SwitchStatement() {Variable = ParseExpression()};

            _scanResult.Next().Is(TokenType.AccoladeOpen);

            var switchCases = new NodeList<SwitchCase>();
            while (true)
            {
                var next = _scanResult.Next();
                if (next.tokenType == TokenType.AccoladeClose)
                    break;
                if (next.tokenType != TokenType.Case)
                    throw new ParsingException();

                var switchCase = new SwitchCase() {CaseExpression = ParseExpression().As<LiteralExpression>()};
                _scanResult.Next().Is(TokenType.Colon);

                var statements = new NodeList<Statement>();
                while (true)
                {
                    var statement = ParseStatement();
                    if (statement == null)
                        break;
                    statements.Add(statement);
                }
                switchCase.Statements = statements;
                switchCases.Add(switchCase);
            }

            result.Cases = switchCases;
            return result;
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
            return new OnStatement() {Target = ParseExpression(), Body = ParseStatement()};
        }

        private WhileStatement ParseWhileStatement()
        {
            _scanResult.Next().Is(TokenType.While);
            return new WhileStatement() { Condition = ParseCondition(), Body = ParseBlockStatement() };
        }

        private Statement ParseAssignmentOrExpressionStatement()
        {
            if (IsNextTokenAssignment())
            {
                var leftSideOfAssignment = ParseLeftSideOfAssignment();

                var assignmentToken = _scanResult.Next();
                var right = ParseExpressionList();
                _scanResult.Next().Is(TokenType.Terminator);

                return new AssignmentStatement() { Left = leftSideOfAssignment, Right = right, Operator = OperatorFor(assignmentToken.tokenType) };
            }
            
            var invocationExpression = new InvocationExpression {RuleExpression = ParseExpression(), Arguments = ParseArgumentList()};

            _scanResult.Next().Is(TokenType.Terminator);

            return new ExpressionStatement {Expression = invocationExpression};
        }

        private Expression ParseLeftSideOfAssignment()
        {
            var cursor = _scanResult.GetCursor();
            ParseExpression();
            var nextScanToken = _scanResult.Next();
            _scanResult.SetCursor(cursor);

            if (nextScanToken.tokenType == TokenType.On)
            {
                var variable = ParseExpression();
                _scanResult.Next().Is(TokenType.On);
                var targets = ParseExpressionList();
                return new VariableOnTargetExpression() { Variable = variable, Targets = targets };
            }

            if (nextScanToken.tokenType == TokenType.Assignment || nextScanToken.tokenType == TokenType.AppendOperator || nextScanToken.tokenType == TokenType.SubtractOperator || nextScanToken.tokenType == TokenType.AssignmentIfEmpty)
            {
                return ParseExpression();
            }

            throw new ParsingException();
        }

        private bool IsNextTokenAssignment()
        {
            var cursor = _scanResult.GetCursor();
            var leftSide = ParseExpression();
            var nextScanToken = _scanResult.Next();
            _scanResult.SetCursor(cursor);

            switch (nextScanToken.tokenType)
            {
                case TokenType.Assignment:
                case TokenType.AppendOperator:
                case TokenType.SubtractOperator:
                case TokenType.On:
				case TokenType.AssignmentIfEmpty:

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
                Name = expressionList.Last().As<LiteralExpression>().Value,
                Modifiers = expressionList.Take(expressionList.Length - 1).ToNodeList(),
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
                Arguments = arguments.SelectMany(ele => ele.Cast<LiteralExpression>()).Select(le => le.Value).ToArray(),
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
                    return new BlockStatement {Statements = statements.ToNodeList()};
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
		    var tokenType = _scanResult.Peek().tokenType;
		    if (IsBinaryOperator(tokenType))
			    return null;

		    switch (tokenType)
		    {
			    case TokenType.EOF:
			    case TokenType.Colon:
			    case TokenType.Terminator:
			    case TokenType.ParenthesisClose:
			    case TokenType.BracketClose:
				    return null;
				case TokenType.VariableDereferencerOpen:
				case TokenType.LiteralExpansionOpen:
				    return ParseExpansionStyleExpression();
			    case TokenType.AccoladeOpen:
				    return null;
			    case TokenType.BracketOpen:
				    return ParseInvocationExpression();

				case TokenType.Not:
				    _scanResult.Next();
				    var expression = ParseExpression();
				    if (expression is BinaryOperatorExpression)
					    throw new ParsingException("see IfStatementWithNegationAndRightSide test");
				    return new NotOperatorExpression { Expression = expression};
			    default:
				    return ScanForCombineExpression(new LiteralExpression {Value = _scanResult.Next().literal});
		    }
	    }

	    private Expression ParseInvocationExpression()
        {
            var bracketOpen = _scanResult.Next();
            if (bracketOpen.tokenType != TokenType.BracketOpen)
                throw new ParsingException();

            var ruleExpression = ParseExpression();
            var arguments = ParseArgumentList();
            var closeBracket = _scanResult.Next();
            if (closeBracket.tokenType != TokenType.BracketClose)
                throw new ParsingException();
            return new InvocationExpression {RuleExpression = ruleExpression.As<LiteralExpression>(), Arguments = arguments};
        }

        private Expression ParseExpansionStyleExpression()
        {
            var token = _scanResult.Next();

	        var result = 
				token.tokenType == TokenType.VariableDereferencerOpen
		        ? (ExpansionStyleExpression) new VariableDereferenceExpression()
		        : new LiteralExpansionExpression();

            result.VariableExpression = ParseExpression();

            var next = _scanResult.Next();

            if (next.tokenType == TokenType.BracketOpen)
            {
                result.IndexerExpression = ParseExpression();
                if (_scanResult.Next().tokenType != TokenType.BracketClose)
                    throw new ParsingException("Expected bracket close while parsing variable dereference expressions' indexer");
                next = _scanResult.Next();
            }

            var modifiers = new NodeList<VariableDereferenceModifier>();

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
            result.Modifiers = modifiers;

            next.Is(TokenType.ParenthesisClose);
            
            return ScanForCombineExpression(result);
        }

	    private static readonly Dictionary<TokenType, Operator> _binaryOperators = new Dictionary<TokenType, Operator>
	    {
		    {TokenType.AppendOperator, Operator.Append},
		    {TokenType.Assignment, Operator.Assignment},
		    {TokenType.SubtractOperator, Operator.Subtract},
		    {TokenType.In, Operator.In},
		    {TokenType.AssignmentIfEmpty, Operator.AssignmentIfEmpty},
		    {TokenType.And, Operator.And},
		    {TokenType.Or, Operator.Or},
		    {TokenType.NotEqual, Operator.NotEqual},
			{TokenType.GreaterThan, Operator.GreaterThan},
			{TokenType.LessThan, Operator.LessThan}
		};
		
		static bool IsBinaryOperator(TokenType tokenType)
		{
			return _binaryOperators.ContainsKey(tokenType);
		}

		private static Operator OperatorFor(TokenType tokenType)
		{
			return _binaryOperators[tokenType];
		}

        Expression ScanForCombineExpression(Expression firstExpression)
        {
            var peek = _scanResult.Peek(false);
            
            if (peek.tokenType == TokenType.EOF || (peek.tokenType != TokenType.Literal && peek.tokenType != TokenType.VariableDereferencerOpen && peek.tokenType != TokenType.LiteralExpansionOpen))
                return firstExpression;

            var tail = ParseExpression();
            var combineExpressionTail = tail as CombineExpression;

            var tailElements = combineExpressionTail != null
                ? combineExpressionTail.Elements
                : new NodeList<Expression>(new[] {tail});

            return new CombineExpression
            {
                Elements = tailElements.Prepend(firstExpression).ToNodeList()
            };
        }

        private NodeList<NodeList<Expression>> ParseArgumentList()
        {
            var expressionLists = new NodeList<NodeList<Expression>> ();
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

            return expressionLists;
        }
		
        public Expression ParseCondition()
        {
	        var left = this.ParseExpression();

	        var nextToken = this._scanResult.Peek().tokenType;
	        if (!IsBinaryOperator(nextToken))
		        return left;

	        this._scanResult.Next();
			
			if (nextToken == TokenType.In)
			{
				left = new BinaryOperatorExpression { Left = left, Operator = Operator.In, Right = ParseExpressionList() };
				nextToken = this._scanResult.Peek().tokenType;
				if (!IsBinaryOperator(nextToken))
					return left;
				this._scanResult.Next();
			}
			
	        var right = ParseCondition();
	        return new BinaryOperatorExpression() {Left = left, Operator = OperatorFor(nextToken), Right = new NodeList<Expression> { right } };
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
