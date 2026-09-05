using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Oqtane.Databases.Interfaces;
using Oqtane.Migrations;
using GIBS.Module.TextMe.Repository;

namespace GIBS.Module.TextMe.Migrations
{
    [DbContext(typeof(TextMeContext))]
    [Migration("GIBS.Module.TextMe.01.00.01.00")]
    public class AddConversationIdToMessages : MultiDatabaseMigration
    {
        public AddConversationIdToMessages(IDatabase database) : base(database)
        {
        }

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: ActiveDatabase.RewriteName("ConversationId"),
                table: ActiveDatabase.RewriteName("GIBSTextMe_Messages"),
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: ActiveDatabase.RewriteName("IX_GIBSTextMe_Messages_ModuleId_ConversationId"),
                table: ActiveDatabase.RewriteName("GIBSTextMe_Messages"),
                columns: new[]
                {
                    ActiveDatabase.RewriteName("ModuleId"),
                    ActiveDatabase.RewriteName("ConversationId")
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: ActiveDatabase.RewriteName("IX_GIBSTextMe_Messages_ModuleId_ConversationId"),
                table: ActiveDatabase.RewriteName("GIBSTextMe_Messages"));

            migrationBuilder.DropColumn(
                name: ActiveDatabase.RewriteName("ConversationId"),
                table: ActiveDatabase.RewriteName("GIBSTextMe_Messages"));
        }
    }
}
