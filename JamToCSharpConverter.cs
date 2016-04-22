using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;

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

class Dummy
{{
    {ruleMethods}

    static void Echo(params JamList[] values)
    {{
        foreach(var value in values)
        {{
               Console.Write(value.ToString());
               Console.Write("" "");
        }}
        Console.WriteLine();
    }}

    static void Main()
    {{
       {variableDeclarations}
       {csharpbody}
    }}
}}";
        }

        private void ProcessNode(Node node, StringBuilder csharpbody, List<string> variables)
        {
            if (node is EmptyExpression)
                return;

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
                ruleMethodCsharp.AppendLine("return null;");
                ruleMethodCsharp.AppendLine("}");
                ruleMethods.Append(ruleMethodCsharp);
                return;
            }

            var expressionStatement = (ExpressionStatement)node;
            var invocationExpression = expressionStatement.Expression as InvocationExpression;

            if (invocationExpression != null)
            {
                var literalRule = (LiteralExpression)invocationExpression.RuleExpression;

                csharpbody.AppendLine($"{literalRule.Value}({invocationExpression.Arguments.Select(CSharpFor).SeperateWithComma()});");

                /*
                if (literalRule.Value == "Echo")
                {
                    var expressionListExpression = invocationExpression.Arguments[0] as ExpressionListExpression;
                    if (expressionListExpression != null)
                    {
                        csharpbody.AppendLine($"System.Console.Write({CSharpFor(expressionListExpression)});");
                        csharpbody.AppendLine("System.Console.WriteLine();");
                    }
                }*/
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
                return ((LiteralExpression) dereferenceExpression.VariableExpression).Value;

            var combineExpression = e as CombineExpression;
            if (combineExpression != null)
                return $"JamList.Combine({combineExpression.Elements.Select(CSharpFor).SeperateWithComma()})";

            var expressionListExpression = e as ExpressionListExpression;
            if (expressionListExpression != null)
                return $"new JamList({expressionListExpression.Expressions.Select(CSharpFor).SeperateWithComma()})";

            throw new ParsingException("CSharpFor cannot deal with " + e);
        }
    }
}
