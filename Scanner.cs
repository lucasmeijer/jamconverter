using System;
using System.Collections.Generic;
using System.Linq;

namespace jamconverter
{
    public class Scanner
    {
        private readonly string _input;
        private int nextChar = 0;
        private bool _insideVariableExpansionModifierSpan;

        public Scanner(string input)
        {
            _input = input;
        }

        public ScanResult Scan()
        {
            return new ScanResult(ScanAllTokens().ToArray());
        }

        public IEnumerable<ScanToken> ScanAllTokens()
        {
            while (true)
            {
                var sr = ScanToken();
                yield return sr;
                if (sr.tokenType == TokenType.EOF)
                    yield break;
                
            }
        }

        public ScanToken ScanToken()
        {
            if (nextChar >= _input.Length)
                return new ScanToken() { literal="", tokenType = TokenType.EOF};

            var c = _input[nextChar];

            if (_insideVariableExpansionModifierSpan)
            {
                if (c == '=')
                {
                    _insideVariableExpansionModifierSpan = false;
                    nextChar++;
                    var scanToken = new ScanToken() { tokenType = TokenType.Assignment, literal = c.ToString() };
                    return scanToken;
                }
                if (char.IsLetter(c))
                {
                    nextChar++;
                    var scanToken = new ScanToken() { tokenType = TokenType.VariableExpansionModifier, literal = c.ToString() };
                    return scanToken;
                }
                if (c == ')')
                    _insideVariableExpansionModifierSpan = false;
            }

            if (char.IsWhiteSpace(c))
                return new ScanToken() { tokenType = TokenType.WhiteSpace, literal = ReadWhiteSpace() };

            if (c == '#')
            {
                ReadUntilEndOfLine();
                return ScanToken();
            }
            
            var literal = ReadLiteral();

            if (literal==":")
                if (!char.IsWhiteSpace(_input[nextChar]))
                    _insideVariableExpansionModifierSpan = true;

            return new ScanToken() {tokenType = TokenTypeFor(literal), literal = literal};
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
            
            if (literal == "actions")
                return TokenType.Actions;

            if (literal == "on")
                return TokenType.On;

            if (literal == "!")
                return TokenType.Not;

            if (literal == "else")
                return TokenType.Else;

            if (literal == "+=")
                return TokenType.AppendOperator;

            return TokenType.Literal;
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
            if (c == '[')
                return false;
            if (c == ']')
                return false;
            if (c == '#')
                return false;

            return !char.IsWhiteSpace(c);
        }

        private string ReadWhiteSpace()
        {
            bool? wasPreviousWhiteSpaceNewLine = null;
            for (int i = nextChar; i != _input.Length; i++)
            {
                bool isNewLine = IsNewLineCharacter(_input[i]);

                if (!char.IsWhiteSpace(_input[i]) || (wasPreviousWhiteSpaceNewLine.HasValue && isNewLine != wasPreviousWhiteSpaceNewLine.Value))
                {
                    var result = _input.Substring(nextChar, i - nextChar);
                    nextChar = i;
                    return result;
                }
                wasPreviousWhiteSpaceNewLine = isNewLine;
            }
            var result2 = _input.Substring(nextChar);
            nextChar = _input.Length;
            return result2;
        }

        private void ReadUntilEndOfLine()
        {
            bool inNewLineSequence = false;
            for (int i = nextChar; i != _input.Length; i++)
            {
                var c = _input[i];
                if (IsNewLineCharacter(c))
                    inNewLineSequence = true;
                else
                {
                    if (inNewLineSequence)
                    {
                        nextChar = i;
                        return;
                    }
                }
            }
            nextChar = _input.Length;
        }

        private static readonly char[] s_NewLineCharacters = {'\n', (char)0x0d, (char)0x0a};

        public static bool IsNewLineCharacter(char c)
        {
            return s_NewLineCharacters.Contains(c);
        }
    }

    public class ScanToken
    {
        public TokenType tokenType;
        public string literal;

        public ScanToken Is(TokenType tokenType)
        {
            if (this.tokenType != tokenType)
                throw new ParsingException();
            return this;
        }
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
        Return,
        AppendOperator,
        Comment,
        Actions,
        On,
        EOF,
        Not,
        Else
    }
}
