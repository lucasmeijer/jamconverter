using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
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
                var node = parser.Parse();
                if (node == null)
                    break;

                ProcessNode(node, csharpbody, variables);
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

        private void ProcessNode(Node node, StringBuilder csharpbody, List<string> variables)
        {
            var ifStatement = node as IfStatement;
            if (ifStatement != null)
            {
                var vde = (VariableDereferenceExpression) ifStatement.Condition;
                var variableName = ((LiteralExpression) vde.VariableExpression).Value;

                csharpbody.AppendLine($"if ({variableName} != null) {{");

                foreach (var statement in ifStatement.Body.Statements)
                    ProcessNode(statement, csharpbody, variables);

                csharpbody.AppendLine("}");
                return;
            }

            var ruleDeclaration = node as RuleDeclaration;
            if (ruleDeclaration != null)
            {
                var ruleMethodCsharp = new StringBuilder();
                ruleMethodCsharp.AppendLine($"public static JamList {ruleDeclaration.Name}({ruleDeclaration.Arguments.Select(a => $"JamList {a}").SeperateWithComma()}) {{");
                foreach (var statement in ruleDeclaration.Body.Statements)
                    ProcessNode(statement, ruleMethodCsharp, variables);

                if (!(ruleDeclaration.Body.Statements.Last() is ReturnStatement))
                    ruleMethodCsharp.AppendLine("return null;");
                ruleMethodCsharp.AppendLine("}");
                ruleMethods.Append(ruleMethodCsharp);
                return;
            }

            var returnStatement = node as ReturnStatement;
            if (returnStatement != null)
            {
                csharpbody.AppendLine($"return {CSharpFor(returnStatement.ReturnExpression)};");
                return;
            }

            var expressionStatement = (ExpressionStatement)node;
            var invocationExpression = expressionStatement.Expression as InvocationExpression;

            if (invocationExpression != null)
            {
                csharpbody.AppendLine($"{CSharpFor(invocationExpression)};");
            }

            var assignmentExpression = expressionStatement.Expression as AssignmentExpression;
            if (assignmentExpression != null)
            {
                var variableName = ((LiteralExpression) assignmentExpression.Left).Value;
                if (!variables.Contains(variableName))
                    variables.Add(variableName);

                var values =
                    ((ExpressionListExpression) assignmentExpression.Right).Expressions.Select(
                        e => ((LiteralExpression) e).Value);

                csharpbody.AppendLine($"{variableName} = new JamList({values.InQuotes().SeperateWithComma()});");
            }
        }

        string CSharpFor(Expression e)
        {
            var literalExpression = e as LiteralExpression;
            if (literalExpression != null)
                return $"new JamList({literalExpression.Value.InQuotes()})";
            var dereferenceExpression = e as VariableDereferenceExpression;
            if (dereferenceExpression != null)
            {
                var sb = new StringBuilder(((LiteralExpression) dereferenceExpression.VariableExpression).Value);
                foreach (var modifier in dereferenceExpression.Modifiers)
                {
                    switch (modifier.Command)
                    {
                        case 'S':
                            var valueStr = modifier.Value == null ? "new JamList(\"\")" : CSharpFor(modifier.Value);
                            sb.Append($".WithSuffix({valueStr})");
                            break;
                        default:
                            throw new NotSupportedException();
                    }
                }
                return sb.ToString();
            }
            var combineExpression = e as CombineExpression;
            if (combineExpression != null)
                return $"JamList.Combine({combineExpression.Elements.Select(CSharpFor).SeperateWithComma()})";

            var expressionListExpression = e as ExpressionListExpression;
            if (expressionListExpression != null)
                return $"new JamList({expressionListExpression.Expressions.Select(CSharpFor).SeperateWithComma()})";

            var invocationExpression = e as InvocationExpression;
            if (invocationExpression != null)
            {
                var literalRule = (LiteralExpression)invocationExpression.RuleExpression;
                return $"{literalRule.Value}({invocationExpression.Arguments.Select(CSharpFor).SeperateWithComma()})";
            }

            if (e == null)
                return "new JamList(new string[0])";
            throw new ParsingException("CSharpFor cannot deal with " + e);
        }
    }
}
