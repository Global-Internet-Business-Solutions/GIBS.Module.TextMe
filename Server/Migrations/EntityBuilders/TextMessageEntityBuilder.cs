using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Migrations.Operations.Builders;
using Oqtane.Databases.Interfaces;
using Oqtane.Migrations;
using Oqtane.Migrations.EntityBuilders;

namespace GIBS.Module.TextMe.Migrations.EntityBuilders
{
    public class TextMessageEntityBuilder : AuditableBaseEntityBuilder<TextMessageEntityBuilder>
    {
        private const string _entityTableName = "GIBSTextMe_Messages";
        private readonly PrimaryKey<TextMessageEntityBuilder> _primaryKey = new("PK_GIBSTextMe_Messages", x => x.MessageId);
        private readonly ForeignKey<TextMessageEntityBuilder> _moduleForeignKey = new("FK_GIBSTextMe_Messages_Module", x => x.ModuleId, "Module", "ModuleId", ReferentialAction.Cascade);

        public TextMessageEntityBuilder(MigrationBuilder migrationBuilder, IDatabase database) : base(migrationBuilder, database)
        {
            EntityTableName = _entityTableName;
            PrimaryKey = _primaryKey;
            ForeignKeys.Add(_moduleForeignKey);
        }

        protected override TextMessageEntityBuilder BuildTable(ColumnsBuilder table)
        {
            MessageId = AddAutoIncrementColumn(table, "MessageId");
            ModuleId = AddIntegerColumn(table, "ModuleId");
            TwilioMessageSid = AddStringColumn(table, "TwilioMessageSid", 64, true);
            Direction = AddStringColumn(table, "Direction", 10);
            SenderNumber = AddStringColumn(table, "SenderNumber", 20);
            RecipientNumber = AddStringColumn(table, "RecipientNumber", 20);
            Body = AddMaxStringColumn(table, "Body", true);
            Status = AddStringColumn(table, "Status", 30);
            ErrorCode = AddStringColumn(table, "ErrorCode", 10, true);

            AddAuditableColumns(table);

            return this;
        }

        public OperationBuilder<AddColumnOperation> MessageId { get; set; }
        public OperationBuilder<AddColumnOperation> ModuleId { get; set; }
        public OperationBuilder<AddColumnOperation> TwilioMessageSid { get; set; }
        public OperationBuilder<AddColumnOperation> Direction { get; set; }
        public OperationBuilder<AddColumnOperation> SenderNumber { get; set; }
        public OperationBuilder<AddColumnOperation> RecipientNumber { get; set; }
        public OperationBuilder<AddColumnOperation> Body { get; set; }
        public OperationBuilder<AddColumnOperation> Status { get; set; }
        public OperationBuilder<AddColumnOperation> ErrorCode { get; set; }
    }
}
