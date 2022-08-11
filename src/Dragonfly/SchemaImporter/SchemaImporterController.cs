namespace Dragonfly.SchemaImporter
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Dragonfly.SchemaImporter.Services;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using NPoco.fastJSON;
    using Umbraco.Cms.Web.BackOffice.Controllers;
    using Umbraco.Cms.Web.Common.Attributes;
    

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



        [HttpGet]
        public ActionResult Import()
        {
            var returnMsg = "";

            var fileName = "package.xml";

            var result = _SchemaImporterService.ImportPackageXmlOnly(fileName);

            result.Message = $"Importing '{fileName}'...";

           // var json = JsonConvert.SerializeObject(result);

            return new JsonResult(result);
        }
    }
}
