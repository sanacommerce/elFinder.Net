using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ElFinder.FileSystem
{
    /// <summary>
    /// Manager provides functionality to work with files and directories.
    /// </summary>
    public interface IFileSystemProvider
    {
        FileMetadata GetFileMetadata(string filePath);

        IList<DirectoryMetadata> GetDirectories(string path, bool visibleOnly = false);

        IList<FileMetadata> GetFiles(string path, bool visibleOnly = false);

        IList<IFileSystemMetadata> GetFileSystemMetadataItems(string directoryPath);

        bool DirectoryExists(string path);

        bool FileExists(string path);

        string CombinePath(params string[] paths);

        void CopyFile(string sourceFilePath, string destinationFilePath);

        void CopyDirectory(string sourceDirectoryPath, string destinationDirectoryPath);

        void DeleteDirectory(string path);

        void CreateDirectoryIfNotExists(string directoryPath);

        void DeleteFile(string path);

        Stream OpenRead(string path);

        Stream OpenWrite(string path);

        void MoveFile(string sourcePath, string destinationPath);

        void MoveDirectory(string sourcePath, string destinationPath);
    }
}
