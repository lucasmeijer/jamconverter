using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using NiceIO;
using Environment = System.Environment;
using StreamWriter = System.IO.StreamWriter;

namespace Unity.IL2CPP
{
	public static class Shell
	{
		private static readonly object FileLocker = new object();
		private static readonly object PipeLocker = new object();

		public class ExecuteArgs
		{
			public string Executable { get; set; }
			public string Arguments { get; set; }
			public Dictionary<string, string> EnvVars { get; set; }
			public string WorkingDirectory { get; set; }
		}

		public class ExecuteResult
		{
			public string StdOut { get; set; }
			public string StdErr { get; set; }
			public int ExitCode { get; set; }
			public TimeSpan Duration { get; set; }
		}

		public interface IExecuteController
		{
			void OnStdoutReceived(string data);
			void OnStderrReceived(string data);

			void AboutToCleanup(string tempOutputFile, string tempErrorFile);
		}

		public static string Execute(NiceIO.NPath filename, string arguments, Dictionary<string, string> envVars = null)
		{
			return Execute(filename.ToString(), arguments, envVars);
		}

		public static string Execute (string filename, string arguments, Dictionary<string, string> envVars = null)
		{
			ExecuteArgs executeArgs = new ExecuteArgs { Executable = filename, Arguments = arguments, EnvVars = envVars};
			var result = Execute(executeArgs);

			var allConsoleOutput = result.StdErr.Trim() + result.StdOut.Trim();

			if (0 != result.ExitCode) {
				throw new Exception (string.Format (
					"Process {0} ended with exitcode {1}" + Environment.NewLine +
					"{2}" + Environment.NewLine,
					executeArgs.Executable,
					result.ExitCode,
					allConsoleOutput));
			}

			return allConsoleOutput;
		}

		public static ExecuteResult Execute(ExecuteArgs executeArgs, IExecuteController controller = null, NPath outputFile = null)
		{
			using (var p = NewProcess(executeArgs))
			{
				FileStream fOut, fError;
				string tempOutputFile, tempErrorFile;

				lock (FileLocker)
				{
				    tempOutputFile = outputFile == null ? Path.GetTempFileName() : outputFile.ToString();
					tempErrorFile = Path.GetTempFileName();
					fOut = File.Create(tempOutputFile);
					fError = File.Create(tempErrorFile);
				}

				var stopWatch = new Stopwatch();
				stopWatch.Start();

				using (
					StreamWriter outputWriter = new StreamWriter(fOut, Encoding.UTF8),
						errorWriter = new StreamWriter(fError, Encoding.UTF8))
				{
					p.OutputDataReceived += (sender, args) =>
					{
						outputWriter.WriteLine(args.Data);
						if (controller != null)
							controller.OnStdoutReceived(args.Data);
					};
					p.ErrorDataReceived += (sender, args) =>
					{
						errorWriter.WriteLine(args.Data);
						if (controller != null)
							controller.OnStderrReceived(args.Data);
					};
					lock (PipeLocker)
					{
						p.Start();
						p.BeginOutputReadLine();
						p.BeginErrorReadLine();
					}

					p.WaitForExit();
					p.CancelErrorRead();
					p.CancelOutputRead();
				}

			    string output = "";
				string error;
			    lock (FileLocker)
			    {
			        if (controller != null)
			            controller.AboutToCleanup(tempOutputFile, tempErrorFile);
                    
			        if (outputFile == null)
			        {
			            output = File.ReadAllText(tempOutputFile, Encoding.UTF8);
                        File.Delete(tempOutputFile);
                    }

                    error = File.ReadAllText(tempErrorFile, Encoding.UTF8);
					File.Delete(tempErrorFile);
				}

				stopWatch.Stop();
				var result = new ExecuteResult()
				{
					ExitCode = p.ExitCode,
					StdOut = outputFile == null ? output : "in outputfile",
					StdErr = error,
					Duration = TimeSpan.FromMilliseconds(stopWatch.ElapsedMilliseconds)
				};

				return result;
			}
		}

		public static Process StartProcess(string filename, string arguments)
		{
			var p = NewProcess(new ExecuteArgs { Executable = filename, Arguments = arguments});

			p.Start();
			return p;
		}

		static Process NewProcess(ExecuteArgs executeArgs)
		{
			var p = new Process
			{
				StartInfo =
				{
					Arguments = executeArgs.Arguments,
					CreateNoWindow = true,
					UseShellExecute = false,
					RedirectStandardOutput = true,
					RedirectStandardInput = true,
					RedirectStandardError = true,
					FileName = executeArgs.Executable,
					StandardOutputEncoding = Encoding.UTF8,
					StandardErrorEncoding = Encoding.UTF8,
					WorkingDirectory = executeArgs.WorkingDirectory,
				}
			};

			if (executeArgs.EnvVars != null)
				foreach (var envVar in executeArgs.EnvVars)
					p.StartInfo.EnvironmentVariables.Add(envVar.Key, envVar.Value);

			return p;
		}
	}
}
