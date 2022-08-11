namespace Dragonfly.SchemaImporter.Services
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.DependencyInjection;
    using Umbraco.Cms.Core.Hosting;
    using Umbraco.Cms.Core.Services;
    using Umbraco.Cms.Core.Web;
    using Umbraco.Cms.Web.Common;

    public class DependencyLoader
    {
        public IHostingEnvironment HostingEnvironment { get; }
        public IHttpContextAccessor ContextAccessor { get; }
        public IUmbracoContextAccessor UmbracoContextAccessor { get; }

        public UmbracoHelper UmbHelper;
        public HttpContext Context;
        public ServiceContext Services;

        public IPackagingService PackagingService;

        public DependencyLoader(
            IHostingEnvironment hostingEnvironment,
            IHttpContextAccessor contextAccessor,
            IUmbracoContextAccessor umbracoContextAccessor,
            ServiceContext serviceContext,
            IPackagingService packagingService
           )
        {
            HostingEnvironment = hostingEnvironment;
            ContextAccessor = contextAccessor;
            UmbracoContextAccessor = umbracoContextAccessor;
            Context = contextAccessor.HttpContext;
            UmbHelper = contextAccessor.HttpContext.RequestServices.GetRequiredService<UmbracoHelper>();

            Services = serviceContext;
            PackagingService = packagingService;
        }
    }
}
