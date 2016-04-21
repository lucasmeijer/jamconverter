using System;
using System.Collections.Generic;
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

            foreach (var expression in expressions)
            {
                if (IsAssignment(expression))
                {
                    builder.Append($"string {expression[0]} = \"{expression[2]}\";");
                    continue;
                }

                if (expression[0] != "Echo")
                    throw new NotSupportedException();

                var token = expression[1];
                var value = IsVariableExpansion(token) ? ExpandVariable(token) : "\""+token + "\"";

                builder.AppendLine("System.Console.WriteLine(" + value + ");");
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
            foreach (var token in simpleProgram.Split(new[] {" "}, StringSplitOptions.RemoveEmptyEntries))
            {
                if (token != ";")
                    buf.Add(token);
                else
                {
                    yield return buf.ToArray();
                    buf.Clear();
                }
            }
        }
    }
}
