using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication.ExtendedProtection;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework.Internal;
using NUnit.Framework;

namespace jamconverter
{
    [TestFixture]
    public class ScannerTests
    {
        [Test]
        public void Simple()
        {
            var a = new Scanner("hello ;");

            var scanResult = a.Scan();
            Assert.AreEqual(TokenType.Literal, scanResult.tokenType);
            Assert.AreEqual("hello", scanResult.literal);

            var scanResult2 = a.Scan();
            Assert.AreEqual(TokenType.WhiteSpace, scanResult2.tokenType);
        }

        [Test]
        public void Invocation()
        {
            var a = new Scanner("[ MyRule arg1 : arg2 ] ;");

            var expected = new[]
            {
                new ScanResult() {tokenType = TokenType.BracketOpen, literal = "["},
                new ScanResult() {tokenType = TokenType.WhiteSpace, literal = " "},
                new ScanResult() {tokenType = TokenType.Literal, literal = "MyRule"},
                new ScanResult() {tokenType = TokenType.WhiteSpace, literal = " "},
                new ScanResult() {tokenType = TokenType.Literal, literal = "arg1"},
                new ScanResult() {tokenType = TokenType.WhiteSpace, literal = " "},
                new ScanResult() {tokenType = TokenType.ArgumentSeperator, literal = ":"},
                new ScanResult() {tokenType = TokenType.WhiteSpace, literal = " "},
                new ScanResult() {tokenType = TokenType.Literal, literal = "arg2"},
                new ScanResult() {tokenType = TokenType.WhiteSpace, literal = " "},
                new ScanResult() {tokenType = TokenType.BracketClose, literal = "]"},
                new ScanResult() {tokenType = TokenType.WhiteSpace, literal = " "},
                new ScanResult() {tokenType = TokenType.Terminator, literal = ";"},
            };

            var result = a.ScanAll().ToArray();
            
            //regular CollectionAssert.AreEqual only works with IComparer<T>
            CollectionAssert.AreEqual(expected.Select(sr => sr.tokenType).ToArray(), result.Select(sr=>sr.tokenType).ToArray());
            CollectionAssert.AreEqual(expected.Select(sr => sr.literal), result.Select(sr => sr.literal));
        }

        [Test]
        public void DereferencingVariable()
        {
            var a = new Scanner("$(myvar)");

            var scanResult1 = a.Scan();
            Assert.AreEqual(TokenType.VariableDereferencer, scanResult1.tokenType);

            var scanResult2 = a.Scan();
            Assert.AreEqual(TokenType.ParenthesisOpen, scanResult2.tokenType);

            var scanResult3 = a.Scan();
            Assert.AreEqual(TokenType.Literal, scanResult3.tokenType);
            Assert.AreEqual("myvar", scanResult3.literal);

            var scanResult4 = a.Scan();
            Assert.AreEqual(TokenType.ParenthesisClose, scanResult4.tokenType);
        }

        [Test]
        public void TwoAccolades()
        {
            var a = new Scanner("{}");
            var result = a.ScanAll().ToArray();
            Assert.AreEqual(2, result.Length);
            Assert.AreEqual(TokenType.AccoladeOpen, result[0].tokenType);
            Assert.AreEqual(TokenType.AccoladeClose, result[1].tokenType);
        }

        [Test]
        public void TwoAccoladesWithLiteralInside()
        {
            var a = new Scanner("{ harry }");
            var result = a.ScanAll().ToArray();
            Assert.AreEqual(5, result.Length);

            CollectionAssert.AreEqual(new[] { TokenType.AccoladeOpen, TokenType.WhiteSpace, TokenType.Literal, TokenType.WhiteSpace, TokenType.AccoladeClose}, result.Select(r => r.tokenType));
        }
    }

    public class Scanner
    {
        private readonly string _input;
        private int nextChar = 0;
        private readonly Queue<ScanResult> _unscanBuffer = new Queue<ScanResult>();

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
            
            if (char.IsWhiteSpace(c))
                return new ScanResult() { tokenType = TokenType.WhiteSpace, literal = ReadWhiteSpace() };

            var literal = ReadLiteral();

            return new ScanResult() {tokenType = TokenTypeFor(literal), literal = literal};
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
                return TokenType.ArgumentSeperator;

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
        ArgumentSeperator,
        BracketOpen,
        VariableDereferencer,
        ParenthesisClose,
        ParenthesisOpen,
        Assignment,
        AccoladeOpen,
        AccoladeClose,
        If
    }
}
