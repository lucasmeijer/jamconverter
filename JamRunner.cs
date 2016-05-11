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
        public string[] Run(SourceFileDescription[] program)
        {
            var tempDir = NPath.CreateTempDirectory("jam");

	        foreach (var file in program)
	        {
		        var jamFile = tempDir.Combine(file.FileName);
		        jamFile.WriteAllText("NotFile all ; " + file.Contents);
	        }
	        var jamPath = Environment.OSVersion.Platform == PlatformID.Win32NT ? "external/jamplus/win32/jam.exe" : "external/jamplus/macosx64/jam";
            var jamBinary = ConverterRoot.Combine(jamPath);

            var execute = Shell.Execute(jamBinary, "-f " + program[0].FileName + " -C " + tempDir);

            var lines = execute.Split(new[] {Environment.NewLine}, StringSplitOptions.None);
            var relevance = RelevantLinesFrom(lines);

            return relevance.ToArray();
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