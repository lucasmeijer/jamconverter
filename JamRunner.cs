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
	        return Run(new JamRunnerInstructions() {JamfilesToCreate = new List<SourceFileDescription>() {file}});
        }
		
		public string[] Run(JamRunnerInstructions instructions)
		{
			var tempDir = NPath.CreateTempDirectory("jamrunner");
			instructions.WorkingDir = instructions.WorkingDir ?? tempDir;

			foreach (var f in instructions.JamfilesToCreate)
				instructions.WorkingDir.Combine(f.File).WriteAllText(f.Contents);

			string startupArg = "";
			if (instructions.CSharpFiles.Any())
			{
				var csharpExe = tempDir.Combine("csharp.exe");
				CSharpRunner.Compile(instructions.CSharpFiles, JamToCSharpConverter.RuntimeDependencies, csharpExe);
				startupArg += "-m " + csharpExe.InQuotes();
			}

			var jamPath = Environment.OSVersion.Platform == PlatformID.Win32NT ? "external/jamplus/bin/win32/jam.exe" : "external/jamplus/macosx64/jam";
			var jamBinary = ConverterRoot.Combine(jamPath);

			startupArg += " -f " + (instructions.JamFileToInvokeOnStartup ?? instructions.JamfilesToCreate[0].File.FileName);

			startupArg += " -C " + instructions.WorkingDir;

			startupArg += " " + instructions.AdditionalArg;
			Console.WriteLine("args: " + startupArg);
            
			var execute = Shell.Execute(jamBinary, startupArg);

			var lines = execute.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
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

	class JamRunnerInstructions
	{
		public List<SourceFileDescription> CSharpFiles = new List<SourceFileDescription>();
		public List<SourceFileDescription> JamfilesToCreate = new List<SourceFileDescription>();
		public NPath WorkingDir;
		public string JamFileToInvokeOnStartup;
		public string AdditionalArg = "";
	}
}