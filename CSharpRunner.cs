using System;
using System.Linq;
using NiceIO;
using NUnit.Framework;
using Unity.IL2CPP;

namespace jamconverter.Tests
{
    class CSharpRunner
    {
        public string[] Run(string program, NPath[] additionalLibs = null)
        {
            var tmpDir = NPath.CreateTempDirectory("Csharp");

            var file = tmpDir.Combine("Main.cs").WriteAllText(program);
            Console.WriteLine(".cs: "+file);
			var csc = new NPath(Environment.OSVersion.Platform == PlatformID.Win32NT ? "C:/il2cpp-dependencies/Roslyn/Binaries/csc.exe" : "/usr/local/bin/mcs");

            var executable = tmpDir.Combine("program.exe");
            if (additionalLibs == null) additionalLibs = new NPath[0];
            Shell.Execute(csc, file + " "+additionalLibs.InQuotes().Select(l=>"-r:"+l).SeperateWithSpace()+ " -debug -out:" + executable);

            foreach (var lib in additionalLibs)
                lib.Copy(tmpDir);

            return Shell.Execute(executable, "").Split(new[] {Environment.NewLine}, StringSplitOptions.None);
        }
    }

    [TestFixture]
    class CSharpRunnerTests
    {
        [Test]
        public void CanRunSimpleProgram()
        {
            var program = @"
class Dummy {
    static void Main()
    {
      System.Console.WriteLine(""Hello!"");
    }
}
";

            var output = new CSharpRunner().Run(program);
            CollectionAssert.AreEqual(new[] {"Hello!"}, output);
        }
    }
}
