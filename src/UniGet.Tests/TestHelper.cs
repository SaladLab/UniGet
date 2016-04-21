using System;
using System.IO;

namespace UniGet.Tests
{
    internal static class TestHelper
    {
        public static string GetDataPath()
        {
            var path = Directory.GetCurrentDirectory();
            var idx = path.LastIndexOf("UniGet.Tests");
            if (idx == -1)
                throw new InvalidOperationException();
            return Path.Combine(path.Substring(0, idx + 12), "TestData");
        }

        public static string GetDataPath(string fileName)
        {
            return Path.Combine(GetDataPath(), fileName);
        }

        public static string GetOutputPath()
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "output");
            if (Directory.Exists(path) == false)
                Directory.CreateDirectory(path);
            return path;
        }

        public static string CreateOutputPath(string name)
        {
            var path = Path.Combine(GetOutputPath(), name);
            if (Directory.Exists(path))
                Directory.Delete(path, true);
            Directory.CreateDirectory(path);
            return path;
        }
    }
}
