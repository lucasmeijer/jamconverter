using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

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
			var csharp = new JamToCSharpConverter().Convert(inputContents);
			File.WriteAllText (Path.GetFileNameWithoutExtension (inputPath) + ".cs", csharp);

        }
    }
}
