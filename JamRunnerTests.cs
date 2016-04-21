using NUnit.Framework;

namespace jamconverter
{
    [TestFixture]
    class JamRunnerTests
    {
        [Test]
        public void CanRunJamProgram()
        {
            var program =
@"
NotFile all ; 
local a = harry sally ;
Echo $(a)$(a) ;
";

            var jamRunner = new JamRunner();
            var output = jamRunner.Run(program);
            CollectionAssert.AreEqual(new[] { "harryharry harrysally sallyharry sallysally" }, output);
        }
    }
}
