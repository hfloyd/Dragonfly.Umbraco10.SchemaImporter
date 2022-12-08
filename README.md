# Dragonfly Umbraco 10 Schema Importer #

A tool to quickly import a generated package.xml file into the back-office of an Umbraco 10 Site created by [Heather Floyd](https://www.HeatherFloyd.com).

[![Dragonfly Website](https://img.shields.io/badge/Dragonfly-Website-A84492)](https://DragonflyLibraries.com/umbraco-packages/schema-importer/) [![Umbraco Marketplace](https://img.shields.io/badge/Umbraco-Marketplace-3544B1?logo=Umbraco&logoColor=white)](https://marketplace.umbraco.com/package/Dragonfly.Umbraco10.SchemaImporter) [![Nuget Downloads](https://buildstats.info/nuget/Dragonfly.Umbraco10.SchemaImporter)](https://www.nuget.org/packages/Dragonfly.Umbraco10.SchemaImporter/) [![GitHub](https://img.shields.io/badge/GitHub-Sourcecode-blue?logo=github)](https://github.com/hfloyd/Dragonfly.Umbraco10.SchemaImporter)


## Versions ##
This package is designed to work with Umbraco 10. [View all available versions](https://DragonflyLibraries.com/umbraco-packages/schema-importer/#Versions).

## Installation ##
[![Nuget Downloads](https://buildstats.info/nuget/Dragonfly.Umbraco10.SchemaImporter)](https://www.nuget.org/packages/Dragonfly.Umbraco10.SchemaImporter/)


```
    PM > Install-Package Dragonfly.Umbraco10.SchemaImporter
```

## Usage ##
After installation, a Dashboard is added to the "Settings" section in the Umbraco back-office (ex: http://YOURSITE.COM/umbraco/#/settings?dashboard=Dragonfly.SchemaImporter)

1. In your source site, use the "Create Package" tool in the "Packages" section to choose the schema items you want to copy, and download the resulting ZIP file. Extract the "package.xml" file from the ZIP.
2. Install 'Schema Importer' into your destination site, and access it via the dashboard added in the "Settings" area. Browse to your downloaded "package.xml" file and import it into your site.

Umbraco's built-in package processing API is used - just like for NuGet packages, so your schema will be imported just like it was part of a NuGet package, without the overhead.

