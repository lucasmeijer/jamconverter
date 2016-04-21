using System;
using NiceIO;
using NUnit.Framework;
using Unity.IL2CPP;

namespace jamconverter
{
    class CSharpRunner
    {
        public string[] Run(string program)
        {
            var tmpDir = NPath.CreateTempDirectory("Csharp");

            var file = tmpDir.Combine("Main.cs").WriteAllText(program);

            var csc = new NPath("C:/il2cpp-dependencies/Roslyn/Binaries/csc.exe");

            var executable = tmpDir.Combine("program.exe");
            Shell.Execute(csc, file + " -out:" + executable);

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
