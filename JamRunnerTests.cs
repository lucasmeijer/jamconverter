using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace jamconverter.Tests
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
            var output = jamRunner.Run(new SourceFileDescription() { Contents = program, FileName = "Jamfile.jam"});
            CollectionAssert.AreEqual(new[] {"harryharry harrysally sallyharry sallysally "}, output);
        }    
    }
}
