using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Oqtane.Modules;
using Oqtane.Repository;
using Oqtane.Infrastructure;
using Oqtane.Repository.Databases.Interfaces;

namespace GIBS.Module.TextMe.Repository
{
    public class TextMeContext : DBContextBase, ITransientService, IMultiDatabase
    {
        public virtual DbSet<Models.TextMe> TextMe { get; set; }

        public TextMeContext(IDBContextDependencies DBContextDependencies) : base(DBContextDependencies)
        {
            // ContextBase handles multi-tenant database connections
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Models.TextMe>().ToTable(ActiveDatabase.RewriteName("GIBSTextMe"));
        }
    }
}
