using ElFinder;
using System.Web.Mvc;
using System.Collections.Generic;
using ElFinder.FileSystem;

namespace elFinder.Net.Web.Controllers
{
    public partial class FileController : Controller
    {
        public virtual ActionResult Index(string folder, string subFolder)
        {
            FileSystemDriver driver = new FileSystemDriver(new FileSystemProvider());
            var root = new Root(
                    new DirectoryMetadata(Server.MapPath("~/Files/" + folder)),
                    "http://" + Request.Url.Authority + "/Files/" + folder)
            {
                // Sample using ASP.NET built in Membership functionality...
                // Only the super user can READ (download files) & WRITE (create folders/files/upload files).
                // Other users can only READ (download files)
                // IsReadOnly = !User.IsInRole(AccountController.SuperUser)

                IsReadOnly = false, // Can be readonly according to user's membership permission
                Alias = "Files", // Beautiful name given to the root/home folder
                MaxUploadSizeInKb = 500, // Limit imposed to user uploaded file <= 500 KB
                LockedFolders = new List<string>(new string[] { "Folder1" })
            };

            // Was a subfolder selected in Home Index page?
            if (!string.IsNullOrEmpty(subFolder))
            {
                root.StartPath = new DirectoryMetadata(Server.MapPath("~/Files/" + folder + "/" + subFolder));
            }

            driver.AddRoot(root);

            var connector = new Connector(driver);

            return connector.Process(this.HttpContext.Request);
        }

        public virtual ActionResult SelectFile(string target)
        {
            FileSystemDriver driver = new FileSystemDriver(new FileSystemProvider());

            driver.AddRoot(
                new Root(
                    new DirectoryMetadata(Server.MapPath("~/Files")),
                    "http://" + Request.Url.Authority + "/Files")
                {
                    IsReadOnly = false
                }
            );

            var connector = new Connector(driver);

            return Json(connector.GetFileByHash(target).Path);
        }

    }
}
