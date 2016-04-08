using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;

namespace UniGet
{
    internal sealed class Packer : IDisposable
    {
        private string _fileName;
        private TarOutputStream _stream;
        private HashSet<string> _addedPathSet = new HashSet<string>();

        public Packer(string fileName)
        {
            _fileName = fileName;

            var fileStream = File.Create(fileName);

            var gzipStream = new GZipOutputStream(fileStream);
            gzipStream.SetLevel(9);

            _stream = new TarOutputStream(gzipStream);
        }

        public void Dispose()
        {
            if (_stream != null)
            {
                _stream.Close();
                _stream = null;
            }
        }

        public bool Add(string sourcePath, string targetPath)
        {
            var targetPathNormalized = targetPath.Replace("\\", "/").Trim('/');
            if (_addedPathSet.Contains(targetPathNormalized))
                return false;

            // guid/asset       : #Assets/Folder/File.dll
            //     /asset.meta  : #Assets/Folder/File.dll.meta
            //     /pathname    : Assets/Folder/File.dll

            var sourceMetaPath = sourcePath + ".meta";

            var guid = File.ReadAllLines(sourceMetaPath)
                           .Where(s => s.StartsWith("guid:"))
                           .Select(s => s.Split()[1])
                           .FirstOrDefault();
            if (guid == null)
                throw new ArgumentException("Cannot extact guid from " + sourceMetaPath);

            // asset

            if (File.Exists(sourcePath))
            {
                var asset = TarEntry.CreateTarEntry(guid + "/asset");
                var assetFile = File.ReadAllBytes(sourcePath);
                asset.Size = assetFile.Length;
                asset.ModTime = File.GetLastWriteTime(sourcePath);
                _stream.PutNextEntry(asset);
                _stream.Write(assetFile, 0, assetFile.Length);
                _stream.CloseEntry();
            }

            // asset.meta
            {
                var meta = TarEntry.CreateTarEntry(guid + "/asset.meta");
                var metaFile = File.ReadAllBytes(sourceMetaPath);
                meta.Size = metaFile.Length;
                meta.ModTime = File.GetLastWriteTime(sourceMetaPath);
                _stream.PutNextEntry(meta);
                _stream.Write(metaFile, 0, metaFile.Length);
                _stream.CloseEntry();
            }

            // pathname
            {
                var pathname = TarEntry.CreateTarEntry(guid + "/pathname");
                var pathnameFile = Encoding.UTF8.GetBytes(targetPathNormalized);
                pathname.Size = pathnameFile.Length;
                pathname.ModTime = File.GetLastWriteTime(sourcePath);
                _stream.PutNextEntry(pathname);
                _stream.Write(pathnameFile, 0, pathnameFile.Length);
                _stream.CloseEntry();
            }

            _addedPathSet.Add(sourcePath);
            return true;
        }

        public bool AddWithMetaGenerated(string sourcePath, string targetPath)
        {
            var targetPathNormalized = targetPath.Replace("\\", "/").Trim('/');
            if (_addedPathSet.Contains(targetPathNormalized))
                return false;

            // guid/asset       : #Assets/Folder/File.dll
            //     /asset.meta  : #Assets/Folder/File.dll.meta
            //     /pathname    : Assets/Folder/File.dll

            var metaData = GenerateMeta(sourcePath, targetPath);
            var guid = metaData.Item1;

            // asset

            if (File.Exists(sourcePath))
            {
                var asset = TarEntry.CreateTarEntry(guid + "/asset");
                var assetFile = File.ReadAllBytes(sourcePath);
                asset.Size = assetFile.Length;
                asset.ModTime = File.GetLastWriteTime(sourcePath);
                _stream.PutNextEntry(asset);
                _stream.Write(assetFile, 0, assetFile.Length);
                _stream.CloseEntry();
            }

            // asset.meta
            {
                var meta = TarEntry.CreateTarEntry(guid + "/asset.meta");
                var metaFile = metaData.Item2;
                meta.Size = metaFile.Length;
                meta.ModTime = File.GetLastWriteTime(sourcePath);
                _stream.PutNextEntry(meta);
                _stream.Write(metaFile, 0, metaFile.Length);
                _stream.CloseEntry();
            }

            // pathname
            {
                var pathname = TarEntry.CreateTarEntry(guid + "/pathname");
                var pathnameFile = Encoding.UTF8.GetBytes(targetPathNormalized);
                pathname.Size = pathnameFile.Length;
                pathname.ModTime = File.GetLastWriteTime(sourcePath);
                _stream.PutNextEntry(pathname);
                _stream.Write(pathnameFile, 0, pathnameFile.Length);
                _stream.CloseEntry();
            }

            _addedPathSet.Add(sourcePath);
            return true;
        }

        public void AddDirectoriesWithMetaGenerated(string targetPath)
        {
            while (true)
            {
                var parentPath = Path.GetDirectoryName(targetPath);
                if (string.IsNullOrEmpty(parentPath))
                    return;

                AddWithMetaGenerated(".", targetPath);
                targetPath = parentPath;
            }
        }

        private Tuple<string, byte[]> GenerateMeta(string sourcePath, string targetPath)
        {
            var guid = GenerateGuid(targetPath);
            if (File.Exists(sourcePath))
            {
                var ext = Path.GetExtension(sourcePath).ToLower();
                switch (ext)
                {
                    case ".dll":
                        return Tuple.Create(
                            guid,
                            Encoding.UTF8.GetBytes(string.Join("\n", new[]
                            {
                                "fileFormatVersion: 2",
                                "guid: " + guid,
                                "MonoAssemblyImporter:",
                                "  serializedVersion: 1",
                                "  iconMap: {}",
                                "  executionOrder: {}",
                                "  userData: "
                            })));

                    case ".mdb":
                        return Tuple.Create(
                            guid,
                            Encoding.UTF8.GetBytes(string.Join("\n", new[]
                            {
                                "fileFormatVersion: 2",
                                "guid: " + guid,
                                "DefaultImporter:",
                                "  serializedVersion: 1",
                                "  iconMap: {}",
                                "  executionOrder: {}",
                                "  userData: "
                            })));

                    default:
                        throw new InvalidOperationException("Cannot generate meta from " + sourcePath);
                }
            }
            else
            {
                return Tuple.Create(
                    guid,
                    Encoding.UTF8.GetBytes(string.Join("\n", new[]
                    {
                        "fileFormatVersion: 2",
                        "guid: " + guid,
                        "folderAsset: yes",
                        "DefaultImporter:",
                        "  userData: ",
                    })));
            }
        }

        private string GenerateGuid(string path)
        {
            var ns = new Guid("421A66D3-14FD-41FD-B965-7BA5B510DAB9");
            return GuidUtility.Create(ns, path).ToString("N").ToLower();
        }
    }
}
