using System;
using System.IO;

namespace UniGet
{
    internal static class NugetPackage
    {
        public static string GetPackageCacheRoot()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "uniget", "nuget");
        }

        public static Tuple<string, SemVer.Version> DownloadPackage(
            string packageId, string version, bool force = false)
        {
            var cacheRootPath = GetPackageCacheRoot();
            if (Directory.Exists(cacheRootPath) == false)
                Directory.CreateDirectory(cacheRootPath);

            var ret = NuGet.CommandLine.Program.Main(
                new[]
                {
                    "install", packageId,
                    "-Version", version, "-Prerelease",
                    "-Source", "nuget.org", "-NonInteractive",
                    "-OutputDirectory", cacheRootPath
                });

            if (ret != 0)
                throw new InvalidOperationException("Nuget exited with error code: " + ret);

            var packagePath = Path.Combine(cacheRootPath, packageId + "." + version);
            if (Directory.Exists(packagePath) == false)
                throw new InvalidOperationException("Cannot find the package nuget downloaded: " + packagePath);

            var ver = new SemVer.Version(version, true);
            return Tuple.Create(packagePath, ver);
        }

        public static void ExtractPackage(
            string packageId, string version, string tarketFrameworkMoniker, string outputDir)
        {
            var cacheRootPath = GetPackageCacheRoot();

            var packagePath = Path.Combine(cacheRootPath, packageId + "." + version);
            if (Directory.Exists(packagePath) == false)
                throw new InvalidOperationException("Cannot find the package: " + packagePath);

            var libPath = Path.Combine(packagePath, "lib", tarketFrameworkMoniker);
            if (Directory.Exists(libPath) == false)
                throw new InvalidOperationException("Cannot find lib directory: " + libPath);

            var target = $"Assets/UnityPackages/{packageId}";
            var targetDir = Path.Combine(outputDir, target);
            Directory.CreateDirectory(targetDir);
            File.WriteAllBytes(targetDir + ".meta",
                               Packer.GenerateMeta(".", target).Item2);

            Packer.GenerateMeta(".", target);

            foreach (var file in Directory.GetFiles(libPath, "*.dll"))
            {
                var fileName = Path.GetFileName(file);

                var mdbFile = file + ".mdb";
                if (File.Exists(mdbFile) == false)
                    MdbTool.ConvertPdbToMdb(file);

                File.Copy(file, Path.Combine(targetDir, fileName), true);
                File.WriteAllBytes(Path.Combine(targetDir, fileName) + ".meta",
                                   Packer.GenerateMeta(file, target + "/" + fileName).Item2);

                if (File.Exists(mdbFile))
                {
                    File.Copy(mdbFile, Path.Combine(targetDir, fileName + ".mdb"), true);
                    File.WriteAllBytes(Path.Combine(targetDir, fileName) + ".mdb.meta",
                                       Packer.GenerateMeta(file, target + "/" + fileName + ".mdb").Item2);
                }
            }
        }
    }
}
