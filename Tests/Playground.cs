using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiceIO;
using NUnit.Framework;

namespace jamconverter.Tests
{
	[TestFixture]
	class Playground
	{
		[Test]
		//[Ignore("asd")]
		public void A()
		{
			var converter = new JamToCSharpConverter();
			var sourceFile = "c:/unity/External/Jamplus/builds/bin/Jambase.jam";
			var output = converter.Convert(new NPath(sourceFile).ReadAllText());
			var file = NiceIO.NPath.SystemTemp.Combine("PlayGround.cs");

			new CSharpRunner().Run(output, new[] { new NPath("c:/jamconverter/bin/runtimelib.dll") }).Select(s => s.TrimEnd());


			file.WriteAllText(output);
			Console.WriteLine(file);
		}
	}
}
