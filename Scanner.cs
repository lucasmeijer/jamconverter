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

            var result = a.ScanAll();
            
            //regular CollectionAssert.AreEqual only works with IComparer<T>
            CollectionAssert.AreEqual(expected.Select(sr => sr.tokenType), result.Select(sr=>sr.tokenType));
            CollectionAssert.AreEqual(expected.Select(sr => sr.literal), result.Select(sr => sr.literal));
        }
    }

    public class Scanner
    {
        private readonly string _input;
        private int nextChar = 0;

        public Scanner(string input)
        {
            _input = input;
        }

        public ScanResult Scan()
        {
            if (nextChar >= _input.Length)
                return null;
            
            var c = _input[nextChar];
            
            if (char.IsWhiteSpace(c))
                return new ScanResult() { tokenType = TokenType.WhiteSpace, literal = ReadWhiteSpace() };

            var literal = ReadLiteral();

            if (literal == ";")
                return new ScanResult() {tokenType = TokenType.Terminator, literal = literal};

            if (literal == "[")
                return new ScanResult() { tokenType = TokenType.BracketOpen, literal = literal };

            if (literal == "]")
                return new ScanResult() { tokenType = TokenType.BracketClose, literal = literal };

            if (literal == ":")
                return new ScanResult() { tokenType = TokenType.ArgumentSeperator, literal = literal };

            return new ScanResult() {tokenType = TokenType.Literal, literal = literal};
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
                if (!char.IsWhiteSpace(_input[i]))
                    continue;
                break;
            }
            var result = _input.Substring(nextChar, i - nextChar);
            nextChar = i;
            return result;
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
            throw new Exception();
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
        BracketOpen
    }
}
