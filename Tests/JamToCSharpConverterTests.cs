using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using jamconverter.AST;
using NUnit.Framework;

namespace jamconverter.Tests
{
    [TestFixture]
    public class JamToCSharpConverterTests
    {
        [Test]
        public void ExpressionListWithLiteral()
        {
            Assert.AreEqual("new JamList(\"hallo\")", ConvertExpressionListWith(MakeLiteral("hallo")));
        }

        [Test]
        public void ExpressionListWithTwoLiterals()
        {
            Assert.AreEqual("new JamList(\"hallo\",\"there\")", ConvertExpressionListWith(MakeLiteral("hallo"), MakeLiteral("there")));
        }

        [Test]
        public void ExpressionListWithVariableDereference()
        {
            Assert.AreEqual("myvar", ConvertExpressionListWith(MakeVDE("myvar")));
        }

        [Test]
        public void ExpressionListWitTwoVariableDereferences()
        {
            Assert.AreEqual("myvar.With(myvar2)", ConvertExpressionListWith(MakeVDE("myvar"), MakeVDE("myvar2")));
        }

        [Test]
        public void EmptyExpressionList()
        {
            Assert.AreEqual("new JamList()", ConvertExpressionListWith());
        }

        [Test]
        public void InvocationAndLiteral()
        {
            Assert.AreEqual("MyFunc().With(\"hallo\")", ConvertExpressionListWith(new InvocationExpression() { RuleExpression = MakeLiteral("MyFunc")}, MakeLiteral("hallo")));
        }

        private static string ConvertExpressionListWith(params Expression[] expressions)
        {
            return new JamToCSharpConverter().CSharpFor(new ExpressionList {Expressions = expressions});
        }

        private static VariableDereferenceExpression MakeVDE(string variableName)
        {
            return new VariableDereferenceExpression() { VariableExpression = new LiteralExpression() { Value = variableName } };
        }

        private static LiteralExpression MakeLiteral(string literal)
        {
            return new LiteralExpression() { Value = literal };
        }
    }
}
