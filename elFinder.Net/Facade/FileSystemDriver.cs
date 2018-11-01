using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using ElFinder.DTO;
using ElFinder.Response;
using ElFinder.FileSystem;
using System.Text;

namespace ElFinder
{
    /// <summary>
    /// Represents a driver for local file system
    /// </summary>
    public class FileSystemDriver : IDriver
    {
        #region private  
        private const string _volumePrefix = "v";
        private List<Root> _roots;
        private IFileSystemProvider _fileSystemProvider;

        private JsonResult Json(object data)
        {
            return new JsonDataContractResult(data) { JsonRequestBehavior = JsonRequestBehavior.AllowGet, ContentType = "text/html" };
        }
        private void DirectoryCopy(DirectoryMetadata sourceDir, string destDirName, bool copySubDirs)
        {
            var dirs = _fileSystemProvider.GetDirectories(sourceDir.Path);

            // If the source directory does not exist, throw an exception.
            if (!_fileSystemProvider.DirectoryExists(sourceDir.Path))
            {
                throw new DirectoryNotFoundException("Source directory does not exist or could not be found: " + sourceDir.Path);
            }

            // If the destination directory does not exist, create it.
            _fileSystemProvider.CreateDirectoryIfNotExists(destDirName);

            // Get the file contents of the directory to copy.
            var files = _fileSystemProvider.GetFiles(sourceDir.Path);

            foreach (FileMetadata file in files)
            {
                // Create the path to the new copy of the file.
                string temppath = _fileSystemProvider.CombinePath(destDirName, file.Name);

                // Copy the file.
                _fileSystemProvider.CopyFile(file.Path, temppath);
            }

            // If copySubDirs is true, copy the subdirectories.
            if (copySubDirs)
            {
                foreach (DirectoryMetadata subdir in dirs)
                {
                    // Create the subdirectory.
                    string temppath = _fileSystemProvider.CombinePath(destDirName, subdir.Name);

                    // Copy the subdirectories.
                    DirectoryCopy(subdir, temppath, copySubDirs);
                }
            }
        }

        private void RemoveThumbs(FullPath path)
        {
            if (path.Directory != null)
            {
                string thumbPath = path.Root.GetExistingThumbPath(path.Directory);
                if (thumbPath != null)
                    _fileSystemProvider.DeleteDirectory(thumbPath);
            }
            else
            {
                string thumbPath = path.Root.GetExistingThumbPath(path.File);
                if (thumbPath != null)
                    _fileSystemProvider.DeleteFile(thumbPath);
            }
        }

        private void UpdateFileMetadata(FileMetadata metadata, DateTime modifiedDateTime, long size)
        {
            metadata.ModifiedDate = modifiedDateTime.ToUniversalTime();
            metadata.Length = size;
        }

        #endregion

        #region public 

        public FullPath ParsePath(string target)
        {
            string volumePrefix = null;
            string pathHash = null;
            for (int i = 0; i < target.Length; i++)
            {
                if (target[i] == '_')
                {
                    pathHash = target.Substring(i + 1);
                    volumePrefix = target.Substring(0, i + 1);
                    break;
                }
            }
            Root root = _roots.First(r => r.VolumeId == volumePrefix);
            string path = Helper.DecodePath(pathHash);
            string dirUrl = path != root.Directory.Name ? path : string.Empty;
            if (dirUrl.Contains("../") || dirUrl.Contains("..\\"))
            {
                // Prevents "Path Traversal" attack.
                throw new NotSupportedException($"Path format is not supported. Path: '{dirUrl}'.");
            }
            var fullPath = _fileSystemProvider.CombinePath(root.Directory.Path, dirUrl);
            if (!fullPath.StartsWith(root.Directory.Path, StringComparison.OrdinalIgnoreCase))
            {
                // Prevents "Path Traversal" attack.
                throw new NotSupportedException($"Invalid path. Path: '{dirUrl}'.");
            }
            bool looksLikeDirectory = fullPath.EndsWith(Path.DirectorySeparatorChar.ToString()) 
                || fullPath.EndsWith(Path.AltDirectorySeparatorChar.ToString());

            if (_fileSystemProvider.DirectoryExists(fullPath) || looksLikeDirectory)
            {
                var dir = new DirectoryMetadata(fullPath);
                return new FullPath(root, dir);
            }
            else
            {
                var file = _fileSystemProvider.GetFileMetadata(fullPath);
                return new FullPath(root, file);
            }
        }

        /// <summary>
        /// Initialize new instance of class ElFinder.FileSystemDriver 
        /// </summary>
        public FileSystemDriver()
            : this(new FileSystemProvider())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileSystemDriver"/> class.
        /// </summary>
        /// <param name="fileSystemProvider">The file system provider.</param>
        /// <exception cref="ArgumentNullException">fileSystemManager;fileSystemManager cannot be null.</exception>
        public FileSystemDriver(IFileSystemProvider fileSystemProvider)
        {
            if (fileSystemProvider == null)
                throw new ArgumentNullException("fileSystemProvider", "fileSystemProvider cannot be null.");

            _roots = new List<Root>();
            _fileSystemProvider = fileSystemProvider;
        }

        /// <summary>
        /// Adds an object to the end of the roots.
        /// </summary>
        /// <param name="item"></param>
        public void AddRoot(Root item)
        {
            item.Initialize(_fileSystemProvider);
            _roots.Add(item);
            item.VolumeId = _volumePrefix + _roots.Count + "_";
        }

        /// <summary>
        /// Gets collection of roots
        /// </summary>
        public IEnumerable<Root> Roots { get { return _roots; } }
        #endregion public

        #region   IDriver
        JsonResult IDriver.Open(string target, bool tree)
        {
            FullPath fullPath = ParsePath(target);
            OpenResponse answer = new OpenResponse(DTOBase.Create(fullPath.Directory, fullPath.Root), fullPath);
            foreach (FileMetadata item in _fileSystemProvider.GetFiles(fullPath.Directory.Path, visibleOnly: true))
            {
                answer.Files.Add(DTOBase.Create(item, fullPath.Root));
            }
            foreach (DirectoryMetadata item in _fileSystemProvider.GetDirectories(fullPath.Directory.Path, visibleOnly: true))
            {
                answer.Files.Add(DTOBase.Create(item, fullPath.Root));
            }
            return Json(answer);
        }
        JsonResult IDriver.Init(string target)
        {
            FullPath fullPath;
            if (string.IsNullOrEmpty(target))
            {
                Root root = _roots.FirstOrDefault(r => r.StartPath != null);
                if (root == null)
                    root = _roots.First();
                fullPath = new FullPath(root, root.StartPath ?? root.Directory);
            }
            else
            {
                fullPath = ParsePath(target);
            }
            InitResponse answer = new InitResponse(DTOBase.Create(fullPath.Directory, fullPath.Root), new Options(fullPath));

            foreach (FileMetadata item in _fileSystemProvider.GetFiles(fullPath.Directory.Path, visibleOnly: true))
            {
                answer.Files.Add(DTOBase.Create(item, fullPath.Root));
            }
            foreach (DirectoryMetadata item in _fileSystemProvider.GetDirectories(fullPath.Directory.Path, visibleOnly: true))
            {
                answer.Files.Add(DTOBase.Create(item, fullPath.Root));
            }
            foreach (Root item in _roots)
            {
                answer.Files.Add(DTOBase.Create(item.Directory, item));
            }
            if (fullPath.Root.Directory.Path != fullPath.Directory.Path)
            {
                foreach (DirectoryMetadata item in _fileSystemProvider.GetDirectories(fullPath.Root.Directory.Path, visibleOnly: true))
                {
                    answer.Files.Add(DTOBase.Create(item, fullPath.Root));
                }
            }
            if (fullPath.Root.MaxUploadSize.HasValue)
            {
                answer.UploadMaxSize = fullPath.Root.MaxUploadSizeInKb.Value + "K";
            }
            return Json(answer);
        }
        ActionResult IDriver.File(string target, bool download)
        {
            FullPath fullPath = ParsePath(target);
            if (fullPath.IsDirectoty)
                return new HttpStatusCodeResult(403, "You can not download whole folder");
            if (!_fileSystemProvider.FileExists(fullPath.File.Path))
                return new HttpNotFoundResult("File not found");
            if (fullPath.Root.IsShowOnly)
                return new HttpStatusCodeResult(403, "Access denied. Volume is for show only");
            return new DownloadFileResult(fullPath.File, download, _fileSystemProvider);
        }
        JsonResult IDriver.Parents(string target)
        {
            FullPath fullPath = ParsePath(target);
            TreeResponse answer = new TreeResponse();
            if (fullPath.Directory.Path == fullPath.Root.Directory.Path)
            {
                answer.Tree.Add(DTOBase.Create(fullPath.Directory, fullPath.Root));
            }
            else
            {
                DirectoryMetadata parent = fullPath.Directory;
                foreach (var item in _fileSystemProvider.GetDirectories(parent.Parent.Path))
                {
                    answer.Tree.Add(DTOBase.Create(item, fullPath.Root));
                }
                while (parent.Path != fullPath.Root.Directory.Path)
                {
                    parent = parent.Parent;
                    answer.Tree.Add(DTOBase.Create(parent, fullPath.Root));
                }
            }
            return Json(answer);
        }
        JsonResult IDriver.Tree(string target)
        {
            FullPath fullPath = ParsePath(target);
            TreeResponse answer = new TreeResponse();
            foreach (var item in _fileSystemProvider.GetDirectories(fullPath.Directory.Path, visibleOnly: true))
            {
                answer.Tree.Add(DTOBase.Create(item, fullPath.Root));
            }
            return Json(answer);
        }
        JsonResult IDriver.List(string target)
        {
            FullPath fullPath = ParsePath(target);
            ListResponse answer = new ListResponse();
            foreach (var item in _fileSystemProvider.GetFileSystemMetadataItems(fullPath.Directory.Path))
            {
                answer.List.Add(item.Name);
            }
            return Json(answer);
        }
        JsonResult IDriver.MakeDir(string target, string name)
        {
            FullPath fullPath = ParsePath(target);
            DirectoryMetadata newDir = new DirectoryMetadata(_fileSystemProvider.CombinePath(fullPath.Directory.Path, name));
            _fileSystemProvider.CreateDirectoryIfNotExists(newDir.Path);
            return Json(new AddResponse(newDir, fullPath.Root));
        }
        JsonResult IDriver.MakeFile(string target, string name)
        {
            FullPath fullPath = ParsePath(target);
            FileMetadata newFile = new FileMetadata(_fileSystemProvider.CombinePath(fullPath.Directory.Path, name));
            var stream = _fileSystemProvider.OpenWrite(newFile.Path);
            stream.Close();

            return Json(new AddResponse(newFile, fullPath.Root));
        }
        JsonResult IDriver.Rename(string target, string name)
        {
            FullPath fullPath = ParsePath(target);
            var answer = new ReplaceResponse();
            answer.Removed.Add(target);
            RemoveThumbs(fullPath);
            if (fullPath.Directory != null)
            {
                string newPath = _fileSystemProvider.CombinePath(fullPath.Directory.Parent.Path, name);
                _fileSystemProvider.MoveDirectory(fullPath.Directory.Path, newPath);
                answer.Added.Add(DTOBase.Create(new DirectoryMetadata(newPath), fullPath.Root));
            }
            else
            {
                string newPath = _fileSystemProvider.CombinePath(fullPath.File.Directory.Path, name);
                _fileSystemProvider.MoveFile(fullPath.File.Path, newPath);
                answer.Added.Add(DTOBase.Create(_fileSystemProvider.GetFileMetadata(newPath), fullPath.Root));
            }
            return Json(answer);
        }
        JsonResult IDriver.Remove(IEnumerable<string> targets)
        {
            RemoveResponse answer = new RemoveResponse();
            foreach (string item in targets)
            {
                FullPath fullPath = ParsePath(item);
                RemoveThumbs(fullPath);
                if (fullPath.Directory != null)
                {
                    _fileSystemProvider.DeleteDirectory(fullPath.Directory.Path);
                }
                else
                {
                    _fileSystemProvider.DeleteFile(fullPath.File.Path);
                }
                answer.Removed.Add(item);
            }
            return Json(answer);
        }
        JsonResult IDriver.Get(string target)
        {
            FullPath fullPath = ParsePath(target);
            GetResponse answer = new GetResponse();
            using (StreamReader reader = new StreamReader(_fileSystemProvider.OpenRead(fullPath.File.Path)))
            {
                answer.Content = reader.ReadToEnd();
            }
            return Json(answer);
        }
        JsonResult IDriver.Put(string target, string content)
        {
            FullPath fullPath = ParsePath(target);
            ChangedResponse answer = new ChangedResponse();

            using (var output = _fileSystemProvider.OpenWrite(fullPath.File.Path))
            {
                output.Write(Encoding.UTF8.GetBytes(content), 0, content.Length);
            }

            UpdateFileMetadata(fullPath.File, modifiedDateTime: DateTime.Now, size: content.Length);

            answer.Changed.Add((FileDTO)DTOBase.Create(fullPath.File, fullPath.Root));
            return Json(answer);
        }
        JsonResult IDriver.Paste(string source, string dest, IEnumerable<string> targets, bool isCut)
        {
            FullPath destPath = ParsePath(dest);
            ReplaceResponse response = new ReplaceResponse();
            foreach (var item in targets)
            {
                FullPath src = ParsePath(item);
                if (src.Directory != null)
                {
                    DirectoryMetadata newDir = new DirectoryMetadata(_fileSystemProvider.CombinePath(destPath.Directory.Path, src.Directory.Name));
                    if (_fileSystemProvider.DirectoryExists(newDir.Path))
                        _fileSystemProvider.DeleteDirectory(newDir.Path);
                    if (isCut)
                    {
                        RemoveThumbs(src);
                        _fileSystemProvider.MoveDirectory(src.Directory.Path, newDir.Path);
                        response.Removed.Add(item);
                    }
                    else
                    {
                        DirectoryCopy(src.Directory, newDir.Path, true);
                    }
                    response.Added.Add(DTOBase.Create(newDir, destPath.Root));
                }
                else
                {
                    string newFilePath = _fileSystemProvider.CombinePath(destPath.Directory.Path, src.File.Name);
                    if (_fileSystemProvider.FileExists(newFilePath))
                        _fileSystemProvider.DeleteFile(newFilePath);
                    if (isCut)
                    {
                        RemoveThumbs(src);
                        _fileSystemProvider.MoveFile(src.File.Path, newFilePath);
                        response.Removed.Add(item);
                    }
                    else
                    {
                        _fileSystemProvider.CopyFile(src.File.Path, newFilePath);
                    }
                    response.Added.Add(DTOBase.Create(new FileMetadata(newFilePath), destPath.Root));
                }
            }
            return Json(response);
        }
        JsonResult IDriver.Upload(string target, System.Web.HttpFileCollectionBase targets)
        {
            FullPath dest = ParsePath(target);
            var response = new AddResponse();
            if (dest.Root.MaxUploadSize.HasValue)
            {
                for (int i = 0; i < targets.AllKeys.Length; i++)
                {
                    HttpPostedFileBase file = targets[i];
                    if (file.ContentLength > dest.Root.MaxUploadSize.Value)
                    {
                        return Error.MaxUploadFileSize();
                    }
                }
            }
            for (int i = 0; i < targets.AllKeys.Length; i++)
            {
                HttpPostedFileBase file = targets[i];
                FileMetadata path = new FileMetadata(_fileSystemProvider.CombinePath(dest.Directory.Path, Path.GetFileName(file.FileName)));

                if (_fileSystemProvider.FileExists(path.Path))
                {
                    if (dest.Root.UploadOverwrite)
                    {
                        //if file already exist we rename the current file, 
                        //and if upload is succesfully delete temp file, in otherwise we restore old file
                        string tmpPath = path.Path + Guid.NewGuid();
                        bool uploaded = false;
                        try
                        {
                            SaveStream(file.InputStream, tmpPath);
                            uploaded = true;
                        }
                        catch { }
                        finally
                        {
                            if (uploaded)
                            {
                                _fileSystemProvider.DeleteFile(path.Path);
                                _fileSystemProvider.MoveFile(tmpPath, path.Path);
                            }
                            else
                            {
                                _fileSystemProvider.DeleteFile(tmpPath);
                            }
                        }
                    }
                    else
                    {
                        SaveStream(file.InputStream, _fileSystemProvider.CombinePath(path.Directory.Name, Helper.GetDuplicatedName(path)));
                    }
                }
                else
                {
                    SaveStream(file.InputStream, path.Path);
                }
                response.Added.Add((FileDTO)DTOBase.Create(new FileMetadata(path.Path), dest.Root));
            }
            return Json(response);
        }

        private void SaveStream(Stream stream, string destinationPath)
        {
            using (var output = _fileSystemProvider.OpenWrite(destinationPath))
            {
                if (stream.CanSeek)
                    stream.Position = 0;
                stream.CopyTo(output);
                output.Flush();
            }
        }

        JsonResult IDriver.Duplicate(IEnumerable<string> targets)
        {
            AddResponse response = new AddResponse();
            foreach (var target in targets)
            {
                FullPath fullPath = ParsePath(target);
                if (fullPath.Directory != null)
                {
                    var parentPath = fullPath.Directory.Parent.Path;
                    var name = fullPath.Directory.Name;
                    var newName = _fileSystemProvider.CombinePath(parentPath, name + " copy");
                    if (!_fileSystemProvider.DirectoryExists(newName))
                    {
                        DirectoryCopy(fullPath.Directory, newName, true);
                    }
                    else
                    {
                        for (int i = 1; i < 100; i++)
                        {
                            var dirCopyName = string.Format("{0} copy {1}", name, i);
                            newName = _fileSystemProvider.CombinePath(parentPath, dirCopyName);
                            if (!_fileSystemProvider.DirectoryExists(newName))
                            {
                                DirectoryCopy(fullPath.Directory, newName, true);
                                break;
                            }
                        }
                    }
                    response.Added.Add(DTOBase.Create(new DirectoryMetadata(newName), fullPath.Root));
                }
                else
                {
                    var parentPath = fullPath.File.Directory.Path;
                    var name = fullPath.File.Name.Substring(0, fullPath.File.Name.Length - fullPath.File.Extension.Length);
                    var ext = fullPath.File.Extension;

                    var newName = _fileSystemProvider.CombinePath(parentPath, name + " copy" + ext);

                    if (!_fileSystemProvider.FileExists(newName))
                    {
                        _fileSystemProvider.CopyFile(fullPath.File.Path, newName);
                    }
                    else
                    {
                        for (int i = 1; i < 100; i++)
                        {
                            var fileCopyName = string.Format("{0} copy {1}{2}", name, i, ext);
                            newName = _fileSystemProvider.CombinePath(parentPath, fileCopyName);
                            if (!_fileSystemProvider.FileExists(newName))
                            {
                                _fileSystemProvider.CopyFile(fullPath.File.Path, newName);
                                break;
                            }
                        }
                    }

                    var copiedFile = _fileSystemProvider.GetFileMetadata(newName);
                    UpdateFileMetadata(copiedFile, modifiedDateTime: DateTime.Now, size: fullPath.File.Length);
                    response.Added.Add(DTOBase.Create(copiedFile, fullPath.Root));
                }
            }
            return Json(response);
        }
        JsonResult IDriver.Thumbs(IEnumerable<string> targets)
        {
            ThumbsResponse response = new ThumbsResponse();
            foreach (string target in targets)
            {
                FullPath path = ParsePath(target);
                response.Images.Add(target, path.Root.GenerateThumbHash(path.File));
            }
            return Json(response);
        }
        JsonResult IDriver.Dim(string target)
        {
            FullPath path = ParsePath(target);
            DimResponse response = new DimResponse(path.Root.GetImageDimension(path.File));
            return Json(response);
        }
        JsonResult IDriver.Resize(string target, int width, int height)
        {
            FullPath path = ParsePath(target);
            RemoveThumbs(path);
            path.Root.PicturesEditor.Resize(path.File.Path, width, height);
            var output = new ChangedResponse();
            output.Changed.Add((FileDTO)DTOBase.Create(path.File, path.Root));
            return Json(output);
        }
        JsonResult IDriver.Crop(string target, int x, int y, int width, int height)
        {
            FullPath path = ParsePath(target);
            RemoveThumbs(path);
            path.Root.PicturesEditor.Crop(path.File.Path, x, y, width, height);
            var output = new ChangedResponse();
            output.Changed.Add((FileDTO)DTOBase.Create(path.File, path.Root));
            return Json(output);
        }
        JsonResult IDriver.Rotate(string target, int degree)
        {
            FullPath path = ParsePath(target);
            RemoveThumbs(path);
            path.Root.PicturesEditor.Rotate(path.File.Path, degree);
            var output = new ChangedResponse();
            output.Changed.Add((FileDTO)DTOBase.Create(path.File, path.Root));
            return Json(output);
        }

        #endregion IDriver
    }
}