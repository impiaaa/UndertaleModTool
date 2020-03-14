using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using UndertaleModLib;
using UndertaleModLib.Scripting;

namespace Test
{
    class Program
    {
        class ScriptContext : IScriptInterface
        {
            protected readonly string path;
            protected UndertaleData data;
            protected bool progressBarVisible;
            protected int progressPosition;

            public ScriptContext(string dataPath, UndertaleData data)
            {
                this.path = dataPath;
                this.data = data;
                progressBarVisible = false;
            }

            public UndertaleData Data => data;
            public string FilePath => path;
            public object Highlighted => throw new NotImplementedException();
            public object Selected => throw new NotImplementedException();
            public bool CanSave => throw new NotImplementedException();

            public void ChangeSelection(object newsel)
            {
            }

            public void EnsureDataLoaded()
            {
                if (Data == null)
                    throw new Exception("Please load data.win first!");
            }

            public void ScriptMessage(string message)
            {
                Console.WriteLine(message);
            }

            public bool ScriptQuestion(string message)
            {
                Console.WriteLine(message);
                return Console.ReadLine().StartsWith("y", StringComparison.CurrentCultureIgnoreCase);
            }

            public void ScriptOpenURL(string url)
            {
                Process.Start(url);
            }

            public void UpdateProgressBar(string message, string status, double progressValue, double maxValue)
            {
                string statusMessage = message + ": " + status + " (" + progressValue + "/" + maxValue + ")";
                if (progressBarVisible)
                {
                    Console.SetCursorPosition(0, progressPosition);
                }
                else
                {
                    progressBarVisible = true;
                    progressPosition = Console.CursorTop;
                }
                Console.Write(statusMessage);
            }

            public void HideProgressBar()
            {
                progressBarVisible = false;
                Console.WriteLine();
            }

            public void ScriptError(string error, string title)
            {
                Console.Error.WriteLine(title + ": " + error);
            }

            public string PromptChooseDirectory(string prompt)
            {
                throw new NotImplementedException();
            }

            public string PromptLoadFile(string defaultExt, string filter)
            {
                throw new NotImplementedException();
            }
        }

        static void Main(string[] args)
        {
            string dataPath = args[0];
            string scriptPath = args[1];

            Console.WriteLine("Loading script.");
            Script script;
            using (var loader = new InteractiveAssemblyLoader())
            {
                loader.RegisterDependency(typeof(UndertaleObject).GetTypeInfo().Assembly);

                script = CSharpScript.Create<object>(new FileStream(scriptPath, FileMode.Open, FileAccess.Read),
                    ScriptOptions.Default
                    .AddImports("UndertaleModLib", "UndertaleModLib.Models", "UndertaleModLib.Decompiler", "UndertaleModLib.Scripting", "UndertaleModLib.Compiler")
                    .AddImports("System", "System.IO", "System.Collections.Generic", "System.Text.RegularExpressions")
                    .AddReferences(typeof(UndertaleObject).GetTypeInfo().Assembly)
                    .AddReferences(typeof(System.Text.RegularExpressions.Regex).GetTypeInfo().Assembly)
                    .WithFilePath(scriptPath)
                    .WithEmitDebugInformation(true),
                    typeof(IScriptInterface),
                    loader);
            }

            Console.Write("Loading data...");
            Console.Out.Flush();
            UndertaleData data = UndertaleIO.Read(new FileStream(dataPath, FileMode.Open, FileAccess.Read));
            Console.WriteLine(" done.");

            Console.WriteLine("Running script.");
            var result = script.RunAsync(new ScriptContext(dataPath, data)).GetAwaiter().GetResult();
            Console.WriteLine("Done.");

            if (result != null && args.Length > 2)
            {
                Console.Write("Writing data...");
                Console.Out.Flush();
                UndertaleIO.Write(new FileStream(args[2], FileMode.Create), data);
                Console.WriteLine(" done.");
            }
        }
    }
}
