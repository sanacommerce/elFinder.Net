using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ElFinder.FileSystem
{
    public class FileSystemProvider : IFileSystemProvider
    {
        public FileMetadata GetFileMetadata(string filePath)
        {
            var result = new FileMetadata(filePath);
            var fileInfo = new FileInfo(filePath);
            if (fileInfo.Exists)
            {
                result.ModifiedDate = fileInfo.LastWriteTime;
                result.Length = fileInfo.Length;
            }

            return result;
        }

        public bool DirectoryExists(string path)
        {
            return Directory.Exists(path);
        }

        public bool FileExists(string path)
        {
            return File.Exists(path);
        }

        public void CreateDirectoryIfNotExists(string directoryPath)
        {
            if (Directory.Exists(directoryPath))
                return;

            Directory.CreateDirectory(directoryPath);
        }

        public IList<DirectoryMetadata> GetDirectories(string path, bool visibleOnly = false)
        {
            var dirInfo = new DirectoryInfo(path);
            var directories = dirInfo.GetDirectories();
            if (visibleOnly)
            {
                return directories.Where(FileSystemInfoVisible).Select(CreateDirectoryMetadata).ToList();
            }

            return directories.Select(CreateDirectoryMetadata).ToList();
        }

        public IList<FileMetadata> GetFiles(string path, bool visibleOnly = false)
        {
            var dirInfo = new DirectoryInfo(path);
            var files = dirInfo.GetFiles();
            if (visibleOnly)
            {
                return files.Where(FileSystemInfoVisible).Select(CreateFileMetadata).ToList();
            }

            return files.Select(CreateFileMetadata).ToList();
        }

        public IList<IFileSystemMetadata> GetFileSystemMetadataItems(string directoryPath)
        {
            var result = new List<IFileSystemMetadata>();
            var dirInfo = new DirectoryInfo(directoryPath);
            var infos = dirInfo.GetFileSystemInfos();
            foreach (var info in infos)
            {
                if (info is DirectoryInfo)
                {
                    result.Add(CreateDirectoryMetadata((DirectoryInfo)info));
                }
                else
                {
                    result.Add(CreateFileMetadata((FileInfo)info));
                }
            }

            return result;
        }

        public string CombinePath(params string[] paths)
        {
            return Path.Combine(paths);
        }

        public void CopyFile(string sourceFilePath, string destinationFilePath)
        {
            var fileInfo = new FileInfo(sourceFilePath);
            fileInfo.CopyTo(destinationFilePath);
        }

        public void CopyDirectory(string sourceDirectoryPath, string destinationDirectoryPath)
        {
            foreach (string dirPath in Directory.GetDirectories(sourceDirectoryPath, "*", SearchOption.AllDirectories))
                Directory.CreateDirectory(dirPath.Replace(sourceDirectoryPath, destinationDirectoryPath));

            foreach (string newPath in Directory.GetFiles(sourceDirectoryPath, "*.*", SearchOption.AllDirectories))
                File.Copy(newPath, newPath.Replace(sourceDirectoryPath, destinationDirectoryPath), true);
        }

        public void DeleteDirectory(string path)
        {
            Directory.Delete(path, true);
        }

        public void DeleteFile(string path)
        {
            File.Delete(path);
        }

        public virtual Stream OpenWrite(string name)
        {
            return new FileInfo(name).OpenWrite();
        }

        public virtual Stream OpenRead(string name)
        {
            return new FileInfo(name).OpenRead();
        }

        public void MoveFile(string sourcePath, string destinationPath)
        {
            File.Move(sourcePath, destinationPath);
        }

        public void MoveDirectory(string sourcePath, string destinationPath)
        {
            Directory.Move(sourcePath, destinationPath);
        }

        protected bool FileSystemInfoVisible(FileSystemInfo info)
        {
            return (info.Attributes & FileAttributes.Hidden) != FileAttributes.Hidden;
        }

        protected FileMetadata CreateFileMetadata(FileInfo info)
        {
            return new FileMetadata(info.FullName)
            {
                Length = info.Length,
                ModifiedDate = info.LastWriteTime
            };
        }

        protected DirectoryMetadata CreateDirectoryMetadata(DirectoryInfo info)
        {
            return new DirectoryMetadata(info.FullName)
            {
                ModifiedDate = info.LastWriteTime
            };
        }
    }
}
