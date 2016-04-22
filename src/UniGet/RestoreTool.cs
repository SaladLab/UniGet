using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using CommandLine;
using Octokit;

namespace UniGet
{
    internal static class RestoreTool
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

            // Run process !

            if (options == null)
                return 1;

            var packageMap = Process(options).Result;
            foreach (var p in packageMap)
            {
                Console.WriteLine($"Restored: {p.Key}: {p.Value}");
            }
            return 0;
        }

        internal static async Task<Dictionary<string, SemVer.Version>> Process(Options options)
        {
            var p = Project.Load(options.ProjectFile);
            var projectDir = Path.GetDirectoryName(options.ProjectFile);
            var outputDir = options.OutputDirectory ?? projectDir;
            var packageMap = new Dictionary<string, SemVer.Version>();

            if (p.Dependencies == null || p.Dependencies.Any() == false)
            {
                Console.WriteLine("No dependencies.");
                return packageMap;
            }

            var context = new ProcessContext
            {
                Options = options,
                OutputDir = outputDir,
                PackageMap = packageMap
            };
            foreach (var d in p.Dependencies)
            {
                await ProcessRecursive(d.Key, d.Value, context);
            }

            return packageMap;
        }

        private class ProcessContext
        {
            public Options Options;
            public string OutputDir;
            public Dictionary<string, SemVer.Version> PackageMap;
        }

        private static async Task ProcessRecursive(string projectId, Project.Dependency projectDependency, ProcessContext context)
        {
            Console.WriteLine("Restore: " + projectId);

            // download package

            var versionRange = new SemVer.Range(projectDependency.Version);

            var packageFile = "";
            var packageVersion = new SemVer.Version(0, 0, 0);

            if (projectDependency.Source == "local")
            {
                var versions = new List<SemVer.Version>();

                var files = Directory.GetFiles(context.Options.LocalRepositoryDirectory ?? "", projectId + ".*.unitypackage");
                foreach (var file in files)
                {
                    var verStr = Path.GetFileNameWithoutExtension(file).Substring(projectId.Length + 1);
                    try
                    {
                        var ver = new SemVer.Version(verStr);
                        if (versionRange.IsSatisfied(ver))
                            versions.Add(ver);
                    }
                    catch (Exception)
                    {
                    }
                }

                if (versions.Any() == false)
                    throw new InvalidOperationException("Cannot find package from local repository: " + projectId);

                packageVersion = versions.Max();
                packageFile = Path.Combine(context.Options.LocalRepositoryDirectory ?? "", $"{projectId}.{packageVersion}.unitypackage");
                if (File.Exists(packageFile) == false)
                    throw new InvalidOperationException("Cannot find package from local repository: " + projectId);
            }
            else if (projectDependency.Source.StartsWith("github:"))
            {
                var parts = projectDependency.Source.Substring(7).Split('/');
                if (parts.Length != 2)
                    throw new InvalidDataException("Cannot determine github repo information from url: " + projectDependency.Source);

                var r = await GithubPackage.DownloadPackageAsync(parts[0], parts[1], projectId, versionRange);
                packageFile = r.Item1;
                packageVersion = r.Item2;
            }
            else if (projectDependency.Source.StartsWith("nuget:"))
            {
                throw new NotImplementedException("nuget not yet!");
            }
            else
            {
                throw new InvalidOperationException("Cannot recognize source: " + projectDependency.Source);
            }

            Func<string, bool> filter = null;
            if (projectDependency.Includes != null || projectDependency.Excludes != null)
            {
                filter = Extracter.MakeFilter(projectDependency.Includes ?? new List<string>(),
                                              projectDependency.Excludes ?? new List<string>());
            }

            // exctract

            Extracter.ExtractUnityPackage(packageFile, context.OutputDir, filter);
            context.PackageMap.Add(projectId, packageVersion);

            // deep into dependencies

            var projectFile = Path.Combine(context.OutputDir, $"Assets/UnityPackages/{projectId}.unitypackage.json");
            if (File.Exists(projectFile))
            {
                var project = Project.Load(projectFile);
                if (project.MergedDependencies != null)
                {
                    foreach (var d in project.Dependencies)
                    {
                        context.PackageMap[d.Key] = new SemVer.Version(d.Value.Version);
                    }
                }
                if (project.Dependencies != null)
                {
                    foreach (var d in project.Dependencies)
                    {
                        if (context.PackageMap.ContainsKey(d.Key) == false)
                        {
                            await ProcessRecursive(d.Key, d.Value, context);
                        }
                    }
                }
            }
        }
    }
}
