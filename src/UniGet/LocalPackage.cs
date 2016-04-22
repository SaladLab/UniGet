using System;
using System.Collections.Generic;
using System.IO;

namespace UniGet
{
    internal static class LocalPackage
    {
        public static List<Tuple<string, SemVer.Version>> GetPackages(string repositoryDirectory, string projectId)
        {
            var packages = new List<Tuple<string, SemVer.Version>>();
            var files = Directory.GetFiles(repositoryDirectory, projectId + ".*.unitypackage");
            foreach (var file in files)
            {
                var verStr = Path.GetFileNameWithoutExtension(file).Substring(projectId.Length + 1);
                try
                {
                    var ver = new SemVer.Version(verStr);
                    packages.Add(Tuple.Create(file, ver));
                }
                catch (Exception)
                {
                }
            }
            return packages;
        }
    }
}
