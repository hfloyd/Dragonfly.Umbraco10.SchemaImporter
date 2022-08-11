#pragma warning disable 1591

namespace Dragonfly.SchemaImporter.Composers
{
    using Dragonfly.SchemaImporter.Services;
    using Microsoft.Extensions.DependencyInjection;
    using Umbraco.Cms.Core.Composing;
    using Umbraco.Cms.Core.DependencyInjection;

    public class SetupComposer : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            // builder.Services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            builder.Services.AddMvcCore().AddRazorViewEngine();
            builder.Services.AddControllersWithViews();
            builder.Services.AddRazorPages();
            //builder.Services.AddSingleton<IRazorViewEngine>();
            //  builder.Services.AddSingleton<ITempDataProvider, CookieTempDataProvider>();
            // builder.Services.AddScoped<IServiceProvider, ServiceProvider>();

            builder.Services.AddHttpContextAccessor();

            builder.Services.AddScoped<DependencyLoader>();
            builder.Services.AddScoped<Dragonfly.SchemaImporter.Services.SchemaImporterService>();
         
            
            //builder.AddUmbracoOptions<Settings>();

        }

    }

}