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
		public void A()
		{
			var converter = new JamToCSharpConverter();
			var inputFile = "c:/unity/External/Jamplus/builds/bin/Jambase.jam";
			var program = new[] {new SourceFileDescription() { Contents = new NPath(inputFile).ReadAllText(), FileName = "Main.cs"} };
			var output = converter.Convert(program);
		
			new CSharpRunner().Run(output, new[] { new NPath("c:/jamconverter/bin/runtimelib.dll") }).Select(s => s.TrimEnd());

			foreach (var sourceFile in output)
			{
				var file = NPath.SystemTemp.Combine(sourceFile.FileName);
				file.WriteAllText(sourceFile.Contents);
				Console.WriteLine(file);
			}
		}
	}
}
