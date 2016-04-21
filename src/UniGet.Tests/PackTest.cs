using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace UniGet.Tests
{
    public class PackTest
    {
        [Fact]
        private void Test_PackSimple()
        {
            // Act

            var options = new PackTool.Options
            {
                ProjectFile = TestHelper.GetDataPath("Simple.json"),
                OutputDirectory = TestHelper.GetOutputPath()
            };
            PackTool.Process(options);

            // Assert

            var unpackPath = TestHelper.CreateOutputPath("Unpack");
            Extracter.ExtractUnityPackage(TestHelper.GetOutputPath() + "/Simple.unitypackage", unpackPath, null);

            var packagePath = Path.Combine(unpackPath, "Assets", "UnityPackages", "Simple");
            Assert.True(File.Exists(Path.Combine(packagePath, "Simple.unitypackage.json")));
            Assert.True(File.Exists(Path.Combine(packagePath, "Simple.unitypackage.json.meta")));
            Assert.True(File.Exists(Path.Combine(packagePath, "Text1.txt")));
            Assert.True(File.Exists(Path.Combine(packagePath, "Text1.txt.meta")));
            Assert.True(File.Exists(Path.Combine(packagePath, "Text2.txt")));
            Assert.True(File.Exists(Path.Combine(packagePath, "Text2.txt.meta")));

            Assert.True(File.Exists(Path.Combine(unpackPath, "Assets", "UnityPackages.meta")));
            Assert.True(File.Exists(Path.Combine(unpackPath, "Assets", "UnityPackages", "Simple.meta")));
        }

        [Fact]
        private void Test_PackFileItem()
        {
            // Act

            var options = new PackTool.Options
            {
                ProjectFile = TestHelper.GetDataPath("FileItem.json"),
                OutputDirectory = TestHelper.GetOutputPath()
            };
            PackTool.Process(options);

            // Assert

            var unpackPath = TestHelper.CreateOutputPath("Unpack");
            Extracter.ExtractUnityPackage(TestHelper.GetOutputPath() + "/FileItem.unitypackage", unpackPath, null);

            var basePath = Path.Combine(unpackPath, "Assets", "UnityPackages");
            Assert.True(File.Exists(Path.Combine(basePath, "FileItem/FileItem.unitypackage.json")));
            Assert.True(File.Exists(Path.Combine(basePath, "FileItem/FileItem.unitypackage.json.meta")));
            Assert.True(File.Exists(Path.Combine(basePath, "1/Text.txt")));
            Assert.True(File.Exists(Path.Combine(basePath, "1/Text.txt.meta")));
            Assert.True(File.Exists(Path.Combine(basePath, "2/Text.txt")));
            Assert.True(File.Exists(Path.Combine(basePath, "2/Text.txt.meta")));

            Assert.True(File.Exists(Path.Combine(unpackPath, "Assets", "UnityPackages.meta")));
            Assert.True(File.Exists(Path.Combine(unpackPath, "Assets", "UnityPackages", "1.meta")));
            Assert.True(File.Exists(Path.Combine(unpackPath, "Assets", "UnityPackages", "2.meta")));
            Assert.True(File.Exists(Path.Combine(unpackPath, "Assets", "UnityPackages", "FileItem.meta")));
        }
    }
}
