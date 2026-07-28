using System.Collections.Generic;
using System.Threading.Tasks;

namespace GIBS.Module.TextMe.Services
{
    public interface ITextMeService 
    {
        Task<List<Models.TextMe>> GetTextMesAsync(int ModuleId);

        Task<Models.TextMe> GetTextMeAsync(int TextMeId, int ModuleId);

        Task<Models.TextMe> AddTextMeAsync(Models.TextMe TextMe);

        Task<Models.TextMe> UpdateTextMeAsync(Models.TextMe TextMe);

        Task DeleteTextMeAsync(int TextMeId, int ModuleId);
    }
}
