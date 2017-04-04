using System;
using ElFinder.FileSystem;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace ElFinder
{
    internal class DownloadFileResult : ActionResult
    {
        public FileMetadata File { get; private set; }
        public bool IsDownload { get; private set; }

        private IFileSystemProvider _fileSystemProvider;

        public DownloadFileResult(FileMetadata file, bool isDownload, IFileSystemProvider fileSystemProvider)
        {
            File = file;
            IsDownload = isDownload;
            _fileSystemProvider = fileSystemProvider;
        }

        public override void ExecuteResult(ControllerContext context)
        {
            HttpResponseBase response = context.HttpContext.Response;
            HttpRequestBase request = context.HttpContext.Request;

            if (!HttpCacheHelper.IsFileFromCache(File, request, response))
            {
                var mime = GetMimeType(File);
                response.Clear();
                response.StatusCode = (int)HttpStatusCode.OK;
                response.AppendHeader("Content-Length", File.Length.ToString());
                response.ContentType = mime;
                response.AddHeader("Last-Modified", File.ModifiedDate.ToString("r"));
                response.AddHeader("ETag", "\"" + GetEntityTag(File) + "\"");
                response.AddHeader("Accept-Ranges", "none");
                response.AppendHeader("Content-Disposition", GetContentDesposition(request, mime));
                response.AppendHeader("Content-Location", File.Name);
                response.AppendHeader("Content-Transfer-Encoding", "binary");
                response.Cache.SetLastModified(File.ModifiedDate);

                WriteFileToResponse(response, File);

                if (response.IsClientConnected)
                    response.Flush();
            }
            else
            {
                response.ContentType = IsDownload ? "application/octet-stream" : Helper.GetMimeType(File);
            }

            context.HttpContext.ApplicationInstance.CompleteRequest();
        }

        private void WriteFileToResponse(HttpResponseBase response, FileMetadata file)
        {
            response.ClearContent();
            using (var stream = _fileSystemProvider.OpenRead(file.Path))
            {
                stream.CopyTo(response.OutputStream);
            }
        }

        private string GetEntityTag(FileMetadata file)
        {
            ASCIIEncoding ascii = new ASCIIEncoding();
            byte[] sourceBytes = ascii.GetBytes(string.Concat(file.Path, "|", file.ModifiedDate.ToFileTimeUtc()));
            return Convert.ToBase64String(new MD5CryptoServiceProvider().ComputeHash(sourceBytes));
        }

        private string GetMimeType(FileMetadata file)
        {
            return IsDownload ? "application/octet-stream" : Helper.GetMimeType(file);
        }

        private string GetContentDesposition(HttpRequestBase request, string mime)
        {
            string fileName;
            string fileNameEncoded = HttpUtility.UrlEncode(File.Name);

            if (request.UserAgent.Contains("MSIE")) // IE < 9 do not support RFC 6266 (RFC 2231/RFC 5987)
                fileName = "filename=\"" + fileNameEncoded + "\"";
            else
                fileName = "filename*=UTF-8\'\'" + fileNameEncoded; // RFC 6266 (RFC 2231/RFC 5987)

            return (!IsDownload && IsInlineType(mime) ? "inline;" : "attachment;") + fileName;
        }

        private bool IsInlineType(string mime)
        {
            return mime.Contains("image") || mime.Contains("text") || mime == "application/x-shockwave-flash";
        }
    }
}
