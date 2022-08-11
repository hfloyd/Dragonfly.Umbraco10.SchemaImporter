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
        private readonly IWebHostEnvironment _HostingEnvironment;
        private readonly IPackagingService _PackagingService;

        private string _tempFolder = "/Temp/SchemaImporter/";

        public string TempFolder
        {
            get => _tempFolder;
        }

        public SchemaImporterService(
            ILogger<SchemaImporterService> logger,
            IWebHostEnvironment HostingEnvironment,
            IPackagingService packagingService)
        {
            _logger = logger;
            _HostingEnvironment = HostingEnvironment;
            _PackagingService = packagingService;
        }

        public StatusMessage ImportPackageXmlOnly(string FilePath)
        {
            var statusMsg = new StatusMessage(false);
            statusMsg.RunningFunctionName = "SchemaImporterService.ImportPackageXml";

            var tempFile = _tempFolder.EnsureEndsWith("/") + FilePath;
            statusMsg.ObjectName = tempFile;

            var webrootPath = _HostingEnvironment.MapPathWebRoot(tempFile);
            var isInWebRoot = System.IO.File.Exists(webrootPath);

            var contentPath = _HostingEnvironment.MapPathContentRoot(tempFile);
            var isInContentRoot = System.IO.File.Exists(contentPath);

            var msg = $"TempFile: '{tempFile}' isInWebRoot:{isInWebRoot} isInContentRoot:{isInContentRoot}";
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
            else if (isInContentRoot)
            {
                var fileInfo = new FileInfo(contentPath);
                var summary = _PackagingService.InstallCompiledPackageData(fileInfo);
                statusMsg.RelatedObject = summary;

                var msg2 = $"'{FilePath}' Imported from ContentRoot";
                statusMsg.DetailedMessages.Add(msg2);
                _logger.LogInformation($"SchemaImporterService.ImportPackageXml:" + msg2, summary);
                statusMsg.Success = true;
            }
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
    }
}
