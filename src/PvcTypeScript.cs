using PvcCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PvcPlugins
{
    public class PvcTypeScript : PvcPlugin
    {
        private readonly string compilerOptions;

        public PvcTypeScript(string compilerOptions)
        {
            this.compilerOptions = compilerOptions;
        }

        public override string[] SupportedTags
        {
            get
            {
                return new[] { ".ts" };
            }
        }

        public override IEnumerable<PvcStream> Execute(IEnumerable<PvcStream> inputStreams)
        {
            var tsFiles = inputStreams.Select(i => i.StreamName).ToList();
            var tsFilesString = string.Join(" ", tsFiles.Select(f => "\"" + f + "\""));

            // Should we embed the compiler instead?
            var compilerPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"Microsoft SDKs\TypeScript\1.4\tsc.exe");
			Console.WriteLine("Using type script compiler: {0}", compilerPath);

			var p = Path.GetTempFileName();
	        try
	        {
		        File.WriteAllText(p, string.Format("{0} {1}", this.compilerOptions, tsFilesString));

		        var result = StreamProcessExecution(compilerPath, Environment.CurrentDirectory, "@" + p);

		        string tsOutputLine;
		        var outStreamReader = new StreamReader(result.StandardOutput.BaseStream);
		        while ((tsOutputLine = outStreamReader.ReadLine()) != null)
		        {
			        Console.WriteLine(tsOutputLine);
		        }

		        var errorStreamReader = new StreamReader(result.StandardError.BaseStream);
		        while ((tsOutputLine = errorStreamReader.ReadLine()) != null)
		        {
			        Console.WriteLine(tsOutputLine);
		        }

		        result.WaitForExit();
		        if (result.ExitCode != 0)
		        {
			        throw new Exception("Type script exited with non-zero status code: " + result.ExitCode);
		        }

		        // TODO: Implement advanced compiler options that affect JS output.
		        //
		        // In the compiler options flag, if the user passes in either of the following options:
		        //      --out (combine JS output into single file option)
		        //      - "redirect JS output to directory" option
		        //      - "generate declaration files" option
		        //      - "generate source maps" options
		        // 
		        // Then we'll need to pick up the corresponding JS and .js.map files.
		        return tsFiles
			        .Select(f => f.Replace(".ts", ".js"))
			        .Where(f => File.Exists(f))
			        .Select(f => PvcUtil.PathToStream(f));
	        }
	        finally
	        {
				File.Delete(p);
			}
         
        }

		public static Process StreamProcessExecution(string processPath, string workingDirectory, params string[] args)
		{
			var startInfo = new ProcessStartInfo(processPath, string.Join(" ", args));
			startInfo.UseShellExecute = false;
			startInfo.CreateNoWindow = true;
			startInfo.ErrorDialog = false;
			startInfo.WorkingDirectory = workingDirectory;
			startInfo.RedirectStandardError = true;
			startInfo.RedirectStandardOutput = true;

			var process = Process.Start(startInfo);
			return process;
		}
	}
}
