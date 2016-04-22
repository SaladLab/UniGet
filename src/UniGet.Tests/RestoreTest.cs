using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace UniGet.Tests
{
    public class RestoreTest
    {
        [Fact]
        private async Task Test_Simple()
        {
            // Arrange

            PackTool.Process(new PackTool.Options
            {
                ProjectFile = TestHelper.GetDataPath("DepA.json"),
                OutputDirectory = TestHelper.GetOutputPath()
            });

            // Act

            var restorePath = TestHelper.CreateOutputPath("Restore");
            await RestoreTool.Process(new RestoreTool.Options
            {
                ProjectFile = TestHelper.GetDataPath("DepB.json"),
                OutputDirectory = restorePath,
                LocalRepositoryDirectory = TestHelper.GetOutputPath()
            });

            // Assert

            var packagePath = Path.Combine(restorePath, "Assets", "UnityPackages", "DepA");
            AssertFileExistsWithMeta(packagePath, "DepA.unitypackage.json");
            AssertFileExistsWithMeta(packagePath, "FileA.txt");
        }

        [Fact]
        private async Task Test_Recursive()
        {
            // Arrange

            foreach (var package in new[] { "DepA", "DepB", "DepC" })
            {
                PackTool.Process(new PackTool.Options
                {
                    ProjectFile = TestHelper.GetDataPath(package + ".json"),
                    OutputDirectory = TestHelper.GetOutputPath()
                });
            }

            // Act

            var restorePath = TestHelper.CreateOutputPath("Restore");
            await RestoreTool.Process(new RestoreTool.Options
            {
                ProjectFile = TestHelper.GetDataPath("DepD.json"),
                OutputDirectory = restorePath,
                LocalRepositoryDirectory = TestHelper.GetOutputPath()
            });

            // Assert

            var basePath = Path.Combine(restorePath, "Assets", "UnityPackages");
            AssertFileExistsWithMeta(basePath, "DepA", "DepA.unitypackage.json");
            AssertFileExistsWithMeta(basePath, "DepA", "FileA.txt");
            AssertFileExistsWithMeta(basePath, "DepB", "DepB.unitypackage.json");
            AssertFileExistsWithMeta(basePath, "DepB", "FileB.txt");
            AssertFileExistsWithMeta(basePath, "DepC", "DepC.unitypackage.json");
            AssertFileExistsWithMeta(basePath, "DepC", "FileC.txt");
        }

        private void AssertFileExists(params string[] names)
        {
            Assert.True(File.Exists(Path.Combine(names)), "File: " + Path.Combine(names));
        }

        private void AssertFileExistsWithMeta(params string[] names)
        {
            Assert.True(File.Exists(Path.Combine(names)), "File: " + Path.Combine(names));
            Assert.True(File.Exists(Path.Combine(names) + ".meta"), "File: " + Path.Combine(names) + ".meta");
        }
    }
}
