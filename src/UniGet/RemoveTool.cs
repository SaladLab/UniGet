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
            var assetsDir = Path.Combine(options.ProjectDir, "Assets");
            if (Directory.Exists(assetsDir) == false)
            {
                Console.WriteLine($"Cannot find {assetsDir}.");
                return 1;
            }

            var packageBaseDir = Path.Combine(assetsDir, "UnityPackages");
            if (Directory.Exists(packageBaseDir) == false)
            {
                // UnityPackages is empty
                return 0;
            }

            foreach (var packageFile in Directory.GetFiles(packageBaseDir, "*.unitypackage.json"))
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

            RemoveEmptyFolder(packageBaseDir);

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
                var metaFile = Path.GetFullPath(Path.Combine(path, "..", Path.GetFileName(path))) + ".meta";
                Directory.Delete(path);
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
