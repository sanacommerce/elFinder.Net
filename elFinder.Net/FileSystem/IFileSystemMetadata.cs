using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ElFinder.FileSystem
{
    public interface IFileSystemMetadata
    {
        string Name { get; }

        /// <summary>
        /// Gets or sets the full path of the file or directory.
        /// </summary>
        string Path { get; }

        string GetRelativePath(string rootPath);

        DateTime ModifiedDate { get; set; }
    }
}
