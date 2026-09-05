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
        public virtual DbSet<Models.TextMessage> Messages { get; set; }
        public virtual DbSet<Models.TextMedia> Media { get; set; }

        public TextMeContext(IDBContextDependencies DBContextDependencies) : base(DBContextDependencies)
        {
            // ContextBase handles multi-tenant database connections
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Models.TextMessage>().ToTable(ActiveDatabase.RewriteName("GIBSTextMe_Messages"));
            builder.Entity<Models.TextMedia>().ToTable(ActiveDatabase.RewriteName("GIBSTextMe_Media"));

            builder.Entity<Models.TextMedia>()
                .HasOne(item => item.Message)
                .WithMany(item => item.MediaItems)
                .HasForeignKey(item => item.MessageId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
