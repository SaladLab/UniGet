using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace UniGet.Tests
{
    public class RemoveTest
    {
        [Fact]
        private async Task Test_Simple()
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

            var restorePath = TestHelper.CreateOutputPath("Restore");
            await RestoreTool.Process(new RestoreTool.Options
            {
                ProjectFile = TestHelper.GetDataPath("DepD.json"),
                OutputDirectory = restorePath,
                LocalRepositoryDirectory = TestHelper.GetOutputPath()
            });

            // Act

            RemoveTool.Process(new RemoveTool.Options
            {
                ProjectDir = restorePath
            });

            // Assert

            var basePath = Path.Combine(restorePath, "Assets", "UnityPackages");
            AssertFileNotExistsWithMeta(basePath, "DepA.unitypackage.json");
            AssertFileNotExistsWithMeta(basePath, "DepA", "FileA.txt");
            AssertFileNotExistsWithMeta(basePath, "DepB.unitypackage.json");
            AssertFileNotExistsWithMeta(basePath, "DepB", "FileB.txt");
            AssertFileNotExistsWithMeta(basePath, "DepC.unitypackage.json");
            AssertFileNotExistsWithMeta(basePath, "DepC", "FileC.txt");
            AssertFolderNotExists(basePath);
            AssertFolderNotExists(basePath, "DepA");
            AssertFolderNotExists(basePath, "DepB");
            AssertFolderNotExists(basePath, "DepC");

        }

        private void AssertFolderNotExists(params string[] names)
        {
            Assert.False(Directory.Exists(Path.Combine(names)), "Dir: " + Path.Combine(names));
        }

        private void AssertFileNotExistsWithMeta(params string[] names)
        {
            Assert.False(File.Exists(Path.Combine(names)), "File: " + Path.Combine(names));
            Assert.False(File.Exists(Path.Combine(names) + ".meta"), "File: " + Path.Combine(names) + ".meta");
        }
    }
}
