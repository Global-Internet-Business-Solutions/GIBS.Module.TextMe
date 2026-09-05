using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Oqtane.Databases.Interfaces;
using Oqtane.Migrations;
using GIBS.Module.TextMe.Migrations.EntityBuilders;
using GIBS.Module.TextMe.Repository;

namespace GIBS.Module.TextMe.Migrations
{
    [DbContext(typeof(TextMeContext))]
    [Migration("GIBS.Module.TextMe.01.00.00.00")]
    public class InitializeModule : MultiDatabaseMigration
    {
        public InitializeModule(IDatabase database) : base(database)
        {
        }

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var textMessageEntityBuilder = new TextMessageEntityBuilder(migrationBuilder, ActiveDatabase);
            textMessageEntityBuilder.Create();
            migrationBuilder.CreateIndex(
                name: ActiveDatabase.RewriteName("IX_GIBSTextMe_Messages_ModuleId"),
                table: ActiveDatabase.RewriteName("GIBSTextMe_Messages"),
                column: ActiveDatabase.RewriteName("ModuleId"));

            var textMediaEntityBuilder = new TextMediaEntityBuilder(migrationBuilder, ActiveDatabase);
            textMediaEntityBuilder.Create();
            migrationBuilder.CreateIndex(
                name: ActiveDatabase.RewriteName("IX_GIBSTextMe_Media_MessageId"),
                table: ActiveDatabase.RewriteName("GIBSTextMe_Media"),
                column: ActiveDatabase.RewriteName("MessageId"));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var textMediaEntityBuilder = new TextMediaEntityBuilder(migrationBuilder, ActiveDatabase);
            textMediaEntityBuilder.Drop();

            var textMessageEntityBuilder = new TextMessageEntityBuilder(migrationBuilder, ActiveDatabase);
            textMessageEntityBuilder.Drop();
        }
    }
}
