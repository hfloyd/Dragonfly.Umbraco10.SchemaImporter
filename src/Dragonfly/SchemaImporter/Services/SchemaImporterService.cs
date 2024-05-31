namespace Dragonfly.SchemaImporter.Services
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Text;
	using System.Text.Json;
	using System.Text.Json.Serialization;
	using System.Threading.Tasks;
	using System.Xml.Linq;
	using Dragonfly.NetModels;
	using Microsoft.AspNetCore.Hosting;
	using Microsoft.Extensions.Logging;
	using NPoco.fastJSON;
	using Umbraco.Cms.Core;
	using Umbraco.Cms.Core.Events;
	using Umbraco.Cms.Core.Extensions;
	using Umbraco.Cms.Core.Manifest;
	using Umbraco.Cms.Core.Models;
	using Umbraco.Cms.Core.Models.Packaging;
	using Umbraco.Cms.Core.Notifications;
	using Umbraco.Cms.Core.Packaging;
	using Umbraco.Cms.Core.Services;
	using Umbraco.Cms.Infrastructure.Packaging;
	using Umbraco.Cms.Infrastructure.Scoping;
	using Umbraco.Extensions;
	using File = System.IO.File;

	public class SchemaImporterService
	{
		private readonly ILogger<SchemaImporterService> _logger;
		private readonly IWebHostEnvironment _WebHostEnvironment;
		private readonly IPackagingService _PackagingService;
		private readonly IAuditService _auditService;
		private readonly IEventAggregator _eventAggregator;
		private readonly PackageDataInstallation _packageDataInstallation;
		private readonly IScopeProvider _scopeProvider;
		private readonly IMediaService _mediaService;
		private readonly IMediaTypeService _mediaTypeService;
		private readonly IContentTypeService _contentTypeService;
		private readonly IContentService _contentService;


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
			IPackagingService packagingService,
			IAuditService auditService,
			IEventAggregator eventAggregator,
			PackageDataInstallation packageDataInstallation,
			IScopeProvider scopeProvider,
			IContentTypeService contentTypeService,
			IContentService contentService,
			IMediaService mediaService,
			IMediaTypeService mediaTypeService)
		{
			_logger = logger;
			_WebHostEnvironment = HostingEnvironment;
			_PackagingService = packagingService;
			_auditService = auditService;
			_eventAggregator = eventAggregator;

			_scopeProvider = scopeProvider;
			_contentTypeService = contentTypeService;
			_contentService = contentService;
			_mediaService = mediaService;
			_mediaTypeService = mediaTypeService;


			_packageDataInstallation =
				packageDataInstallation ?? throw new ArgumentNullException(nameof(packageDataInstallation));

			_tempFolderFullPath = Path.GetFullPath(
				Path.Combine(hostingEnvironment.LocalTempPath, _tempFolderName));
			_scopeProvider = scopeProvider;
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

			var msg = $"TempFile: '{tempFile}' isInWebRoot:{isInWebRoot}";
			statusMsg.DetailedMessages.Add(msg);
			_logger.LogInformation("SchemaImporterService.ImportPackageXml: " + msg);

			if (isInWebRoot)
			{
				InstallationSummary summary = new InstallationSummary("");
				try
				{
					var fileInfo = new FileInfo(webrootPath);
					// summary = _PackagingService.InstallCompiledPackageData(fileInfo);
					summary = InstallCompiledPackageData(fileInfo);
					statusMsg.RelatedObject = summary;

					var msg2 = $"'{FilePath}' Imported from WebRoot";
					statusMsg.DetailedMessages.Add(msg2);
					_logger.LogInformation($"SchemaImporterService.ImportPackageXml:" + msg2, summary);
					statusMsg.Success = true;
				}
				catch (Exception ex)
				{
					//var options = new JsonSerializerOptions { WriteIndented = true };
					//var summaryInfo = JsonSerializer.Serialize(summary, options);
					var msg2 = $"Error Importing '{tempFile}' - {ex.Message}";
					statusMsg.DetailedMessages.Add(msg2);
					_logger.LogError($"SchemaImporterService.ImportPackageXml: {msg2}", ex);
					statusMsg.Success = false;
					//throw;
				}
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


		// Copied from Umbraco Core (\src\Umbraco.Infrastructure\Services\Implement\PackagingService.cs)
		public InstallationSummary InstallCompiledPackageData(
		 FileInfo packageXmlFile,
		 int userId = Constants.Security.SuperUserId)
		{
			XDocument xml;
			using (StreamReader streamReader = File.OpenText(packageXmlFile.FullName))
			{
				xml = XDocument.Load(streamReader);
			}

			return InstallCompiledPackageData(xml, userId);
		}

		// Copied from Umbraco Core (\src\Umbraco.Infrastructure\Services\Implement\PackagingService.cs)
		// and EDITED slightly
		public InstallationSummary InstallCompiledPackageData(
		 XDocument? packageXml,
		 int userId = Constants.Security.SuperUserId)
		{
			CompiledPackage compiledPackage = _PackagingService.GetCompiledPackageInfo(packageXml);

			if (compiledPackage == null)
			{
				throw new InvalidOperationException("Could not read the package file " + packageXml);
			}

			// Trigger the Importing Package Notification and stop execution if event/user is cancelling it
			var importingPackageNotification = new ImportingPackageNotification(compiledPackage.Name);
			if (_eventAggregator.PublishCancelable(importingPackageNotification))
			{
				return new InstallationSummary(compiledPackage.Name);
			}

			InstallationSummary summary = InstallPackageData(compiledPackage, userId, out _);

			_auditService.Add(AuditType.PackagerInstall, userId, -1, "Package", $"Package data installed for package '{compiledPackage.Name}'.");

			// trigger the ImportedPackage event
			_eventAggregator.Publish(new ImportedPackageNotification(summary).WithStateFrom(importingPackageNotification));

			return summary;
		}

		//Copied from Umbraco.Core (\src\Umbraco.Infrastructure\Packaging\PackageInstallation.cs)
		//and EDITED slightly
		public InstallationSummary InstallPackageData(CompiledPackage compiledPackage, int userId, out PackageDefinition packageDefinition)
		{
			packageDefinition = new PackageDefinition { Name = compiledPackage.Name };

			//		InstallationSummary installationSummary = _packageDataInstallation.InstallPackageData(compiledPackage, userId);
			InstallationSummary installationSummary = InstallPackageData(compiledPackage, userId);

			// Make sure the definition is up to date with everything (note: macro partial views are embedded in macros)
			foreach (IDataType x in installationSummary.DataTypesInstalled)
			{
				packageDefinition.DataTypes.Add(x.Id.ToInvariantString());
			}

			foreach (ILanguage x in installationSummary.LanguagesInstalled)
			{
				packageDefinition.Languages.Add(x.Id.ToInvariantString());
			}

			foreach (IDictionaryItem x in installationSummary.DictionaryItemsInstalled)
			{
				packageDefinition.DictionaryItems.Add(x.Id.ToInvariantString());
			}

			foreach (IMacro x in installationSummary.MacrosInstalled)
			{
				packageDefinition.Macros.Add(x.Id.ToInvariantString());
			}

			foreach (ITemplate x in installationSummary.TemplatesInstalled)
			{
				packageDefinition.Templates.Add(x.Id.ToInvariantString());
			}

			foreach (IContentType x in installationSummary.DocumentTypesInstalled)
			{
				packageDefinition.DocumentTypes.Add(x.Id.ToInvariantString());
			}

			foreach (IMediaType x in installationSummary.MediaTypesInstalled)
			{
				packageDefinition.MediaTypes.Add(x.Id.ToInvariantString());
			}

			foreach (IFile x in installationSummary.StylesheetsInstalled)
			{
				packageDefinition.Stylesheets.Add(x.Path);
			}

			foreach (IScript x in installationSummary.ScriptsInstalled)
			{
				packageDefinition.Scripts.Add(x.Path);
			}

			foreach (IPartialView x in installationSummary.PartialViewsInstalled)
			{
				packageDefinition.PartialViews.Add(x.Path);
			}

			packageDefinition.ContentNodeId = installationSummary.ContentInstalled.FirstOrDefault()?.Id.ToInvariantString();

			foreach (IMedia x in installationSummary.MediaInstalled)
			{
				packageDefinition.MediaUdis.Add(x.GetUdi());
			}

			return installationSummary;
		}
		//Copied From Umbraco.Core (src\Umbraco.Infrastructure\Packaging\PackageDataInstallation.cs)
		//and EDITED
		public InstallationSummary InstallPackageData(CompiledPackage compiledPackage, int userId)
		{
			using (IScope scope = _scopeProvider.CreateScope())
			{
				var phase = "START";
				var completed = "";
				var installationSummary = new InstallationSummary(compiledPackage.Name)
				{
					Warnings = compiledPackage.Warnings
				};

				try
				{
					phase = "ImportDataTypes";
					installationSummary.DataTypesInstalled =
						_packageDataInstallation.ImportDataTypes(compiledPackage.DataTypes.ToList(), userId,
							out IEnumerable<EntityContainer> dataTypeEntityContainersInstalled);

					completed+= " Completed: DataTypes";
					phase = "ImportLanguages";
					installationSummary.LanguagesInstalled =
						_packageDataInstallation.ImportLanguages(compiledPackage.Languages, userId);
					completed += ", Languages";

					phase = "ImportDictionaryItems";
					installationSummary.DictionaryItemsInstalled =
						_packageDataInstallation.ImportDictionaryItems(compiledPackage.DictionaryItems, userId);
					completed += ", Dictionary Items";

					phase = "ImportMacros";
					installationSummary.MacrosInstalled =
						_packageDataInstallation.ImportMacros(compiledPackage.Macros, userId);
					completed += ", Macros";

					phase = "ImportMacroPartialViews";
					installationSummary.MacroPartialViewsInstalled =
						_packageDataInstallation.ImportMacroPartialViews(compiledPackage.MacroPartialViews, userId);
					completed += ", Macro Partial Views";

					phase = "ImportTemplates";
					installationSummary.TemplatesInstalled =
						_packageDataInstallation.ImportTemplates(compiledPackage.Templates.ToList(), userId);
					completed += ", Templates";

					phase = "ImportDocumentTypes";
					installationSummary.DocumentTypesInstalled =
						_packageDataInstallation.ImportDocumentTypes(compiledPackage.DocumentTypes, userId,
							out IEnumerable<EntityContainer> documentTypeEntityContainersInstalled);
					completed += ", Document Types";

					phase = "ImportMediaTypes";
					installationSummary.MediaTypesInstalled =
						_packageDataInstallation.ImportMediaTypes(compiledPackage.MediaTypes, userId,
							out IEnumerable<EntityContainer> mediaTypeEntityContainersInstalled);
					completed += ", Media Types";

					phase = "ImportStylesheets";
					installationSummary.StylesheetsInstalled = _packageDataInstallation.ImportStylesheets(compiledPackage.Stylesheets, userId);
					completed += ", Stylesheets";

					phase = "ImportScripts";
					installationSummary.ScriptsInstalled =
						_packageDataInstallation.ImportScripts(compiledPackage.Scripts, userId);
					completed += ", Scripts";

					phase = "ImportPartialViews";
					installationSummary.PartialViewsInstalled =
						_packageDataInstallation.ImportPartialViews(compiledPackage.PartialViews, userId);
					completed += ", Partial Views";

					phase = "EntityContainersInstalled";
					var entityContainersInstalled = new List<EntityContainer>();
					entityContainersInstalled.AddRange(dataTypeEntityContainersInstalled);
					entityContainersInstalled.AddRange(documentTypeEntityContainersInstalled);
					entityContainersInstalled.AddRange(mediaTypeEntityContainersInstalled);
					installationSummary.EntityContainersInstalled = entityContainersInstalled;

					// We need a reference to the imported doc types to continue
					phase = "DocumentTypesInstalled.ToDictionary";
					var importedDocTypes = installationSummary.DocumentTypesInstalled.ToDictionary(x => x.Alias, x => x);

					phase = "MediaTypesInstalled.ToDictionary";
					var importedMediaTypes = installationSummary.MediaTypesInstalled.ToDictionary(x => x.Alias, x => x);

					phase = "ImportContentBase.Documents";
					installationSummary.ContentInstalled = _packageDataInstallation.ImportContentBase(compiledPackage.Documents, importedDocTypes,
						userId, _contentTypeService, _contentService);

					phase = "ImportContentBase.Media";
					installationSummary.MediaInstalled = _packageDataInstallation.ImportContentBase(compiledPackage.Media, importedMediaTypes,
						userId, _mediaTypeService, _mediaService);

					phase = "END";
				}
				catch (Exception e)
				{
					var msg = $"Error during package installation: InstallPackageData:{phase} - {e.Message}";

					if (phase == "ImportDocumentTypes" && e.Message.Contains("Sequence contains no matching element"))
					{
						msg = $"{msg} \r\n(This error may indicate an issue creating DocType Folders. " +
						      $"There might be a conflict with existing folders - try removing folder information from your package.xml file and try the import again.)";
					}

					msg += "\r\n" + completed;
					_logger.LogError(e, msg);
					throw;
				}

				scope.Complete();

				return installationSummary;
			}
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
