using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using CommandLine;
using Newtonsoft.Json.Linq;

namespace UniGet
{
    internal static class PackTool
    {
        internal class Options
        {
            [Value(0, Required = true, HelpText = "Project File")]
            public string ProjectFile { get; set; }

            [Option('o', "output", HelpText = "Specifies the directory for the created unity package file. If not specified, uses the current directory.")]
            public string OutputDirectory { get; set; }
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

            Console.WriteLine(options.ProjectFile);
            Console.WriteLine(options.OutputDirectory);

            // Run process !

            if (options != null)
                return Process(options);
            else
                return 1;
        }

        internal class FileItem
        {
            public string Source;
            public string Target;
            public bool MetaGenerated;
        }

        private static int Process(Options options)
        {
            var p = Project.Load(options.ProjectFile);
            var projectDir = Path.GetDirectoryName(options.ProjectFile);
            var outputDir = options.OutputDirectory ?? projectDir;

            if (string.IsNullOrEmpty(p.Id))
                throw new InvalidDataException("Cannot find id from project.");

            if (p.Files == null || p.Files.Any() == false)
                throw new InvalidDataException("Cannot find files from project.");

            var defaultTargetDir = "Assets/UnityPackages/" + p.Id;
            var tempDir = "";

            var files = new List<FileItem>();
            foreach (var fileValue in p.Files)
            {
                if (fileValue is JObject)
                {
                    // TODO: !
                }
                else if (fileValue.ToString().StartsWith("$"))
                {
                    var keyword = fileValue.ToString().ToLower();
                    if (keyword != "$dependencies$")
                    {
                        throw new InvalidDataException("Wrong keyword: " + keyword);
                    }

                    tempDir = Extracter.CreateTemporaryDirectory();
                    RestoreTool.Run(new[] { options.ProjectFile, "--output", tempDir });

                    foreach (var file in Directory.GetFiles(tempDir, "*", SearchOption.AllDirectories))
                    {
                        if (Path.GetExtension(file).ToLower() == ".meta")
                        {
                            var assetFile = file.Substring(0, file.Length - 5);
                            files.Add(new FileItem
                            {
                                Source = assetFile,
                                Target = assetFile.Substring(tempDir.Length + 1).Replace("\\", "/"),
                            });
                        }
                    }
                }
                else
                {
                    // TODO: Pattern

                    var fileName = Path.GetFileName(fileValue.ToString());
                    var filePath = Path.Combine(projectDir, fileValue.ToString());
                    files.Add(new FileItem
                    {
                        Source = filePath,
                        Target = defaultTargetDir + "/" + fileName,
                        MetaGenerated = true
                    });

                    // if dll, add *.mdb. if not exist, generate one from pdb
                    if (Path.GetExtension(fileName).ToLower() == ".dll")
                    {
                        var mdbFilePath = filePath + ".mdb";

                        if (File.Exists(mdbFilePath) == false ||
                            File.GetLastWriteTime(filePath) > File.GetLastWriteTime(mdbFilePath))
                        {
                            MdbTool.ConvertPdbToMdb(filePath);
                        }

                        if (File.Exists(mdbFilePath))
                        {
                            files.Add(new FileItem
                            {
                                Source = mdbFilePath,
                                Target = defaultTargetDir + "/" + fileName + ".mdb",
                                MetaGenerated = true
                            });
                        }
                    }
                }
            }

            if (files.Any() == false)
                throw new InvalidDataException("Nothing to add for files.");

            var packagePath = Path.Combine(outputDir, p.Id + ".unitypackage");
            using (var packer = new Packer(packagePath))
            {
                var generatedDirs = new HashSet<string>();
                foreach (var file in files)
                {
                    if (file.MetaGenerated)
                    {
                        generatedDirs.Add(Path.GetDirectoryName(file.Target));
                        packer.AddWithMetaGenerated(file.Source, file.Target);
                    }
                    else
                    {
                        packer.Add(file.Source, file.Target);
                    }
                }

                foreach (var dir in generatedDirs)
                {
                    packer.AddDirectoriesWithMetaGenerated(dir);
                }
            }

            if (string.IsNullOrEmpty(tempDir) == false)
                Directory.Delete(tempDir, true);

            return 0;
        }
    }
}
