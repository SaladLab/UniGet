using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using Newtonsoft.Json;
using Octokit;
using CommandLine;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;

namespace UniGet
{
    internal static class RestoreTool
    {
        internal class Options
        {
            [Value(0, Required = true, HelpText = "Project File")]
            public string ProjectFile { get; set; }
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

            if (options != null)
                return Process(options).Result;
            else
                return 1;
        }

        private static async Task<int> Process(Options options)
        {
            //var client = new GitHubClient(new ProductHeaderValue("octokit.samples"));
            //var releases = await client.Repository.Release.GetAll("SaladLab", "Unity3D.UiManager");
            //foreach (var release in releases)
            //{
            //    Console.WriteLine(release.Name);
            //}

            //var releases = await GetGithubReleasesAsync("SaladLab", "Unity3D.UiManager");
            //foreach (var release in releases)
            //{
            //    Console.WriteLine(release.Version.ToString() + ":" + JsonConvert.SerializeObject(release.DownloadUrls));
            //}

            // await DownloadGithubReleaseAsync("SaladLab", "Json.Net.Unity3D", "Json-Net-Unity3D", new SemVer.Range(">=1.0.0"), false);
            ExtractUnityPackage(
                @"D:\Project\GitHub\UniGet\src\bin\Debug\SaladLab~Json.Net.Unity3D~8.0.3~Json-Net-Unity3D.unitypackage",
                @"D:\Project\GitHub\UniGet\src\bin\Debug\Test", null);

            return 0;
        }

        internal class PackageRelease
        {
            public SemVer.Version Version;
            public List<Tuple<string, string>> DownloadUrls;
        }

        private static async Task<List<PackageRelease>> GetGithubReleasesAsync(string owner, string repoName)
        {
            var packageRelease = new List<PackageRelease>();

            var client = new GitHubClient(new ProductHeaderValue("octokit.samples"));
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

        private static async Task<string> DownloadGithubReleaseAsync(
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

            return saveFileName;
        }

        private static void ExtractUnityPackage(string packageFile, string outputDir, Func<string, bool> filter)
        {
            var tempPath = GetTemporaryDirectory();

            using (var fileStream = new FileStream(packageFile, System.IO.FileMode.Open, FileAccess.Read))
            using (var gzipStream = new GZipInputStream(fileStream))
            {
                var tarArchive = TarArchive.CreateInputTarArchive(gzipStream);
                tarArchive.ExtractContents(tempPath);
                tarArchive.Close();
            }

            Directory.Delete(tempPath, true);
        }

        private static string GetTemporaryDirectory()
        {
            var tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDirectory);
            return tempDirectory;
        }
    }
}
