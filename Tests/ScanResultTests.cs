using System;
using System.Linq;
using NUnit.Framework;

namespace jamconverter.Tests
{
    [TestFixture]
    public class ScanResultTests
    {
        [Test]
        public void ConstructionFailsIfLastTokenIsNotEOF()
        {
            Assert.Throws<ArgumentException>(() => new ScanResult(new[] {new ScanToken() {literal = "hallo", tokenType = TokenType.Literal}}));
        }

        [Test]
        public void Next()
        {
            var scanTokens = HelloThereSailorTokens();
            var scanResult = new ScanResult(scanTokens);

            Assert.AreEqual(scanTokens[0], scanResult.Next());
        }

        [Test]
        public void NextThrowAfterLastToken()
        {
            var scanResult = new ScanResult(new ScanToken[] { new ScanToken() { tokenType = TokenType.EOF } });
            scanResult.Next();

            Assert.Throws<InvalidOperationException>(()=>
            {
                scanResult.Next();
            });
        }

        [Test]
        public void PeekThrowAfterLastToken()
        {
            var scanResult = new ScanResult(new ScanToken[] { new ScanToken() { tokenType = TokenType.EOF } });
            scanResult.Next();

            Assert.Throws<InvalidOperationException>(() => scanResult.Peek());
        }

        [Test]
        public void NextProgressesAndSkipsWhiteSpace()
        {
            var scanTokens = HelloThereSailorTokens();
            var scanResult = new ScanResult(scanTokens);

            Assert.AreEqual(scanTokens[0], scanResult.Next());
            Assert.AreEqual(scanTokens[2], scanResult.Next());
        }

        [Test]
        public void PeekDoesNotProgress()
        {
            var scanTokens = HelloThereSailorTokens();
            var scanResult = new ScanResult(scanTokens);

            Assert.AreEqual(scanTokens[0], scanResult.Peek());
            Assert.AreEqual(scanTokens[0], scanResult.Next());
        }

        [Test]
        public void PeekSkipsWhiteSpace()
        {
            var scanTokens = HelloThereSailorTokens();
            var scanResult = new ScanResult(scanTokens);

            scanResult.Next();

            Assert.AreEqual(scanTokens[2], scanResult.Peek());
        }

        [Test]
        public void CanConsumeAndRewind()
        {
            var scanTokens = HelloThereSailorTokens();
            var scanResult = new ScanResult(scanTokens);

            Assert.AreEqual(scanTokens[0], scanResult.Next());

            var cursor = scanResult.GetCursor();

            Assert.AreEqual(scanTokens[2], scanResult.Next());
            Assert.AreEqual(scanTokens[4], scanResult.Next());

            scanResult.SetCursor(cursor);
            Assert.AreEqual(scanTokens[2], scanResult.Next());
            Assert.AreEqual(scanTokens[4], scanResult.Next());
        }

        [Test]
        public void CanReadUntilEndOfLine()
        {
            var scanTokens = new Scanner(
@"hello there sailor
what a nice day
    line with some whitespace in front").ScanAllTokens();

            var scanResult = new ScanResult(scanTokens.ToArray());
            scanResult.Next();

            Assert.AreEqual(" there sailor", scanResult.ProduceStringUntilEndOfLine());
            Assert.AreEqual("what a nice day", scanResult.ProduceStringUntilEndOfLine());
            Assert.AreEqual("    line with some whitespace in front", scanResult.ProduceStringUntilEndOfLine());

            Assert.AreEqual(TokenType.EOF, scanResult.Next().tokenType);
        }

        private static ScanToken[] HelloThereSailorTokens()
        {
            var scanTokens = new[]
            {
                new ScanToken {literal = "hello", tokenType = TokenType.Literal},
                new ScanToken {literal = " ", tokenType = TokenType.WhiteSpace},
                new ScanToken {literal = "there", tokenType = TokenType.Literal},
                new ScanToken {literal = " ", tokenType = TokenType.WhiteSpace},
                new ScanToken {literal = "sailor", tokenType = TokenType.Literal},
                new ScanToken {literal = "", tokenType = TokenType.EOF},
            };
            return scanTokens;
        }
    }
}
