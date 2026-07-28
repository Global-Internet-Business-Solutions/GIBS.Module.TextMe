using Microsoft.AspNetCore.Builder; 
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Oqtane.Infrastructure;
using GIBS.Module.TextMe.Repository;
using GIBS.Module.TextMe.Services;

namespace GIBS.Module.TextMe.Startup
{
    public class ServerStartup : IServerStartup
    {
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // not implemented
        }

        public void ConfigureMvc(IMvcBuilder mvcBuilder)
        {
            // not implemented
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<ITextMeService, ServerTextMeService>();
            services.AddDbContextFactory<TextMeContext>(opt => { }, ServiceLifetime.Transient);
        }
    }
}
