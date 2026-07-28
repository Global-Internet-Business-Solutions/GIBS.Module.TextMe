using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Oqtane.Modules;
using Oqtane.Models;
using Oqtane.Infrastructure;
using Oqtane.Interfaces;
using Oqtane.Enums;
using Oqtane.Repository;
using GIBS.Module.TextMe.Repository;
using System.Threading.Tasks;

namespace GIBS.Module.TextMe.Manager
{
    public class TextMeManager : MigratableModuleBase, IInstallable, IPortable, ISearchable
    {
        private readonly ITextMeRepository _TextMeRepository;
        private readonly IDBContextDependencies _DBContextDependencies;

        public TextMeManager(ITextMeRepository TextMeRepository, IDBContextDependencies DBContextDependencies)
        {
            _TextMeRepository = TextMeRepository;
            _DBContextDependencies = DBContextDependencies;
        }

        public bool Install(Tenant tenant, string version)
        {
            return Migrate(new TextMeContext(_DBContextDependencies), tenant, MigrationType.Up);
        }

        public bool Uninstall(Tenant tenant)
        {
            return Migrate(new TextMeContext(_DBContextDependencies), tenant, MigrationType.Down);
        }

        public string ExportModule(Oqtane.Models.Module module)
        {
            string content = "";
            List<Models.TextMe> TextMes = _TextMeRepository.GetTextMes(module.ModuleId).ToList();
            if (TextMes != null)
            {
                content = JsonSerializer.Serialize(TextMes);
            }
            return content;
        }

        public void ImportModule(Oqtane.Models.Module module, string content, string version)
        {
            List<Models.TextMe> TextMes = null;
            if (!string.IsNullOrEmpty(content))
            {
                TextMes = JsonSerializer.Deserialize<List<Models.TextMe>>(content);
            }
            if (TextMes != null)
            {
                foreach(var TextMe in TextMes)
                {
                    _TextMeRepository.AddTextMe(new Models.TextMe { ModuleId = module.ModuleId, Name = TextMe.Name });
                }
            }
        }

        public Task<List<SearchContent>> GetSearchContentsAsync(PageModule pageModule, DateTime lastIndexedOn)
        {
           var searchContentList = new List<SearchContent>();

           foreach (var TextMe in _TextMeRepository.GetTextMes(pageModule.ModuleId))
           {
               if (TextMe.ModifiedOn >= lastIndexedOn)
               {
                   searchContentList.Add(new SearchContent
                   {
                       EntityName = "GIBSTextMe",
                       EntityId = TextMe.TextMeId.ToString(),
                       Title = TextMe.Name,
                       Body = TextMe.Name,
                       ContentModifiedBy = TextMe.ModifiedBy,
                       ContentModifiedOn = TextMe.ModifiedOn
                   });
               }
           }

           return Task.FromResult(searchContentList);
        }
    }
}
