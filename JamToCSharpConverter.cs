using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using jamconverter.AST;
using NRefactory = ICSharpCode.NRefactory.CSharp;

namespace jamconverter
{
    class JamToCSharpConverter
    {
        private NRefactory.SyntaxTree _syntaxTree;
        private NRefactory.TypeDeclaration _dummyType;
        private NRefactory.MethodDeclaration _mainMethod;

        public string Convert(string simpleProgram)
        {
            _syntaxTree = new NRefactory.SyntaxTree();
            _syntaxTree.Members.Add(new NRefactory.UsingDeclaration("System"));
            _syntaxTree.Members.Add(new NRefactory.UsingDeclaration("System.Linq"));
            _syntaxTree.Members.Add(new NRefactory.UsingDeclaration("static BuiltinFunctions"));
            _dummyType = new NRefactory.TypeDeclaration {Name = "Dummy"};
            _syntaxTree.Members.Add(_dummyType);


            new List<NRefactory.Statement>();
            var parser = new Parser(simpleProgram);
            var body = new NRefactory.BlockStatement();
            while (true)
            {
                var statement = parser.ParseStatement();
                if (statement == null)
                    break;

                var nStatement = ProcessStatement(statement);
                if (nStatement != null)
                    body.Statements.Add(nStatement);
            }
            
            _mainMethod = new NRefactory.MethodDeclaration { Name = "Main", ReturnType = new NRefactory.PrimitiveType("void"), Modifiers = NRefactory.Modifiers.Static, Body = body};
            _dummyType.Members.Add(_mainMethod);

            return _syntaxTree.ToString();
        }

        private NRefactory.Statement ProcessStatement(Statement statement, StringBuilder csharpbody=null, List<string> variables=null)
        {
            if (statement == null)
                return null;

            if (statement is IfStatement)
                return ProcessIfStatement((IfStatement) statement);

            if (statement is WhileStatement)
                return ProcessWhileStatement((WhileStatement) statement);

            if (statement is RuleDeclarationStatement)
            {
                ProcessRuleDeclarationStatement(variables, (RuleDeclarationStatement) statement);
                return null;
            }

            if (statement is ReturnStatement)
                return new NRefactory.ReturnStatement(ProcessExpressionList(statement.As<ReturnStatement>().ReturnExpression));

            if (statement is ForStatement)
                return ProcessForStatement((ForStatement) statement);

            if (statement is BreakStatement)
                return new NRefactory.BreakStatement();

            if (statement is ContinueStatement)
                return new NRefactory.ContinueStatement();

            if (statement is BlockStatement)
                return ProcessBlockStatement((BlockStatement) statement);

            if (statement is SwitchStatement)
                return ProcessSwitchStatement((SwitchStatement) statement);

            return ProcessExpressionStatement((ExpressionStatement) statement);
        }
        
        private NRefactory.SwitchStatement ProcessSwitchStatement(SwitchStatement switchStatement)
        {
            var invocationExpression = new NRefactory.InvocationExpression(new NRefactory.IdentifierExpression("SwitchTokenFor"), ProcessExpression(switchStatement.Variable));
            var result = new NRefactory.SwitchStatement() {Expression = invocationExpression};
           
            foreach(var switchCase in switchStatement.Cases)
            {
                var section = new NRefactory.SwitchSection();
                section.CaseLabels.Add(new NRefactory.CaseLabel(new NRefactory.PrimitiveExpression(switchCase.CaseExpression.Value)));
                section.Statements.AddRange(switchCase.Statements.Select(s => ProcessStatement(s)));
                section.Statements.Add(new NRefactory.BreakStatement());
                result.SwitchSections.Add(section);
            }
            return result;
        }
        
        private NRefactory.ForeachStatement ProcessForStatement(ForStatement statement)
        {
            return new NRefactory.ForeachStatement
            {
                VariableType = JamListAstType,
                VariableName = statement.LoopVariable.Value,
                InExpression = ProcessExpressionList(statement.List),
                EmbeddedStatement = ProcessStatement(statement.Body)
            };
        }

        private NRefactory.ExpressionStatement ProcessExpressionStatement(ExpressionStatement expressionStatement)
        {
            if (expressionStatement.Expression is InvocationExpression)
                return new NRefactory.ExpressionStatement(ProcessExpression(expressionStatement.Expression));

            if (expressionStatement.Expression is BinaryOperatorExpression)
                return new NRefactory.ExpressionStatement(ProcessAssignmentExpressionStatement((BinaryOperatorExpression) expressionStatement.Expression));

            throw new ArgumentException("Unsupported expression: " + expressionStatement.Expression);
        }

        private NRefactory.Expression ProcessAssignmentExpressionStatement(BinaryOperatorExpression assignmentExpression)
        {
            var variableName = VariableNameFor((LiteralExpression)assignmentExpression.Left);
            if (_dummyType.Members.OfType<NRefactory.FieldDeclaration>().All(f => f.Variables.Any(v => v.Name != variableName)))
                _dummyType.Members.Add(new NRefactory.FieldDeclaration() {Variables = { new NRefactory.VariableInitializer(variableName) } , ReturnType = JamListAstType, Modifiers = NRefactory.Modifiers.Static});

            var leftExpression = new NRefactory.IdentifierExpression(VariableNameFor(assignmentExpression.Left.As<LiteralExpression>()));
            switch (assignmentExpression.Operator)
            {
                case Operator.Assignment:
                    
                    return new NRefactory.AssignmentExpression(leftExpression, NRefactory.AssignmentOperatorType.Assign, ProcessExpressionList(assignmentExpression.Right));

                default:
                    var csharpMethodNameForAssignmentOperator = CsharpMethodNameForAssignmentOperator(assignmentExpression.Operator);
                    var memberReferenceExpression = new NRefactory.MemberReferenceExpression(leftExpression, csharpMethodNameForAssignmentOperator);
                    var processExpression = ProcessExpressionList(assignmentExpression.Right);
                    return new NRefactory.InvocationExpression(memberReferenceExpression, processExpression);
            }
        }

        private static string CsharpMethodNameForAssignmentOperator(Operator assignmentOperator)
        {
            switch (assignmentOperator)
            {
                case Operator.Append:
                    return "Append";
                case Operator.Subtract:
                    return "Subtract";
                default:
                    throw new NotSupportedException("Unsupported operator in assignment: " + assignmentOperator);
            }
        }

        private void ProcessRuleDeclarationStatement(List<string> variables, RuleDeclarationStatement ruleDeclaration)
        {
            //because the parser always interpets an invocation without any arguments as one with a single argument: an empty expressionlist,  let's make sure we always are ready to take a single argument
            var arguments = ruleDeclaration.Arguments.Length == 0 ? new[] { "dummyArgument" } : ruleDeclaration.Arguments;

            var body = new NRefactory.BlockStatement();

            var processRuleDeclarationStatement = new NRefactory.MethodDeclaration()
            {
                Name = MethodNameFor(ruleDeclaration),
                ReturnType = JamListAstType,
                Modifiers = NRefactory.Modifiers.Static,
                Body = body
            };
            processRuleDeclarationStatement.Parameters.AddRange(arguments.Select(a => new NRefactory.ParameterDeclaration(JamListAstType, ArgumentNameFor(a))));

            foreach (var subStatement in ruleDeclaration.Body.Statements)
                body.Statements.Add(ProcessStatement(subStatement, null, variables));

            if (!(ruleDeclaration.Body.Statements.Last() is ReturnStatement))
                body.Statements.Add(new NRefactory.ReturnStatement(new NRefactory.NullReferenceExpression()));
            
            _dummyType.Members.Add(processRuleDeclarationStatement);
        }

        public static NRefactory.AstType JamListAstType => new NRefactory.SimpleType("JamList");

        private NRefactory.IfElseStatement ProcessIfStatement(IfStatement ifStatement)
        {
            return new NRefactory.IfElseStatement(ProcessCondition(ifStatement.Condition), ProcessStatement(ifStatement.Body), ProcessStatement(ifStatement.Else));
        }
        
        private NRefactory.BlockStatement ProcessBlockStatement(BlockStatement blockStatement)
        {
            var processBlockStatement = new NRefactory.BlockStatement();
            processBlockStatement.Statements.AddRange(blockStatement.Statements.Select(s => ProcessStatement(s)));
            return processBlockStatement;
        }

        private NRefactory.WhileStatement ProcessWhileStatement(WhileStatement whileStatement)
        {
            return new NRefactory.WhileStatement(ProcessCondition(whileStatement.Condition), ProcessStatement(whileStatement.Body, null, null));
        }

        private string ArgumentNameFor(string argumentName)
        {
            return CleanIllegalCharacters(argumentName);
        }

        private static string MethodNameFor(string ruleName)
        {
            return CleanIllegalCharacters(ruleName);
        }

        private static string MethodNameFor(RuleDeclarationStatement ruleDeclarationStatement)
        {
            return MethodNameFor(ruleDeclarationStatement.Name);
        }

        private static string VariableNameFor(LiteralExpression variableExpression)
        {
            return CleanIllegalCharacters(variableExpression.Value);
        }

        static string CleanIllegalCharacters(string input)
        {
            return input.Replace(".", "_");
        }
    
        NRefactory.Expression ProcessCondition(Condition condition)
        {
            var conditionWithoutNegation = ConditionWithoutNegation(condition);

            return condition.Negated ? new NRefactory.UnaryOperatorExpression(NRefactory.UnaryOperatorType.Not, conditionWithoutNegation) : conditionWithoutNegation;
        }

        private NRefactory.Expression ConditionWithoutNegation(Condition condition)
        {
            if (condition.Right == null)
                return new NRefactory.InvocationExpression(new NRefactory.MemberReferenceExpression(ProcessExpression(condition.Left), "AsBool"));

            return new NRefactory.InvocationExpression(new NRefactory.MemberReferenceExpression(ProcessExpression(condition.Left), CSharpMethodForConditionOperator(condition.Operator)), ProcessExpressionList(condition.Right));
        }

        string CSharpMethodForConditionOperator(Operator @operator)
        {
            switch (@operator)
            {
                case Operator.Assignment:
                    return "JamEquals";
                case Operator.In:
                    return "IsIn";
                default:
                    throw new NotSupportedException("Unknown conditional operator: "+@operator);
            }
        }

        public NRefactory.Expression ProcessExpressionList(ExpressionList expressionList)
        {
            NRefactory.Expression result = new NRefactory.ObjectCreateExpression(JamListAstType);
 
            foreach(var expression in expressionList.Expressions)
                result = new NRefactory.InvocationExpression(new NRefactory.MemberReferenceExpression(result, "With"), ProcessExpression(expression));

            return result;
        }

        private IEnumerable<LiteralExpression> PopAllLiteralExpressionsFromQueue(Queue<Expression> queue)
        {
            while(queue.Any())
            {
                if (queue.Peek() is LiteralExpression)
                    yield return (LiteralExpression) queue.Dequeue();
                else
                    yield break;
            }
        }

        NRefactory.Expression ProcessExpression(Expression e)
        {
            var literalExpression = e as LiteralExpression;
            if (literalExpression != null)
                return new NRefactory.ObjectCreateExpression(JamListAstType, new NRefactory.PrimitiveExpression(literalExpression.Value));
                        
            var dereferenceExpression = e as VariableDereferenceExpression;
            if (dereferenceExpression != null)
                return ProcessVariableDereferenceExpression(dereferenceExpression);

            var combineExpression = e as CombineExpression;
            if (combineExpression != null)
            {
                var combineMethod = new NRefactory.MemberReferenceExpression(new NRefactory.IdentifierExpression("JamList"), "Combine");
                return new NRefactory.InvocationExpression(combineMethod, combineExpression.Elements.Select(ProcessExpression));
            }

            var invocationExpression = e as InvocationExpression;
            if (invocationExpression != null)
            {
                var methodName = MethodNameFor(invocationExpression.RuleExpression.As<LiteralExpression>().Value);

                return new NRefactory.InvocationExpression(new NRefactory.IdentifierExpression(methodName), invocationExpression.Arguments.Select(ProcessExpressionList));
            }

            if (e == null)
                return new NRefactory.ObjectCreateExpression(JamListAstType);

            throw new ParsingException("CSharpFor cannot deal with " + e);
        }

        private NRefactory.Expression ProcessVariableDereferenceExpression(VariableDereferenceExpression dereferenceExpression)
        {
            var variableExpression = new NRefactory.IdentifierExpression(VariableNameFor(dereferenceExpression.VariableExpression.As<LiteralExpression>()));
            NRefactory.Expression resultExpression = variableExpression;

            if (dereferenceExpression.IndexerExpression != null)
            {
                var memberReferenceExpression = new NRefactory.MemberReferenceExpression(resultExpression, "IndexedBy");
                var indexerExpression = ProcessExpression(dereferenceExpression.IndexerExpression);
                resultExpression = new NRefactory.InvocationExpression(memberReferenceExpression, indexerExpression);
            }

            foreach (var modifier in dereferenceExpression.Modifiers)
            {
                var csharpMethod = CSharpMethodForModifier(modifier);

                var memberReferenceExpression = new NRefactory.MemberReferenceExpression(resultExpression, csharpMethod);
                var valueExpression = ProcessExpression(modifier.Value);
                resultExpression = new NRefactory.InvocationExpression(memberReferenceExpression, valueExpression);
            }
            return resultExpression;
        }

        private string CSharpMethodForModifier(VariableDereferenceModifier modifier)
        {
            switch (modifier.Command)
            {
                case 'S':
                    return "WithSuffix";
                case 'E':
                    return "IfEmptyUse";
                case 'G':
                    return "GristWith";
                case 'J':
                    return "JoinWithValue";
                default:
                    throw new NotSupportedException("Unkown variable expansion command: " + modifier.Command);
            }
        }
    }
}
