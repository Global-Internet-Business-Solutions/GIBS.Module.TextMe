using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Oqtane.Enums;
using Oqtane.Infrastructure;
using Oqtane.Interfaces;
using Oqtane.Models;
using Oqtane.Modules;
using Oqtane.Repository;
using GIBS.Module.TextMe.Repository;

namespace GIBS.Module.TextMe.Manager
{
    public class TextMeManager : MigratableModuleBase, IInstallable, IPortable, ISearchable
    {
        private readonly IDBContextDependencies _dbContextDependencies;

        public TextMeManager(IDBContextDependencies dbContextDependencies)
        {
            _dbContextDependencies = dbContextDependencies;
        }

        public bool Install(Tenant tenant, string version)
        {
            return Migrate(new TextMeContext(_dbContextDependencies), tenant, MigrationType.Up);
        }

        public bool Uninstall(Tenant tenant)
        {
            return Migrate(new TextMeContext(_dbContextDependencies), tenant, MigrationType.Down);
        }

        public string ExportModule(Oqtane.Models.Module module)
        {
            return string.Empty;
        }

        public void ImportModule(Oqtane.Models.Module module, string content, string version)
        {
        }

        public Task<List<SearchContent>> GetSearchContentsAsync(PageModule pageModule, DateTime lastIndexedOn)
        {
            return Task.FromResult(new List<SearchContent>());
        }
    }
}
