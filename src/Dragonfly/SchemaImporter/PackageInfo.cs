namespace Dragonfly.SchemaImporter
{
    using System;

    /// <summary>
    /// Static class with various information and constants about the package.
    /// </summary>
    public class PackageInfo
    {
        /// <summary>
        /// Gets the short alias of the package.
        /// </summary>
        public const string ShortAlias = "Dragonfly.SchemaImporter";

        /// <summary>
        /// Gets the alias of the package.
        /// </summary>
        public const string Alias = "Dragonfly.Umbraco10.SchemaImporter";

        /// <summary>
        /// Gets the friendly name of the package.
        /// </summary>
        public const string Name = "Dragonfly Schema Importer (for Umbraco 10)";

        /// <summary>
        /// Gets the version of the package.
        /// </summary>
        public static readonly Version Version = typeof(PackageInfo).Assembly.GetName().Version;

        /// <summary>
        /// Gets the URL of the GitHub repository for this package.
        /// </summary>
        public const string GitHubUrl = "https://github.com/hfloyd/Dragonfly.Umbraco10.SchemaImporter";

        /// <summary>
        /// Gets the URL of the issue tracker for this package.
        /// </summary>
        public const string IssuesUrl = "https://github.com/hfloyd/Dragonfly.Umbraco10.SchemaImporter/issues";

        /// <summary>
        /// Gets the URL of the documentation for this package.
        /// </summary>
        public const string DocumentationUrl = "https://github.com/hfloyd/Dragonfly.Umbraco10.SchemaImporter#documentation";



    }


}
