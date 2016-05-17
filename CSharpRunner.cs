using System;
using System.Collections;
using System.Linq;
using NiceIO;
using NUnit.Framework;
using Unity.IL2CPP;
using System.Collections.Generic;

namespace jamconverter.Tests
{
    class CSharpRunner
    {
        public string[] Run(ProgramDescripton program, IEnumerable<NPath> additionalLibs = null)
        {
            var executable = Compile(program, additionalLibs);

            return Shell.Execute(executable, "").Split(new[] {Environment.NewLine}, StringSplitOptions.None);
        }

        public static NPath Compile(ProgramDescripton program, IEnumerable<NPath> additionalLibs, NPath outputFile = null)
        {
            var executable = outputFile ??  NPath.CreateTempDirectory("CSharp").Combine("program.exe");
            var tmpDir = executable.Parent;

            var absoluteCSFiles = new List<NPath>();
            foreach (var fileEntry in program)
            {
                var absolutePath = tmpDir.Combine(fileEntry.FileName);
                absoluteCSFiles.Add(absolutePath);
                var file = absolutePath.WriteAllText(fileEntry.Contents);
                Console.WriteLine(".cs: " + file);
            }

            var compiler = new NPath(@"C:\il2cpp-dependencies\MonoBleedingEdge\builds\monodistribution\bin\mcs" + (Environment.OSVersion.Platform == PlatformID.Win32NT ? ".bat" : ""));
            
            if (additionalLibs == null) additionalLibs = new NPath[0];

            Shell.Execute(compiler, absoluteCSFiles.InQuotes().SeperateWithSpace() + " " + additionalLibs.InQuotes().Select(l => "-r:" + l).SeperateWithSpace() + " -debug -langversion:6 -out:" + executable);

            foreach (var lib in additionalLibs)
                lib.Copy(tmpDir);
            return executable;
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

            var output = new CSharpRunner().Run(new ProgramDescripton() { new SourceFileDescription() { Contents = program, FileName = "Main.cs" }});
            CollectionAssert.AreEqual(new[] {"Hello!"}, output);
        }
    }
}
