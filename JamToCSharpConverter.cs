using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using jamconverter.AST;

namespace jamconverter
{
    class JamToCSharpConverter
    {
        readonly StringBuilder ruleMethods = new StringBuilder();

        public string Convert(string simpleProgram)
        {
   
            var csharpbody = new StringBuilder();

            var parser = new Parser(simpleProgram);
            var variables = new List<string>();
            while (true)
            {
                var statement = parser.ParseStatement();
                if (statement == null)
                    break;

                ProcessStatement(statement, csharpbody, variables);
            }

            var variableDeclarations = variables.Select(v => "JamList " + v + ";\n").SeperateWithSpace();

            return 
       $@"
using System;

partial class Dummy
{{
    {ruleMethods}

    static void Main()
    {{
       {variableDeclarations}
       {csharpbody}
    }}
}}";
        }

        private void ProcessStatement(Statement statement, StringBuilder csharpbody, List<string> variables)
        {
            var ifStatement = statement as IfStatement;
            if (ifStatement != null)
            {
                var condition = ifStatement.Condition;
                
                var vde = condition as VariableDereferenceExpression;

                if (vde != null)
                {
                    var variableName = VariableNameFor((LiteralExpression) vde.VariableExpression);
                    csharpbody.AppendLine($"if ({variableName} != null) {{");
                }

                var boe = condition as BinaryOperatorExpression;
                if (boe != null)
                    csharpbody.AppendLine($"if ({CSharpFor(boe.Left)}.JamEquals({CSharpFor(boe.Right)})) {{");
                
                foreach (var subStatement in ifStatement.Body.Statements)
                    ProcessStatement(subStatement, csharpbody, variables);

                csharpbody.AppendLine("}");
                return;
            }

            var ruleDeclaration = statement as RuleDeclarationStatement;
            if (ruleDeclaration != null)
            {
                var ruleMethodCsharp = new StringBuilder();

                //because the parser always interpets an invocation without any arguments as one with a single argument: an empty expressionlist,  let's make sure we always are ready to take a single argument
                var arguments = ruleDeclaration.Arguments.Length == 0
                    ? new[] {"dummyArgument"}
                    : ruleDeclaration.Arguments;

                ruleMethodCsharp.AppendLine($"public static JamList {MethodNameFor(ruleDeclaration)}({arguments.Select(a => $"JamList {ArgumentNameFor(a)}").SeperateWithComma()}) {{");
                foreach (var subStatement in ruleDeclaration.Body.Statements)
                    ProcessStatement(subStatement, ruleMethodCsharp, variables);

                if (!(ruleDeclaration.Body.Statements.Last() is ReturnStatement))
                    ruleMethodCsharp.AppendLine("return null;");
                ruleMethodCsharp.AppendLine("}");
                ruleMethods.Append(ruleMethodCsharp);
                return;
            }

            var returnStatement = statement as ReturnStatement;
            if (returnStatement != null)
            {
                csharpbody.AppendLine($"return {CSharpFor(returnStatement.ReturnExpression)};");
                return;
            }

            var expressionStatement = (ExpressionStatement)statement;
            var invocationExpression = expressionStatement.Expression as InvocationExpression;

            if (invocationExpression != null)
            {
                csharpbody.AppendLine($"{CSharpFor(invocationExpression)};");
            }

            var assignmentExpression = expressionStatement.Expression as BinaryOperatorExpression;
            if (assignmentExpression != null)
            {
                var variableName = VariableNameFor((LiteralExpression) assignmentExpression.Left);
                if (!variables.Contains(variableName))
                    variables.Add(variableName);

                var valueArguments =
                    assignmentExpression.Right.Expressions.Select(
                        e => ((LiteralExpression)e).Value);

                var value = $"new JamList({valueArguments.InQuotes().SeperateWithComma()})";

                switch (assignmentExpression.Operator)
                {
                    case Operator.Assignment:
                        csharpbody.AppendLine($"{variableName} = {value};");
                        break;
                    case Operator.Append:
                        csharpbody.AppendLine($"{variableName}.Append({value});");
                        break;
                }
            }
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

        string CSharpFor(ExpressionList expressionList)
        {
            return $"new JamList({expressionList.Expressions.Select(CSharpFor).SeperateWithComma()})";
        }

        string CSharpFor(Expression e)
        {
            var literalExpression = e as LiteralExpression;
            if (literalExpression != null)
                return $"new JamList({literalExpression.Value.InQuotes()})";
            var dereferenceExpression = e as VariableDereferenceExpression;
            if (dereferenceExpression != null)
            {
                var sb = new StringBuilder(VariableNameFor((LiteralExpression) dereferenceExpression.VariableExpression));

                if (dereferenceExpression.IndexerExpression != null)
                    sb.Append($".IndexedBy({CSharpFor(dereferenceExpression.IndexerExpression)})");

                foreach (var modifier in dereferenceExpression.Modifiers)
                {
                    switch (modifier.Command)
                    {
                        case 'S':
                            var valueStr = modifier.Value == null ? "new JamList(\"\")" : CSharpFor(modifier.Value);
                            sb.Append($".WithSuffix({valueStr})");
                            break;
                        case 'E':
                            sb.Append($".IfEmptyUse({CSharpFor(modifier.Value)})");
                            break;
                        default:
                            throw new NotSupportedException("Unkown variable expansion command: "+modifier.Command);
                    }
                }
                return sb.ToString();
            }
            var combineExpression = e as CombineExpression;
            if (combineExpression != null)
                return $"JamList.Combine({combineExpression.Elements.Select(CSharpFor).SeperateWithComma()})";

            var invocationExpression = e as InvocationExpression;
            if (invocationExpression != null)
            {
                var literalRule = (LiteralExpression) invocationExpression.RuleExpression;
                var methodName = MethodNameFor(literalRule.Value);
                return $"{methodName}({invocationExpression.Arguments.Select(CSharpFor).SeperateWithComma()})";
            }

            if (e == null)
                return "new JamList(new string[0])";
            throw new ParsingException("CSharpFor cannot deal with " + e);
        }
    }
}
