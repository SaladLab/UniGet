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
            AssertFileExistsWithMeta(packagePath, "Simple.unitypackage.json");
            AssertFileExistsWithMeta(packagePath, "Text1.txt");
            AssertFileExistsWithMeta(packagePath, "Text2.txt");

            AssertFileExists(unpackPath, "Assets", "UnityPackages.meta");
            AssertFileExists(unpackPath, "Assets", "UnityPackages", "Simple.meta");
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
            AssertFileExistsWithMeta(basePath, "FileItem", "FileItem.unitypackage.json");
            AssertFileExistsWithMeta(basePath, "1/Text.txt");
            AssertFileExistsWithMeta(basePath, "2/Text.txt");

            AssertFileExists(unpackPath, "Assets", "UnityPackages.meta");
            AssertFileExists(unpackPath, "Assets", "UnityPackages", "1.meta");
            AssertFileExists(unpackPath, "Assets", "UnityPackages", "2.meta");
            AssertFileExists(unpackPath, "Assets", "UnityPackages", "FileItem.meta");
        }

        private void AssertFileExists(params string[] names)
        {
            Assert.True(File.Exists(Path.Combine(names)));
        }

        private void AssertFileExistsWithMeta(params string[] names)
        {
            Assert.True(File.Exists(Path.Combine(names)));
            Assert.True(File.Exists(Path.Combine(names) + ".meta"));
        }
    }
}
