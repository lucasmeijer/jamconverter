using System;
using System.Collections.Generic;
using System.Linq;
using NiceIO;
using Unity.IL2CPP;

namespace jamconverter
{
    internal class JamRunner
    {
        public string[] Run(string program)
        {
            var tempDir = NPath.CreateTempDirectory("jam");
            var jamFile = tempDir.Combine("Jambase.jam");
            jamFile.WriteAllText("NotFile all ; " + program);

            var jamBinary = ConverterRoot.Combine("external/jamplus/win32/jam.exe");

            var execute = Shell.Execute(jamBinary, "-f " + jamFile + " -C " + jamFile.Parent);

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

        public NPath ConverterRoot => new NPath("c:/jamconverter");
    }
}