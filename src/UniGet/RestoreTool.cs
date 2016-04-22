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
            else if (projectDependency.Source.StartsWith("https://github.com"))
            {
                var parts = projectDependency.Source.Split('/');
                if (parts.Length < 3)
                    throw new InvalidDataException("Cannot determine github repo information from url: " + projectDependency.Source);

                var r = await DownloadGithubReleaseAsync(
                    parts[parts.Length - 2], parts[parts.Length - 1],
                    projectId, versionRange);
                packageFile = r.Item1;
                packageVersion = r.Item2;
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

        internal class PackageRelease
        {
            public SemVer.Version Version;
            public List<Tuple<string, string>> DownloadUrls;
        }

        private static async Task<List<PackageRelease>> GetGithubReleasesAsync(string owner, string repoName)
        {
            var packageRelease = new List<PackageRelease>();

            var client = new GitHubClient(new ProductHeaderValue("uniget"));
            var githubReleases = await client.Repository.Release.GetAll(owner, repoName);
            foreach (var release in githubReleases)
            {
                var ver = GetVersionFromRelease(release.TagName);
                if (ver == null)
                    continue;

                var downloadUrls = new List<Tuple<string, string>>();
                foreach (var a in release.Assets)
                {
                    if (Path.GetExtension(a.Name).ToLower() == ".unitypackage")
                        downloadUrls.Add(Tuple.Create(a.Name, a.BrowserDownloadUrl));
                }

                if (downloadUrls.Any())
                {
                    packageRelease.Add(new PackageRelease
                    {
                        Version = ver,
                        DownloadUrls = downloadUrls
                    });
                }
            }

            return packageRelease;
        }

        private static SemVer.Version GetVersionFromRelease(string tagName)
        {
            try
            {
                var i = tagName.ToList().FindIndex(c => char.IsDigit(c));
                if (i != -1)
                    return new SemVer.Version(tagName.Substring(i));
            }
            catch (Exception)
            {
            }
            return null;
        }

        private static string GetPackageCacheRoot()
        {
            return Path.Combine(Environment.GetEnvironmentVariable("APPDATA"), "uniget");
        }

        private static async Task<Tuple<string, SemVer.Version>> DownloadGithubReleaseAsync(
            string owner, string repoName, string assetName, SemVer.Range versionRange, bool force = false)
        {
            var releases = await GetGithubReleasesAsync(owner, repoName);
            var release = releases.Where(r => versionRange.IsSatisfied(r.Version))
                                  .OrderBy(r => r.Version).LastOrDefault();
            if (release == null)
                throw new ArgumentException("Cannot find release matched version range.");

            var url = release.DownloadUrls.FirstOrDefault(d => Path.GetFileNameWithoutExtension(d.Item1) == assetName);
            if (url == null)
                throw new ArgumentException("Cannot find asset name.");

            var cacheRootPath = GetPackageCacheRoot();
            if (Directory.Exists(cacheRootPath) == false)
                Directory.CreateDirectory(cacheRootPath);

            var saveFileName = Path.Combine(
                cacheRootPath,
                string.Format("{0}~{1}~{2}~{3}.unitypackage", owner, repoName, release.Version, assetName));

            if (File.Exists(saveFileName) == false || force)
            {
                using (var httpClient = new HttpClient())
                using (var request = new HttpRequestMessage(HttpMethod.Get, url.Item2))
                {
                    var response = await httpClient.SendAsync(request);

                    using (var contentStream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = new FileStream(saveFileName, System.IO.FileMode.Create, FileAccess.Write))
                    {
                        await contentStream.CopyToAsync(fileStream);
                    }
                }
            }

            return Tuple.Create(saveFileName, release.Version);
        }
    }
}
