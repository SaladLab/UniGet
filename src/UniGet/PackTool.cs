using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using CommandLine;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

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

            [Option('l', "local", HelpText = "Specifies the directory for the local repository.")]
            public string LocalRepositoryDirectory { get; set; }
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

        internal static int Process(Options options)
        {
            var p = Project.Load(options.ProjectFile);
            var projectDir = Path.GetDirectoryName(options.ProjectFile);
            var outputDir = options.OutputDirectory ?? projectDir;

            if (string.IsNullOrEmpty(p.Id))
                throw new InvalidDataException("Cannot find id from project.");

            if (string.IsNullOrEmpty(p.Version))
                throw new InvalidDataException("Cannot find version from project.");

            if (p.Files == null || p.Files.Any() == false)
                throw new InvalidDataException("Cannot find files from project.");

            var defaultTargetDir = "Assets/UnityPackages/" + p.Id;
            var tempDir = Extracter.CreateTemporaryDirectory();

            var files = new List<FileItem>();
            foreach (var fileValue in p.Files)
            {
                if (fileValue is JObject)
                {
                    var fileItem = fileValue.ToObject<FileItem>();
                    var filePath = Path.Combine(projectDir, fileItem.Source);
                    AddFiles(files, filePath, fileItem.Target);
                }
                else if (fileValue.ToString().StartsWith("$"))
                {
                    var keyword = fileValue.ToString().ToLower();
                    if (keyword != "$dependencies$")
                    {
                        throw new InvalidDataException("Wrong keyword: " + keyword);
                    }

                    RestoreTool.Process(new RestoreTool.Options
                    {
                        ProjectFile = options.ProjectFile,
                        OutputDirectory = tempDir,
                        LocalRepositoryDirectory = options.LocalRepositoryDirectory
                    }).Wait();

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
                    var filePath = Path.Combine(projectDir, fileValue.ToString());
                    AddFiles(files, filePath, defaultTargetDir + "/");
                }
            }

            if (files.Any() == false)
                throw new InvalidDataException("Nothing to add for files.");

            var packagePath = Path.Combine(outputDir, p.Id + ".unitypackage");
            using (var packer = new Packer(packagePath))
            {
                var generatedDirs = new HashSet<string>();

                // add files
                foreach (var file in files)
                {
                    if (file.MetaGenerated)
                    {
                        generatedDirs.Add(Path.GetDirectoryName(file.Target).Replace("\\", "/"));
                        packer.AddWithMetaGenerated(file.Source, file.Target);
                    }
                    else
                    {
                        packer.Add(file.Source, file.Target);
                    }
                }

                // add project.json
                var projectPath = Path.Combine(tempDir, p.Id + ".unitypackage.json");
                p.Files = null;
                var jsonSettings = new JsonSerializerSettings
                {
                    DefaultValueHandling = DefaultValueHandling.Ignore,
                };
                File.WriteAllText(projectPath,  JsonConvert.SerializeObject(p, Formatting.Indented, jsonSettings));
                generatedDirs.Add(defaultTargetDir);
                packer.AddWithMetaGenerated(projectPath, defaultTargetDir + "/" + p.Id + ".unitypackage.json");

                // add meta of directories
                foreach (var dir in generatedDirs)
                {
                    packer.AddDirectoriesWithMetaGenerated(dir);
                }
            }

            if (string.IsNullOrEmpty(tempDir) == false)
                Directory.Delete(tempDir, true);

            return 0;
        }

        internal static void AddFiles(List<FileItem> files, string source, string target)
        {
            foreach (var f in FileUtility.GetFiles(source, target))
            {
                var srcFile = f.Item1;
                var dstFile = f.Item2.Replace("\\", "/");

                if (Path.GetExtension(srcFile).ToLower() == ".meta")
                    continue;

                if (File.Exists(srcFile + ".meta"))
                {
                    files.Add(new FileItem
                    {
                        Source = srcFile,
                        Target = dstFile,
                    });
                }
                else
                {
                    files.Add(new FileItem
                    {
                        Source = srcFile,
                        Target = dstFile,
                        MetaGenerated = true
                    });
                }

                // if dll, add *.mdb. if not exist, generate one from pdb
                if (Path.GetExtension(srcFile).ToLower() == ".dll")
                {
                    var mdbFilePath = srcFile + ".mdb";

                    if (File.Exists(mdbFilePath) == false ||
                        File.GetLastWriteTime(srcFile) > File.GetLastWriteTime(mdbFilePath))
                    {
                        MdbTool.ConvertPdbToMdb(srcFile);
                    }

                    if (File.Exists(mdbFilePath))
                    {
                        files.Add(new FileItem
                        {
                            Source = mdbFilePath,
                            Target = dstFile + ".mdb",
                            MetaGenerated = true
                        });
                    }
                }
            }
        }
    }
}
