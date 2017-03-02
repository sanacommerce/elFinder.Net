using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ElFinder.FileSystem
{
    /// <summary>
    /// Represents information about file.
    /// </summary>
    public class FileMetadata : IFileSystemMetadata
    {
        char _separator;

        public FileMetadata(string path)
        {
            this.Path = path;
            this.ModifiedDate = DateTime.Now.ToUniversalTime();
            this.Length = 0;

            _separator = path.IndexOf(System.IO.Path.DirectorySeparatorChar) > -1
                ? System.IO.Path.DirectorySeparatorChar
                : System.IO.Path.AltDirectorySeparatorChar;

            var dirPath = System.IO.Path.GetDirectoryName(this.Path)
                    .Replace(System.IO.Path.DirectorySeparatorChar, _separator)
                    .Replace(System.IO.Path.AltDirectorySeparatorChar, _separator) + _separator;
            Directory = new DirectoryMetadata(dirPath);
        }

        /// <summary>
        /// Gets the name of the file.
        /// </summary>
        public string Name
        {
            get { return System.IO.Path.GetFileName(this.Path); }
        }

        /// <summary>
        /// Gets or sets the full path of the file.
        /// </summary>
        public string Path { get; private set; }

        public string GetRelativePath(string rootPath)
        {
            return Helper.GetRelativePath(this.Path, rootPath);
        }

        public DirectoryMetadata Directory { get; private set; }

        public string Extension
        {
            get { return System.IO.Path.GetExtension(this.Path); }
        }

        /// <summary>
        /// Gets or sets the last modified date of the file.
        /// </summary>
        public DateTime ModifiedDate { get; set; }

        public long Length { get; set; }
    }
}
