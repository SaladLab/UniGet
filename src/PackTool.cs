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
            var p = JObject.Parse(File.ReadAllText(options.ProjectFile));
            var projectDir = Path.GetDirectoryName(options.ProjectFile);
            var outputDir = options.OutputDirectory ?? projectDir;

            var idValue = p.GetValue("id");
            if (idValue == null)
                throw new InvalidDataException("Cannot find id from project.");
            var projectId = idValue.ToString();

            var filesValue = (JArray)p.GetValue("files");
            if (filesValue == null)
                throw new InvalidDataException("Cannot find files from project.");

            var defaultTargetDir = "Assets/UnityPackages/" + projectId;

            var files = new List<FileItem>();
            foreach (var fileValue in filesValue)
            {
                if (fileValue is JObject)
                {
                    // TODO: More
                }
                else
                {
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

                        if (File.Exists(mdbFilePath) == false)
                            MdbTool.ConvertPdbToMdb(filePath);

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

            var packagePath = Path.Combine(outputDir, projectId + ".unitypackage");
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

            return 0;
        }
    }
}
