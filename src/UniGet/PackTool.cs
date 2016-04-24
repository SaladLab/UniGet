using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using CommandLine;
using Newtonsoft.Json;
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

        internal static int Process(Options options)
        {
            var p = Project.Load(options.ProjectFile);
            var projectDir = Path.GetDirectoryName(options.ProjectFile);
            var outputDir = options.OutputDirectory ?? projectDir;

            if (string.IsNullOrEmpty(p.Id))
                throw new InvalidDataException("Cannot find id from project.");

            if (string.IsNullOrEmpty(p.Version))
                throw new InvalidDataException("Cannot find version from project.");

            p.Version = new SemVer.Version(p.Version).ToString();

            if (p.Files == null || p.Files.Any() == false)
                throw new InvalidDataException("Cannot find files from project.");

            var homeBaseDir = "Assets/UnityPackages";
            var homeDir = homeBaseDir + "/" + p.Id;
            var tempDir = Extracter.CreateTemporaryDirectory();

            var files = new List<Project.FileItem>();
            var packagePath = Path.Combine(outputDir, $"{p.Id}.{p.Version}.unitypackage");
            using (var packer = new Packer(packagePath))
            {
                foreach (var fileValue in p.Files)
                {
                    if (fileValue is JObject)
                    {
                        var fileItem = fileValue.ToObject<Project.FileItem>();
                        var filePath = Path.Combine(projectDir, fileItem.Source);
                        var targetResolved = fileItem.Target.Replace("$id$", p.Id)
                                                            .Replace("$home$", homeDir)
                                                            .Replace("$homebase$", homeBaseDir);
                        AddFiles(packer, files, filePath, targetResolved, fileItem.Extra);
                    }
                    else if (fileValue.ToString().StartsWith("$"))
                    {
                        var keyword = fileValue.ToString().ToLower();
                        if (keyword != "$dependencies$")
                        {
                            throw new InvalidDataException("Wrong keyword: " + keyword);
                        }

                        var mergedProjectMap = RestoreTool.Process(new RestoreTool.Options
                        {
                            ProjectFile = options.ProjectFile,
                            OutputDirectory = tempDir,
                            LocalRepositoryDirectory = options.LocalRepositoryDirectory
                        }).Result;

                        p.MergedDependencies = mergedProjectMap.ToDictionary(
                            i => i.Key,
                            i => new Project.Dependency { Version = i.Value.ToString() });

                        foreach (var file in Directory.GetFiles(tempDir, "*", SearchOption.AllDirectories))
                        {
                            if (Path.GetExtension(file).ToLower() == ".meta")
                            {
                                var assetFile = file.Substring(0, file.Length - 5);
                                var targetFile = assetFile.Substring(tempDir.Length + 1).Replace("\\", "/");

                                // NOTE:
                                // if Extra field of file in merged dependencies,
                                // read *.unitypackage.json and use data from them.

                                if (File.Exists(assetFile))
                                {
                                    files.Add(new Project.FileItem
                                    {
                                        Source = assetFile,
                                        Target = targetFile,
                                        Merged = true
                                    });
                                }

                                packer.Add(assetFile, targetFile);
                            }
                        }
                    }
                    else
                    {
                        var filePath = Path.Combine(projectDir, fileValue.ToString());
                        AddFiles(packer, files, filePath, homeDir + "/", false);
                    }
                }

                if (files.Any() == false)
                    throw new InvalidDataException("Nothing to add for files.");

                // make files

                var jsonSettings = new JsonSerializerSettings
                {
                    DefaultValueHandling = DefaultValueHandling.Ignore,
                };
                p.Files = files.Select(
                    f => (f.Extra || f.Merged)
                        ? JToken.FromObject(new Project.FileItem { Target = f.Target, Extra = f.Extra, Merged = f.Merged }, JsonSerializer.Create(jsonSettings))
                        : JToken.FromObject(f.Target)).ToList();

                // add project.json

                var projectPath = Path.Combine(tempDir, p.Id + ".unitypackage.json");
                File.WriteAllText(projectPath,  JsonConvert.SerializeObject(p, Formatting.Indented, jsonSettings));
                packer.AddWithMetaGenerated(projectPath, homeBaseDir + "/" + p.Id + ".unitypackage.json");
                packer.AddDirectoriesWithMetaGenerated(homeBaseDir);
            }

            if (string.IsNullOrEmpty(tempDir) == false)
                Directory.Delete(tempDir, true);

            return 0;
        }

        internal static void AddFiles(Packer packer, List<Project.FileItem> files, string source, string target, bool extra)
        {
            var dirs = new HashSet<string>();

            foreach (var f in FileUtility.GetFiles(source, target))
            {
                var srcFile = f.Item1;
                var dstFile = f.Item2.Replace("\\", "/");

                if (Path.GetExtension(srcFile).ToLower() == ".meta")
                    continue;

                // add file

                files.Add(new Project.FileItem
                {
                    Source = srcFile,
                    Target = dstFile,
                    Extra = extra,
                });

                if (File.Exists(srcFile + ".meta"))
                    packer.Add(srcFile, dstFile);
                else
                    packer.AddWithMetaGenerated(srcFile, dstFile);

                dirs.Add(Path.GetDirectoryName(dstFile).Replace("\\", "/"));

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
                        files.Add(new Project.FileItem
                        {
                            Source = mdbFilePath,
                            Target = dstFile + ".mdb",
                            Extra = extra,
                        });

                        packer.AddWithMetaGenerated(mdbFilePath, dstFile + ".mdb");
                    }
                }
            }

            foreach (var dir in dirs)
            {
                packer.AddDirectoriesWithMetaGenerated(dir);
            }
        }
    }
}
