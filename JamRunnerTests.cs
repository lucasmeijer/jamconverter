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
local a = harry sally ;
Echo $(a)$(a) ;
";

            var jamRunner = new JamRunner();
            var output = jamRunner.Run(program);
            CollectionAssert.AreEqual(new[] {"harryharry harrysally sallyharry sallysally "}, output);
        }

        [Test]
        [Ignore("PlayGround")]
        public void PlayGround()
        {
            var program =
@"

hallo = vier ;
a = hallo ;
Echo joh $($(a) bb) ;

if ! $(pieter) { Echo yes ; } else { Echo no ; }
";

            var jamRunner = new JamRunner();
            var output = jamRunner.Run(program);
            CollectionAssert.AreEqual(new[] { "harryharry harrysally sallyharry sallysally " }, output);
        }
    }
}
