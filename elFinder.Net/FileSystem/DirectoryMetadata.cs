using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ElFinder.FileSystem
{
    public class DirectoryMetadata: IFileSystemMetadata
    {
        DirectoryMetadata _parent;
        char _separator;

        public DirectoryMetadata(string path)
        {
            _separator = path.IndexOf(System.IO.Path.DirectorySeparatorChar) > -1 
                ? System.IO.Path.DirectorySeparatorChar 
                : System.IO.Path.AltDirectorySeparatorChar;

            Path = path.TrimEnd(_separator) + _separator;
            
            if (Path.Length > 1)
            {
                var parentPath = GetParentPath(Path);
                _parent = new DirectoryMetadata(parentPath);
            }
        }

        /// <summary>
        /// Gets the name of the directory.
        /// </summary>
        public string Name
        {
            get { return System.IO.Path.GetFileName(Path.TrimEnd(_separator)); }
        }

        /// <summary>
        /// Gets or sets the full path of the directory.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets the last modified date of the file.
        /// </summary>
        public DateTime ModifiedDate { get; set; }

        public DirectoryMetadata Parent
        {
            get { return _parent; }
        }

        public string GetRelativePath(string rootPath)
        {
            return Helper.GetRelativePath(this.Path, rootPath);
        }

        private string GetParentPath(string path)
        {
            var indexOf = path.TrimEnd(_separator).LastIndexOf(_separator);
            if (indexOf > -1)
                return path.Substring(0, indexOf);

            return _separator.ToString();
        }
    }
}
