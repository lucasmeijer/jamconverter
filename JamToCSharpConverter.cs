using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;

namespace jamconverter
{
    class JamToCSharpConverter
    {
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
class Dummy
{{
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

            var expressionStatement = node as ExpressionStatement;

            var invocationExpression = expressionStatement.Expression as InvocationExpression;

            if (invocationExpression != null)
            {
                var literalRule = invocationExpression.RuleExpression as LiteralExpression;
                if (literalRule.Value == "Echo")
                {
                    var expressionListExpression = invocationExpression.Arguments[0] as ExpressionListExpression;
                    if (expressionListExpression != null)
                    {
                        foreach (var expression in expressionListExpression.Expressions)
                        {
                            csharpbody.AppendLine($"System.Console.Write({CSharpFor(expression)});");
                        }
                        csharpbody.AppendLine($"System.Console.WriteLine();");
                    }
                }
            }

            var assignmentExpression = expressionStatement.Expression as AssignmentExpression;
            if (assignmentExpression != null)
            {
                var variableName = ((LiteralExpression) assignmentExpression.Left).Value;
                if (!variables.Contains(variableName))
                    variables.Add(variableName);

                var value =
                    ((LiteralExpression) ((ExpressionListExpression) assignmentExpression.Right).Expressions[0])
                        .Value;
                csharpbody.AppendLine($"{variableName} = new JamList(\"{value}\");");
            }
        }

        string CSharpFor(Expression e)
        {
            var literalExpression = e as LiteralExpression;
            if (literalExpression != null)
                return "\"" + literalExpression.Value + " \"";
            var dereferenceExpression = e as VariableDereferenceExpression;
            if (dereferenceExpression != null)
                return ((LiteralExpression) dereferenceExpression.VariableExpression).Value;
            throw new ParsingException();
        }
    }
}
