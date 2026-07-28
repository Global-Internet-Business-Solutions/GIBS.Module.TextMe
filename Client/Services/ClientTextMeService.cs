using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Oqtane.Services;
using Oqtane.Shared;

namespace GIBS.Module.TextMe.Services
{

    public class ClientTextMeService : ServiceBase, ITextMeService
    {
        public ClientTextMeService(HttpClient http, SiteState siteState) : base(http, siteState) { }

        private string Apiurl => CreateApiUrl("TextMe");

        public async Task<List<Models.TextMe>> GetTextMesAsync(int ModuleId)
        {
            List<Models.TextMe> TextMes = await GetJsonAsync<List<Models.TextMe>>(CreateAuthorizationPolicyUrl($"{Apiurl}?moduleid={ModuleId}", EntityNames.Module, ModuleId), Enumerable.Empty<Models.TextMe>().ToList());
            return TextMes.OrderBy(item => item.Name).ToList();
        }

        public async Task<Models.TextMe> GetTextMeAsync(int TextMeId, int ModuleId)
        {
            return await GetJsonAsync<Models.TextMe>(CreateAuthorizationPolicyUrl($"{Apiurl}/{TextMeId}/{ModuleId}", EntityNames.Module, ModuleId));
        }

        public async Task<Models.TextMe> AddTextMeAsync(Models.TextMe TextMe)
        {
            return await PostJsonAsync<Models.TextMe>(CreateAuthorizationPolicyUrl($"{Apiurl}", EntityNames.Module, TextMe.ModuleId), TextMe);
        }

        public async Task<Models.TextMe> UpdateTextMeAsync(Models.TextMe TextMe)
        {
            return await PutJsonAsync<Models.TextMe>(CreateAuthorizationPolicyUrl($"{Apiurl}/{TextMe.TextMeId}", EntityNames.Module, TextMe.ModuleId), TextMe);
        }

        public async Task DeleteTextMeAsync(int TextMeId, int ModuleId)
        {
            await DeleteAsync(CreateAuthorizationPolicyUrl($"{Apiurl}/{TextMeId}/{ModuleId}", EntityNames.Module, ModuleId));
        }
    }
}
