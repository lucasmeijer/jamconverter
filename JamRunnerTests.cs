using System;
using System.Collections.Generic;
using NiceIO;
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
            var output = jamRunner.Run(new SourceFileDescription() { Contents = program, File = new NPath("Jamfile.jam")});
            CollectionAssert.AreEqual(new[] {"harryharry harrysally sallyharry sallysally "}, output);
        }    
    }
}
