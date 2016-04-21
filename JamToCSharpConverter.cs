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
            var expressions = new Lexer().Lex(simpleProgram);

            var builder = new StringBuilder(
            @"
class Dummy
{
    static void Main()
    {
");

            var variables = new List<string>();
            foreach (var expression in expressions)
            {
                if (IsAssignment(expression))
                {
                    var targetVariableName = expression[0];
                    if (!variables.Contains(targetVariableName))
                    {
                        builder.AppendLine($"string {targetVariableName};");
                        variables.Add(targetVariableName);
                    }
                    builder.Append($"{targetVariableName} = \"{expression[2]}\";");
                    continue;
                }

                if (expression[0] == "Echo")
                {
                    bool first = true;
                    foreach (var token in expression.Skip(1))
                    {
                        if (!first)
                            builder.AppendLine("System.Console.Write(\" \");");
                        var value = IsVariableExpansion(token) ? ExpandVariable(token) : "\"" + token + "\"";
                        builder.AppendLine("System.Console.Write(" + value + ");");

                        first = false;
                    }
                    builder.AppendLine("System.Console.WriteLine();");
                }


            }


            builder.Append(@"
    }
}
");              

            return builder.ToString();
        }

        private static Regex s_variableRegex = new Regex(@"\$\((.*)\)");

        private bool IsVariableExpansion(string token)
        {
            return s_variableRegex.IsMatch(token);
        }

        string ExpandVariable(string token)
        {
            return s_variableRegex.Match(token).Groups[1].Value;
        }

        private bool IsAssignment(string[] expression)
        {
            if (expression.Length < 2)
                return false;
            return expression[1] == "=";
        }
    }

    internal class Lexer
    {
        public IEnumerable<string[]> Lex(string simpleProgram)
        {
            var buf = new List<string>();
            var tokens = new Queue<string>(simpleProgram.Split(new[] {" "}, StringSplitOptions.RemoveEmptyEntries));

            while (tokens.Any())
            {
                var token = tokens.Dequeue();

                if (token == "rule")
                {
                    yield return EatTokensUntilEndOfScopeBlock(tokens).Prepend(token).ToArray();
                }
            }

            foreach (var token in tokens)
            {
                if (token == "rule")

                if (token != ";")
                    buf.Add(token);
                else
                {
                    yield return buf.ToArray();
                    buf.Clear();
                }
            }
        }

        private IEnumerable<string> EatTokensUntilEndOfScopeBlock(Queue<string> tokens)
        {
            yield break;
        }
    }
}
