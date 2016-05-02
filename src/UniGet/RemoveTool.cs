using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using Newtonsoft.Json.Linq;

namespace UniGet
{
    internal static class RemoveTool
    {
        internal class Options
        {
            [Value(0, Required = true, HelpText = "Project Directory")]
            public string ProjectDir { get; set; }
        }

        public static int Run(params string[] args)
        {
            var parser = new Parser(config => config.HelpWriter = Console.Out);
            if (args.Length == 0)
            {
                parser.ParseArguments<Options>(new[] { "--help" });
                return 1;
            }

            Options options = null;
            var result = parser.ParseArguments<Options>(args)
                               .WithParsed(r => { options = r; });

            // Run process !

            if (options != null)
                return Process(options);
            else
                return 1;
        }

        internal static int Process(Options options)
        {
            var baseDir = Path.Combine(options.ProjectDir, "Assets", "UnityPackages");
            if (Directory.Exists(baseDir) == false)
            {
                Console.WriteLine($"Cannot find {baseDir}.");
                return 1;
            }

            foreach (var packageFile in Directory.GetFiles(baseDir, "*.unitypackage.json"))
            {
                if (File.Exists(packageFile) == false)
                    continue;

                Project p = null;
                try
                {
                    p = Project.Load(packageFile);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Ignore {packageFile} due to a parsing error: {e}");
                    continue;
                }

                if (p.Files != null)
                {
                    foreach (var fileValue in p.Files)
                    {
                        var target = (fileValue is JObject)
                            ? fileValue.ToObject<Project.FileItem>().Target
                            : fileValue.ToString();

                        var filePath = Path.Combine(options.ProjectDir, target);
                        if (File.Exists(filePath))
                            File.Delete(filePath);
                        if (File.Exists(filePath + ".meta"))
                            File.Delete(filePath + ".meta");
                    }
                }

                File.Delete(packageFile);
                if (File.Exists(packageFile + ".meta"))
                    File.Delete(packageFile + ".meta");
            }

            RemoveEmptyFolder(baseDir);

            return 0;
        }

        internal static bool RemoveEmptyFolder(string path)
        {
            var ok = true;
            foreach (var dir in Directory.GetDirectories(path))
            {
                if (RemoveEmptyFolder(dir) == false)
                    ok = false;
            }

            if (ok && Directory.GetFiles(path).Any() == false)
            {
                Directory.Delete(path);
                var metaFile = Path.Combine(path, "..", Path.GetFileName(path)) + ".meta";
                if (File.Exists(metaFile))
                    File.Delete(metaFile);
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
