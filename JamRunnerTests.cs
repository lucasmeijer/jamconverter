using System;
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
local a = harry sally ;
Echo $(a)$(a) ;
";
            var jamRunner = new JamRunner();
            var output = jamRunner.Run(new [] { new SourceFileDescription() { Contents = program, FileName = "Jamfile.jam"}});
            CollectionAssert.AreEqual(new[] {"harryharry harrysally sallyharry sallysally "}, output);
        }

		[Test]
		public void CanRunMultifileProgram()
		{
			var jam1 =
@"
Echo jam1 ;
include file2.jam ;
Echo jam1 post ;
";
			var jam2 =
@"
Echo Jam2 ;
";

			var jamRunner = new JamRunner();
			var output = jamRunner.Run(new[]
			{
				new SourceFileDescription() { Contents = jam1, FileName = "Jamfile.jam" },
				new SourceFileDescription() { Contents = jam2, FileName = "file2.jam" }
			});
			CollectionAssert.AreEqual(new[] { "jam1 ","Jam2 ","jam1 post " }, output);
		}



		[Test]
    //    [Ignore("PlayGround")]
        public void PlayGround()
        {
            var program =
@"

hallo = vier vijf ;
b = ja dus ;

Echo $(hallo)john$(b) ;

";

            var jamRunner = new JamRunner();
			var output = jamRunner.Run(new[] { new SourceFileDescription() { Contents = program, FileName = "Jamfile.jam" } });
			foreach (var line in output)
                Console.WriteLine(line);

            
        }
    }
}
