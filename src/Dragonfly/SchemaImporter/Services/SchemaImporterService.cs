namespace Dragonfly.SchemaImporter.Services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Dragonfly.NetModels;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Logging;
    using Umbraco.Cms.Core.Extensions;
    using Umbraco.Cms.Core.Services;
    using Umbraco.Extensions;

    public class SchemaImporterService
    {
        private readonly ILogger<SchemaImporterService> _logger;
        private readonly IWebHostEnvironment _WebHostEnvironment;
        private readonly IPackagingService _PackagingService;

        private string _tempFolderName = "SchemaImporter";

        public string TempFolderName
        {
            get => _tempFolderName;
        }
        private string _tempFolderFullPath = "";

        public string TempFolderFullPath
        {
            get => _tempFolderFullPath;
        }
      

        public SchemaImporterService(
            ILogger<SchemaImporterService> logger,
            IWebHostEnvironment HostingEnvironment,
            Umbraco.Cms.Core.Hosting.IHostingEnvironment hostingEnvironment,
            IPackagingService packagingService)
        {
            _logger = logger;
            _WebHostEnvironment = HostingEnvironment;
            _PackagingService = packagingService;

            _tempFolderFullPath = Path.GetFullPath(
                Path.Combine(hostingEnvironment.LocalTempPath, _tempFolderName));
        }

        public StatusMessage ImportPackageXmlOnly(string FilePath)
        {
            var statusMsg = new StatusMessage(false);
            statusMsg.RunningFunctionName = "SchemaImporterService.ImportPackageXml";

            //  var tempFile = _tempFolder.EnsureEndsWith("/") + FilePath;
            var tempFile = FilePath;
            statusMsg.ObjectName = tempFile;

            var webrootPath = _WebHostEnvironment.MapPathWebRoot(tempFile);
            var isInWebRoot = System.IO.File.Exists(webrootPath);

         //   var contentPath = _WebHostEnvironment.MapPathContentRoot(tempFile);
          //  var isInContentRoot = false;// System.IO.File.Exists(contentPath);

            var msg = $"TempFile: '{tempFile}' isInWebRoot:{isInWebRoot}";
            statusMsg.DetailedMessages.Add(msg);
            _logger.LogInformation("SchemaImporterService.ImportPackageXml: " + msg);

            if (isInWebRoot)
            {
                var fileInfo = new FileInfo(webrootPath);
                var summary = _PackagingService.InstallCompiledPackageData(fileInfo);
                statusMsg.RelatedObject = summary;

                var msg2 = $"'{FilePath}' Imported from WebRoot";
                statusMsg.DetailedMessages.Add(msg2);
                _logger.LogInformation($"SchemaImporterService.ImportPackageXml:" + msg2, summary);
                statusMsg.Success = true;
            }
            //else if (isInContentRoot)
            //{
            //    var fileInfo = new FileInfo(contentPath);
            //    var summary = _PackagingService.InstallCompiledPackageData(fileInfo);
            //    statusMsg.RelatedObject = summary;

            //    var msg2 = $"'{FilePath}' Imported from ContentRoot";
            //    statusMsg.DetailedMessages.Add(msg2);
            //    _logger.LogInformation($"SchemaImporterService.ImportPackageXml:" + msg2, summary);
            //    statusMsg.Success = true;
            //}
            else
            {
                //File not found
                var msg2 = $"Unable to Import '{tempFile}' - File not found on disk.";
                statusMsg.DetailedMessages.Add(msg2);
                _logger.LogWarning($"SchemaImporterService.ImportPackageXml:" + msg2);
                statusMsg.Success = false;
            }

            return statusMsg;
        }

        #region File Management
        //Copied from https://github.com/KevinJump/uSync/blob/f0f045a9fa77f5d6b5e4052598c516f4c74db858/uSync.BackOffice/Services/SyncFileService.cs#L87

        /// <summary>
        /// Remove a folder from disk
        /// </summary>
        /// <param name="folder"></param>
        /// <param name="safe"></param>
        public void DeleteFolder(string folder, bool safe = false)
        {
            try
            {
                var resolvedFolder = GetAbsPath(folder);
                if (Directory.Exists(resolvedFolder))
                    Directory.Delete(resolvedFolder, true);
            }
            catch (Exception ex)
            {
                // can happen when its locked, question is - do you care?
                _logger.LogWarning(ex, "Failed to remove directory {folder}", folder);
                if (!safe) throw;
            }
        }


        /// <summary>
        ///  remove a file from disk.
        /// </summary>
        /// <param name="path"></param>
        public void DeleteFile(string path)
        {
            var localPath = GetAbsPath(path);
            if (FileExists(localPath))
                File.Delete(localPath);
        }

        /// <summary>
        ///  does a file exist 
        /// </summary>
        public bool FileExists(string path)
            => File.Exists(GetAbsPath(path));

        /// <summary>
        ///  return the absolute path for any given path. 
        /// </summary>
        public string GetAbsPath(string path)
        {
            if (Path.IsPathFullyQualified(path)) return CleanLocalPath(path);
            return CleanLocalPath(_WebHostEnvironment.MapPathContentRoot(path.TrimStart('/')));
        }

        /// <summary>
        ///  clean up the local path, and full expand any short file names
        /// </summary>
        private string CleanLocalPath(string path)
            => Path.GetFullPath(path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar));
    }

    #endregion
}
