using System;
using System.Collections.Generic;
using System.Linq;

namespace jamconverter
{
    public class Scanner
    {
        private readonly string _input;
        private int nextChar = 0;
        private readonly Queue<ScanResult> _unscanBuffer = new Queue<ScanResult>();
        private bool _insideVariableExpansionModifierSpan;

        public Scanner(string input)
        {
            _input = input;
        }

        public ScanResult Scan()
        {
            if (_unscanBuffer.Any())
                return _unscanBuffer.Dequeue();

            if (nextChar >= _input.Length)
                return null;
            
            var c = _input[nextChar];

            if (_insideVariableExpansionModifierSpan)
            {
                if (c == '=')
                {
                    nextChar++;
                    _insideVariableExpansionModifierSpan = false;
                    return new ScanResult() { tokenType = TokenType.Assignment, literal = c.ToString() };
                }
                if (char.IsLetter(c))
                {
                    nextChar++;
                    return new ScanResult() { tokenType = TokenType.VariableExpansionModifier, literal = c.ToString()};
                }
                if (c == ')')
                    _insideVariableExpansionModifierSpan = false;
            }

            if (char.IsWhiteSpace(c))
                return new ScanResult() { tokenType = TokenType.WhiteSpace, literal = ReadWhiteSpace() };
            
            var literal = ReadLiteral();

            if (literal==":")
                if (!char.IsWhiteSpace(NextChar()))
                    _insideVariableExpansionModifierSpan = true;

            return new ScanResult() {tokenType = TokenTypeFor(literal), literal = literal};
        }

        char NextChar()
        {
            if (_unscanBuffer.Any())
                return _unscanBuffer.Peek().literal[0];
            return _input[nextChar];
        }
        
        private TokenType TokenTypeFor(string literal)
        {
            if (literal == ";")
                return TokenType.Terminator;

            if (literal == "[")
                return TokenType.BracketOpen;

            if (literal == "]")
                return TokenType.BracketClose;

            if (literal == ":")
                return TokenType.Colon;

            if (literal == "$")
                return TokenType.VariableDereferencer;

            if (literal == "(")
                return TokenType.ParenthesisOpen;

            if (literal == ")")
                return TokenType.ParenthesisClose;

            if (literal == "{")
                return TokenType.AccoladeOpen;

            if (literal == "}")
                return TokenType.AccoladeClose;

            if (literal == "=")
                return TokenType.Assignment;

            if (literal == "if")
                return TokenType.If;

            if (literal == "rule")
                return TokenType.Rule;

            if (literal == "return")
                return TokenType.Return;

            return TokenType.Literal;
        }

        public IEnumerable<ScanResult> ScanAll()
        {
            while (true)
            {
                var sr = Scan();
                if (sr == null)
                    yield break;
                yield return sr;
            }
        }

        private string ReadLiteral()
        {
            int i;
            for (i = nextChar; i != _input.Length; i++)
            {
                if (IsLiteral(_input[i]))
                    continue;
                break;
            }

            if (i == nextChar)
            {
                nextChar++;
                return _input[i].ToString();
            }

            var result = _input.Substring(nextChar, Math.Max(1,i - nextChar));
            nextChar = i;
            return result;
        }

        private bool IsLiteral(char c)
        {
            if (c == ')')
                return false;
            if (c == '(')
                return false;
            if (c == '}')
                return false;
            if (c == '{')
                return false;
            if (c == '$')
                return false;
            if (c == ':')
                return false;
            return !char.IsWhiteSpace(c);
        }

        private string ReadWhiteSpace()
        {
            for (int i = nextChar; i != _input.Length; i++)
            {
                if (!char.IsWhiteSpace(_input[i]))
                {
                    var result = _input.Substring(nextChar, i - nextChar);
                    nextChar = i;
                    return result;
                }
            }
            var result2 = _input.Substring(nextChar);
            nextChar = _input.Length;
            return result2;
        }

        public ScanResult ScanSkippingWhiteSpace()
        {
            while (true)
            {
                var sr = Scan();
                if (sr != null && sr.tokenType == TokenType.WhiteSpace)
                    continue;
                return sr;
            }
        }

        public void UnScan(ScanResult sr)
        {
            _unscanBuffer.Enqueue(sr);
        }
    }

    public class ScanResult
    {
        public TokenType tokenType;
        public string literal;
    }

    public enum TokenType
    {
        Literal,
        Terminator,
        WhiteSpace,
        BracketClose,
        Colon,
        BracketOpen,
        VariableDereferencer,
        ParenthesisClose,
        ParenthesisOpen,
        Assignment,
        AccoladeOpen,
        AccoladeClose,
        If,
        Rule,
        VariableExpansionModifier,
        Return
    }
}
