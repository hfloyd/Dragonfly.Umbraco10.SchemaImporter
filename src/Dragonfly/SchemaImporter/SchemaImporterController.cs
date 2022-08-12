using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Dragonfly.SchemaImporter
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Dragonfly.NetModels;
    using Dragonfly.SchemaImporter.Services;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;
    using NPoco.fastJSON;
    using Umbraco.Cms.Web.BackOffice.Controllers;
    using Umbraco.Cms.Web.Common.Attributes;
    using Umbraco.Extensions;


    //  /umbraco/backoffice/Dragonfly/SchemaImporter/
    [PluginController("Dragonfly")]
    [IsBackOffice]
    public class SchemaImporterController : UmbracoAuthorizedApiController
    {

        private readonly ILogger<SchemaImporterController> _logger;
        private readonly SchemaImporterService _SchemaImporterService;

        public SchemaImporterController(
            ILogger<SchemaImporterController> logger,
            SchemaImporterService schemaImporterService
        )
        {
            _logger = logger;
            _SchemaImporterService = schemaImporterService;
        }

        //  /umbraco/backoffice/Dragonfly/SchemaImporter/GetApi
        /// <summary>
        ///  simple call, used to locate the controller
        ///  when we inject it into the javascript variables.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public string GetApi() => "Hello, Schema Import Controller Found.";


        //  /umbraco/backoffice/Dragonfly/SchemaImporter/GetVersion
        [HttpGet]
        public string GetVersion()
        {
            return PackageInfo.Version.ToString();
        }


        //  /umbraco/backoffice/Dragonfly/SchemaImporter/ImportDefault
        [HttpGet]
        public ActionResult ImportDefault()
        {
            var fileName = "package.xml";
            var filePath = "/Temp/" + _SchemaImporterService.TempFolderName.EnsureEndsWith("/") + fileName;
            var result = _SchemaImporterService.ImportPackageXmlOnly(filePath);

            result.Message = $"Importing '{fileName}'...";

            // var json = JsonConvert.SerializeObject(result);

            return new JsonResult(result);
        }

        //  /umbraco/backoffice/Dragonfly/SchemaImporter/UploadImport
        //Based on code From https://github.com/KevinJump/uSync/blob/f0f045a9fa77f5d6b5e4052598c516f4c74db858/uSync.BackOffice/Controllers/uSyncDashboardApiController.cs#L316
        [HttpPost]
        public async Task<UploadImportResult> UploadImport()
        {
            var file = Request.Form.Files[0];
            //var clean = Request.Form["clean"];

            if (file.Length > 0)
            {
                var stub = Path.GetFileNameWithoutExtension(file.FileName);
                var ext = Path.GetExtension(file.FileName);
                var tempFileName = Dragonfly.NetHelpers.Strings.CreateUniqueName(stub) + ext;

                var tempFolder = _SchemaImporterService.TempFolderFullPath;
                var tempFile = Path.Combine(tempFolder, tempFileName);

                Directory.CreateDirectory(Path.GetDirectoryName(tempFile));

                using (var stream = new FileStream(tempFile, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                try
                {
                    if (ext == ".xml")
                    {
                        var result = _SchemaImporterService.ImportPackageXmlOnly(tempFile);
                        if (!result.Success)
                        {
                            return new UploadImportResult(false)
                            {
                                Errors = ConvertStatusMessageToErrorsList(result),
                                FilePath = tempFile,
                                DetailedResultStatus = result
                            };
                        }
                        else
                        {
                            return new UploadImportResult(true)
                            {
                                FilePath = tempFile,
                                DetailedResultStatus = result
                            };
                        }
                    }
                    else if (ext == ".zip")
                    {
                        var error = "Zip files are not yet supported.";
                        var result = new StatusMessage(false, error);
                        return new UploadImportResult(false)
                        {
                            Errors = error.AsEnumerableOfOne(),
                            FilePath = tempFile,
                            DetailedResultStatus = result
                        };
                    }
                    else
                    {
                        var error = $"Files of type '{ext}' are not supported.";
                        var result = new StatusMessage(false, error);
                        return new UploadImportResult(false)
                        {
                            Errors = error.AsEnumerableOfOne(),
                            FilePath = tempFile,
                            DetailedResultStatus = result
                        };
                    }
                }
                catch
                {
                    throw;
                }
                finally
                {
                    // remove the temp folder & file
                    _SchemaImporterService.DeleteFile(tempFile);
                    _SchemaImporterService.DeleteFolder(tempFolder, true);
                }
            }
            else
            {
                var errorFile = $"File is empty.";
                var result = new StatusMessage(false, errorFile);
                return new UploadImportResult(false)
                {
                    Errors = errorFile.AsEnumerableOfOne(),
                    DetailedResultStatus = result
                };
            }

        }

        private IEnumerable<string> ConvertStatusMessageToErrorsList(StatusMessage Msg)
        {
            var errors = new List<string>();

            if (Msg.HasAnyExceptions())
            {
                if (Msg.RelatedException != null)
                {
                    var err = $"Exception: {Msg.RelatedException.Message} - See Log for details";
                    errors.Add(err);
                }

                if (Msg.InnerStatuses.Any())
                {
                    foreach (var innerStatus in Msg.InnerStatuses)
                    {
                        if (innerStatus.RelatedException != null)
                        {
                            var err = $"Exception: {innerStatus.RelatedException.Message} - See Log for details";
                            errors.Add(err);
                        }
                    }
                }
            }

            if (Msg.DetailedMessages.Any())
            {
                errors.AddRange(Msg.DetailedMessages);
            }

            if (Msg.InnerStatuses.Any())
            {
                foreach (var innerStatus in Msg.InnerStatuses)
                {
                    if (innerStatus.DetailedMessages.Any())
                    {
                        errors.AddRange(Msg.DetailedMessages);
                    }
                }
            }

            return errors;
        }

    }


    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class UploadImportResult
    {
        public UploadImportResult(bool success)
        {
            Success = success;
        }

        public bool Success { get; set; }

        public IEnumerable<string> Errors { get; set; }
        public string FilePath { get; set; }

        public StatusMessage DetailedResultStatus { get; set; }
    }
}


