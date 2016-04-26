using System.Linq;
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

            var scanResult = a.ScanToken();
            Assert.AreEqual(TokenType.Literal, scanResult.tokenType);
            Assert.AreEqual("hello", scanResult.literal);

            var scanResult2 = a.ScanToken();
            Assert.AreEqual(TokenType.WhiteSpace, scanResult2.tokenType);
        }

        [Test]
        public void Invocation()
        {
            var a = new Scanner("[ MyRule arg1 : arg2 ] ;");

            var expected = new[]
            {
                new ScanToken() {tokenType = TokenType.BracketOpen, literal = "["},
                new ScanToken() {tokenType = TokenType.WhiteSpace, literal = " "},
                new ScanToken() {tokenType = TokenType.Literal, literal = "MyRule"},
                new ScanToken() {tokenType = TokenType.WhiteSpace, literal = " "},
                new ScanToken() {tokenType = TokenType.Literal, literal = "arg1"},
                new ScanToken() {tokenType = TokenType.WhiteSpace, literal = " "},
                new ScanToken() {tokenType = TokenType.Colon, literal = ":"},
                new ScanToken() {tokenType = TokenType.WhiteSpace, literal = " "},
                new ScanToken() {tokenType = TokenType.Literal, literal = "arg2"},
                new ScanToken() {tokenType = TokenType.WhiteSpace, literal = " "},
                new ScanToken() {tokenType = TokenType.BracketClose, literal = "]"},
                new ScanToken() {tokenType = TokenType.WhiteSpace, literal = " "},
                new ScanToken() {tokenType = TokenType.Terminator, literal = ";"},
                new ScanToken() {tokenType = TokenType.EOF, literal = ""},
            };

            var result = a.ScanAllTokens().ToArray();
            
            //regular CollectionAssert.AreEqual only works with IComparer<T>
            CollectionAssert.AreEqual(expected.Select(sr => sr.tokenType).ToArray(), result.Select(sr=>sr.tokenType).ToArray());
            CollectionAssert.AreEqual(expected.Select(sr => sr.literal), result.Select(sr => sr.literal));
        }

        [Test]
        public void DereferencingVariable()
        {
            var a = new Scanner("$(myvar)");

            var scanResult1 = a.ScanToken();
            Assert.AreEqual(TokenType.VariableDereferencer, scanResult1.tokenType);

            var scanResult2 = a.ScanToken();
            Assert.AreEqual(TokenType.ParenthesisOpen, scanResult2.tokenType);

            var scanResult3 = a.ScanToken();
            Assert.AreEqual(TokenType.Literal, scanResult3.tokenType);
            Assert.AreEqual("myvar", scanResult3.literal);

            var scanResult4 = a.ScanToken();
            Assert.AreEqual(TokenType.ParenthesisClose, scanResult4.tokenType);
        }

        [Test]
        public void TwoAccolades()
        {
            var a = new Scanner("{}");
            var result = a.ScanAllTokens().ToArray();
            Assert.AreEqual(3, result.Length);
            Assert.AreEqual(TokenType.AccoladeOpen, result[0].tokenType);
            Assert.AreEqual(TokenType.AccoladeClose, result[1].tokenType);
            Assert.AreEqual(TokenType.EOF, result[2].tokenType);
        }

        [Test]
        public void TwoAccoladesWithLiteralInside()
        {
            var a = new Scanner("{ harry }");
            var result = a.ScanAllTokens().ToArray();

            CollectionAssert.AreEqual(new[] { TokenType.AccoladeOpen, TokenType.WhiteSpace, TokenType.Literal, TokenType.WhiteSpace, TokenType.AccoladeClose, TokenType.EOF}, result.Select(r => r.tokenType));
        }


        [Test]
        public void LetterFollowedByDollar()
        {
            var a = new Scanner("a$");
            var result = a.ScanAllTokens().ToArray();

            CollectionAssert.AreEqual(new[] { TokenType.Literal, TokenType.VariableDereferencer, TokenType.EOF }, result.Select(r => r.tokenType));
        }


        [Test]
        public void VariableExpansionModifier()
        {
            var a = new Scanner("$(harry:BS");
            var result = a.ScanAllTokens().ToArray();

            CollectionAssert.AreEqual(new[] { TokenType.VariableDereferencer, TokenType.ParenthesisOpen, TokenType.Literal, TokenType.Colon, TokenType.VariableExpansionModifier, TokenType.VariableExpansionModifier, TokenType.EOF }, result.Select(r => r.tokenType));

            Assert.AreEqual("B", result[4].literal);
            Assert.AreEqual("S", result[5].literal);
        }

        [Test]
        public void VariableExpansionModifierWithValue()
        {
            var a = new Scanner("$(harry:BS=v");
            var result = a.ScanAllTokens().ToArray();

            CollectionAssert.AreEqual(new[] { TokenType.VariableDereferencer, TokenType.ParenthesisOpen, TokenType.Literal, TokenType.Colon, TokenType.VariableExpansionModifier, TokenType.VariableExpansionModifier, TokenType.Assignment, TokenType.Literal, TokenType.EOF }, result.Select(r => r.tokenType));
            
            Assert.AreEqual("v", result[7].literal);
        }

        [Test]
        public void DontCombineNewLineAndWhiteSpaceInSingleToken()
        {
            
            var a = new Scanner(
/* note that in addition to the newline, there is whitepsace after hello, and before there*/
@"hello  
  there");
            var result = a.ScanAllTokens().ToArray();

            CollectionAssert.AreEqual(new[] { TokenType.Literal, TokenType.WhiteSpace, TokenType.WhiteSpace, TokenType.WhiteSpace, TokenType.Literal, TokenType.EOF }, result.Select(r => r.tokenType));
        }

        [Test]
        public void Comment()
        {
            var a = new Scanner(
@"hello #this means hello
on_new_line");
            var result = a.ScanAllTokens().ToArray();

            //we do not expect the comment to be reported by the scanner
            CollectionAssert.AreEqual(new[] { TokenType.Literal, TokenType.WhiteSpace, TokenType.Literal, TokenType.EOF}, result.Select(r => r.tokenType));
        }
    }
}