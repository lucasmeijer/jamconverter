using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Runtime.CompilerServices;
using jamconverter.Tests;
using NiceIO;
using Unity.IL2CPP;

namespace jamconverter
{
    internal class JamRunner
    {
        public string[] Run(SourceFileDescription file)
        {
            return Run(new ProgramDescripton {file});
        }

        public string[] Run(ProgramDescripton program)
        {
            var tempDir = NPath.CreateTempDirectory("jam");

            InjectNotFileAll(program);
	        foreach (var subfile in program)
	        {
		        var file = tempDir.Combine(subfile.FileName);
                file.WriteAllText(subfile.Contents);
	        }

	        var jamPath = Environment.OSVersion.Platform == PlatformID.Win32NT ? "external/jamplus/bin/ntx86/jam.debug.exe" : "external/jamplus/macosx64/jam";
            var jamBinary = ConverterRoot.Combine(jamPath);

            string startupArg = "";
            if (program.Any(f => f.FileName.EndsWith(".cs")))
            {
                var csharpExe = tempDir.Combine("csharp.exe");
                CSharpRunner.Compile(new ProgramDescripton(program.Where(f => f.FileName.EndsWith(".cs"))), new NPath[] {}, csharpExe);

                startupArg += "-m " + csharpExe.InQuotes();
            }

            var firstJamFile = program.FirstOrDefault(f => f.FileName.EndsWith(".jam"));
            if (firstJamFile != null)
            {
                startupArg += " -f " + firstJamFile.FileName;
            }

            startupArg += " -C " + tempDir;
            var execute = Shell.Execute(jamBinary, startupArg);

            var lines = execute.Split(new[] {Environment.NewLine}, StringSplitOptions.None);
            var relevance = RelevantLinesFrom(lines);

            return relevance.ToArray();
        }

        void InjectNotFileAll(List<SourceFileDescription> program)
        {
            var firstJamFile = program.FirstOrDefault(s => s.FileName.EndsWith(".jam"));
            if (firstJamFile != null)
                firstJamFile.Contents = "NotFile all ; \n" + firstJamFile.Contents;
            else
            {
                program.Add(new SourceFileDescription() {FileName = "Jamfile.jam", Contents = "NotFile all ;"});
            }
        }

        private IEnumerable<string> RelevantLinesFrom(IEnumerable<string> lines)
        {
            foreach (var line in lines)
            {
                if (line.StartsWith("*** found "))
                    yield break;
                yield return line;
            }
        }

        private static string GetConverterRoot([CallerFilePath] string sourceFilePath = "")
        {
            return Path.GetDirectoryName (sourceFilePath);
        }

        public static NPath ConverterRoot => new NPath(GetConverterRoot());
    }
}