using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using jamconverter.Tests;

namespace jamconverter
{
    class Program
    {
        static void Main(string[] args)
        {
			if (args.Count() != 1)
				throw new System.ArgumentException ();
			var inputPath = args [0];
			var inputContents = File.ReadAllText (inputPath);
            var input = new ProgramDescripton { new SourceFileDescription {FileName = inputPath, Contents = inputContents}};
			var csharp = new JamToCSharpConverter().Convert(input);
			foreach (var convertedFile in csharp)
				File.WriteAllText (convertedFile.FileName, convertedFile.Contents);
        }
    }
}
