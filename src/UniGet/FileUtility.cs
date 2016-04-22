using System;
using System.IO;
using System.Collections.Generic;

namespace UniGet
{
    internal static class FileUtility
    {
        public static IEnumerable<Tuple<string, string>> GetFiles(string source, string target)
        {
            if (source.Contains("*") || source.Contains("?"))
            {
                // search with wildcard
                var sourceDir = Path.GetDirectoryName(source);
                var sourceFileName = Path.GetFileName(source);
                var option = SearchOption.TopDirectoryOnly;
                if (sourceDir.EndsWith("**"))
                {
                    option = SearchOption.AllDirectories;
                    sourceDir = sourceDir.Substring(0, sourceDir.Length - 3);
                }
                foreach (var file in Directory.GetFiles(sourceDir, sourceFileName, option))
                {
                    yield return Tuple.Create(file, Path.Combine(target, file.Substring(sourceDir.Length + 1)));
                }
            }
            else
            {
                // simple
                if (target.EndsWith("/") || target.EndsWith("\\"))
                    yield return Tuple.Create(source, Path.Combine(target, Path.GetFileName(source)));
                else
                    yield return Tuple.Create(source, target);
            }
        }
    }
}
