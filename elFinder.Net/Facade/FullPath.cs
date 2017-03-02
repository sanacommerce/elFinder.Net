using System.IO;
using System;
using ElFinder.FileSystem;

namespace ElFinder
{
    public class FullPath
    {
        public Root Root
        {
            get { return _root; }
        }
        public bool IsDirectoty
        {
            get { return _isDirectory; }
        }
        public string RelativePath
        {
            get
            {
                return _relativePath;
            }
        }
        public DirectoryMetadata Directory
        {
            get
            {
                return _isDirectory ? (DirectoryMetadata)_fileSystemObject : null;
            }
        }
        public FileMetadata File
        {
            get
            {
                return !_isDirectory ? (FileMetadata)_fileSystemObject : null;
            }
        }
        public FullPath(Root root, IFileSystemMetadata fileSystemObject)
        {
            if (root == null)
                throw new ArgumentNullException("root", "Root can not be null");
            if (fileSystemObject == null)
                throw new ArgumentNullException("root", "Filesystem object can not be null");
            _root = root;
            _fileSystemObject = fileSystemObject;
            _isDirectory = _fileSystemObject is DirectoryMetadata;
            _relativePath = fileSystemObject.GetRelativePath(root.Directory.Path);
        }

        private Root _root;
        private IFileSystemMetadata _fileSystemObject;
        private bool _isDirectory;
        private string _relativePath;
    }
}