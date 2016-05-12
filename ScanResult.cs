using System;
using System.Linq;
using System.Text;

namespace jamconverter
{
    public class ScanResult
    {
        private readonly ScanToken[] _scanTokens;
        public int _cursor = 0;

        public ScanResult(ScanToken[] scanTokens)
        {
            if (scanTokens.Length == 0 || scanTokens.Last().tokenType != TokenType.EOF)
                throw new ArgumentException();

            _scanTokens = scanTokens;
        }

		public bool IsAtEnd
		{
			get { return _cursor >= _scanTokens.Length; }
		}

        public ScanToken Next(bool skipWhiteSpace=true)
        {
            if (IsAtEnd)
                throw new InvalidOperationException("Reached end of stream while expecting more tokens");

            if (_scanTokens[_cursor].tokenType == TokenType.WhiteSpace && skipWhiteSpace)
            {
                _cursor++;
                return Next();
            }

            return _scanTokens[_cursor++];
        }

        public ScanToken Peek(bool skipWhiteSpace = true)
        {
            if (IsAtEnd)
                throw new InvalidOperationException();

            int tempCursor = _cursor;
            while (_scanTokens[tempCursor].tokenType == TokenType.WhiteSpace && skipWhiteSpace)
            {
                tempCursor++;
            }
            
            return _scanTokens[tempCursor];
        }

        public int GetCursor()
        {
            return _cursor;
        }

        public void SetCursor(int cursor)
        {
            _cursor = cursor;
        }

        public string ProduceStringUntilEndOfLine()
        {
            var sb = new StringBuilder();
            while (true)
            {
                var token = Peek(skipWhiteSpace:false);
                if (token.tokenType == TokenType.WhiteSpace && Scanner.IsNewLineCharacter(token.literal[0]))
                {
                    Next(skipWhiteSpace: false);
                    return sb.ToString();
                }
                if (token.tokenType == TokenType.EOF)
                    return sb.ToString();
                
                sb.Append(Next(skipWhiteSpace: false).literal);
            }
        }
    }
}
