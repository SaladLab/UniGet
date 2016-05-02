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
                ProjectFile = TestHelper.GetDataPath("Simple.json"),
                OutputDirectory = TestHelper.GetOutputPath()
            });

            // Act

            var restorePath = TestHelper.CreateOutputPath("Restore");
            await RestoreTool.Process(new RestoreTool.Options
            {
                ProjectFile = TestHelper.GetDataPath("SimpleRestore.json"),
                OutputDirectory = restorePath,
                LocalRepositoryDirectory = TestHelper.GetOutputPath()
            });

            // Assert

            var packagePath = Path.Combine(restorePath, "Assets", "UnityPackages", "Simple");
            AssertFileExistsWithMeta(packagePath, "../Simple.unitypackage.json");
            AssertFileExistsWithMeta(packagePath, "Text1.txt");
            AssertFileExistsWithMeta(packagePath, "Text2.txt");
            AssertFileNotExists(packagePath, "SubDir", "TextInSubDir.txt");

            AssertFileExists(restorePath, "Assets", "UnityPackages.meta");
            AssertFileExists(restorePath, "Assets", "UnityPackages", "Simple.meta");
        }

        [Fact]
        private async Task Test_SimpleWithExtra()
        {
            // Arrange

            PackTool.Process(new PackTool.Options
            {
                ProjectFile = TestHelper.GetDataPath("Simple.json"),
                OutputDirectory = TestHelper.GetOutputPath()
            });

            // Act

            var restorePath = TestHelper.CreateOutputPath("Restore");
            await RestoreTool.Process(new RestoreTool.Options
            {
                ProjectFile = TestHelper.GetDataPath("SimpleRestoreWithExtra.json"),
                OutputDirectory = restorePath,
                LocalRepositoryDirectory = TestHelper.GetOutputPath()
            });

            // Assert

            var packagePath = Path.Combine(restorePath, "Assets", "UnityPackages", "Simple");
            AssertFileExistsWithMeta(packagePath, "../Simple.unitypackage.json");
            AssertFileExistsWithMeta(packagePath, "Text1.txt");
            AssertFileExistsWithMeta(packagePath, "Text2.txt");
            AssertFileExistsWithMeta(packagePath, "SubDir", "TextInSubDir.txt");

            AssertFileExists(restorePath, "Assets", "UnityPackages.meta");
            AssertFileExists(restorePath, "Assets", "UnityPackages", "Simple.meta");
            AssertFileExists(restorePath, "Assets", "UnityPackages", "Simple", "SubDir.meta");
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
            AssertFileExistsWithMeta(basePath, "DepA.unitypackage.json");
            AssertFileExistsWithMeta(basePath, "DepA", "FileA.txt");
            AssertFileExistsWithMeta(basePath, "DepB.unitypackage.json");
            AssertFileExistsWithMeta(basePath, "DepB", "FileB.txt");
            AssertFileExistsWithMeta(basePath, "DepC.unitypackage.json");
            AssertFileExistsWithMeta(basePath, "DepC", "FileC.txt");
        }

        [Fact]
        private async Task Test_Lowest()
        {
            // Arrange

            foreach (var package in new[] { "DepY.1.0.0", "DepY.1.1.0" })
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
                ProjectFile = TestHelper.GetDataPath("ProjectDepY_Lowest.json"),
                OutputDirectory = restorePath,
                LocalRepositoryDirectory = TestHelper.GetOutputPath()
            });

            // Assert

            var basePath = Path.Combine(restorePath, "Assets", "UnityPackages");
            AssertFileExistsWithMeta(basePath, "DepY.unitypackage.json");
            AssertFileExistsWithMeta(basePath, "DepY", "FileY.txt");
        }

        [Fact]
        private async Task Test_Floating()
        {
            // Arrange

            foreach (var package in new[] { "DepY.1.0.0", "DepY.1.1.0" })
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
                ProjectFile = TestHelper.GetDataPath("ProjectDepY_Floating.json"),
                OutputDirectory = restorePath,
                LocalRepositoryDirectory = TestHelper.GetOutputPath()
            });

            // Assert

            var basePath = Path.Combine(restorePath, "Assets", "UnityPackages");
            AssertFileExistsWithMeta(basePath, "DepY.unitypackage.json");
            AssertFileExistsWithMeta(basePath, "DepY", "FileYv110.txt");
        }

        [Fact]
        private async Task Test_Recursive_NearestWin()
        {
            // Arrange:
            // Project -> DepX 1.x -> DepY >=1.0
            //         -> DepY 1.x
            // - Available DepY: 1.0, 1.1

            foreach (var package in new[] { "DepX.1.0.0", "DepY.1.0.0", "DepY.1.1.0" })
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
                ProjectFile = TestHelper.GetDataPath("ProjectDepX_Y.json"),
                OutputDirectory = restorePath,
                LocalRepositoryDirectory = TestHelper.GetOutputPath()
            });

            // Assert

            var basePath = Path.Combine(restorePath, "Assets", "UnityPackages");
            AssertFileExistsWithMeta(basePath, "DepX.unitypackage.json");
            AssertFileExistsWithMeta(basePath, "DepX", "FileX.txt");
            AssertFileExistsWithMeta(basePath, "DepY.unitypackage.json");
            AssertFileExistsWithMeta(basePath, "DepY", "FileYv110.txt");
        }

        [Fact]
        private async Task Test_Recursive_NearestWin_ConflictVersion()
        {
            // Arrange:
            // Project -> DepX 1.x -> DepY >=1.0
            //         -> DepY <=0.9
            // - Available DepY: 0.9, 1.0

            foreach (var package in new[] { "DepX.1.0.0", "DepY.0.9.0", "DepY.1.0.0" })
            {
                PackTool.Process(new PackTool.Options
                {
                    ProjectFile = TestHelper.GetDataPath(package + ".json"),
                    OutputDirectory = TestHelper.GetOutputPath()
                });
            }

            // Act

            var restorePath = TestHelper.CreateOutputPath("Restore");
            var e = await Record.ExceptionAsync(() =>
                RestoreTool.Process(new RestoreTool.Options
                {
                    ProjectFile = TestHelper.GetDataPath("ProjectDepX_Y_Conflict.json"),
                    OutputDirectory = restorePath,
                    LocalRepositoryDirectory = TestHelper.GetOutputPath()
                }));

            // Assert

            Assert.NotNull(e);
            Assert.IsType<InvalidDataException>(e);
        }

        [Fact]
        private async Task Test_GithubPackage()
        {
            // Act

            var restorePath = TestHelper.CreateOutputPath("Restore");
            await RestoreTool.Process(new RestoreTool.Options
            {
                ProjectFile = TestHelper.GetDataPath("RestoreGithub.json"),
                OutputDirectory = restorePath
            });

            // Assert

            var basePath = Path.Combine(restorePath, "Assets", "UnityPackages");
            AssertFileExistsWithMeta(basePath, "NetLegacySupport.unitypackage.json");
            AssertFileExistsWithMeta(basePath, "NetLegacySupport", "NetLegacySupport.Action.dll");
            AssertFileExistsWithMeta(basePath, "NetLegacySupport", "NetLegacySupport.Action.dll.mdb");
        }

        [Fact]
        private async Task Test_NugetPackage()
        {
            // Act

            var restorePath = TestHelper.CreateOutputPath("Restore");
            await RestoreTool.Process(new RestoreTool.Options
            {
                ProjectFile = TestHelper.GetDataPath("RestoreNuget.json"),
                OutputDirectory = restorePath
            });

            // Assert

            var basePath = Path.Combine(restorePath, "Assets", "UnityPackages");
            AssertFileExistsWithMeta(basePath, "protobuf-net.unitypackage.json");
            AssertFileExistsWithMeta(basePath, "protobuf-net", "protobuf-net.dll");
            AssertFileExistsWithMeta(basePath, "protobuf-net", "protobuf-net.dll.mdb");
        }

        private void AssertFileExists(params string[] names)
        {
            Assert.True(File.Exists(Path.Combine(names)), "File: " + Path.Combine(names));
        }

        private void AssertFileNotExists(params string[] names)
        {
            Assert.False(File.Exists(Path.Combine(names)), "File: " + Path.Combine(names));
        }

        private void AssertFileExistsWithMeta(params string[] names)
        {
            Assert.True(File.Exists(Path.Combine(names)), "File: " + Path.Combine(names));
            Assert.True(File.Exists(Path.Combine(names) + ".meta"), "File: " + Path.Combine(names) + ".meta");
        }
    }
}
