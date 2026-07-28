using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using Oqtane.Services;
using GIBS.Module.TextMe.Services;

namespace GIBS.Module.TextMe.Startup
{
    public class ClientStartup : IClientStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            if (!services.Any(s => s.ServiceType == typeof(ITextMeService)))
            {
                services.AddScoped<ITextMeService, ClientTextMeService>();
            }
        }
    }
}
