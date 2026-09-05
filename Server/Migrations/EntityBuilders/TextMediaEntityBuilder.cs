using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Migrations.Operations.Builders;
using Oqtane.Databases.Interfaces;
using Oqtane.Migrations;
using Oqtane.Migrations.EntityBuilders;

namespace GIBS.Module.TextMe.Migrations.EntityBuilders
{
    public class TextMediaEntityBuilder : BaseEntityBuilder<TextMediaEntityBuilder>
    {
        private const string _entityTableName = "GIBSTextMe_Media";
        private readonly PrimaryKey<TextMediaEntityBuilder> _primaryKey = new("PK_GIBSTextMe_Media", x => x.MediaId);
        private readonly ForeignKey<TextMediaEntityBuilder> _messageForeignKey = new("FK_GIBSTextMe_Media_Messages", x => x.MessageId, "GIBSTextMe_Messages", "MessageId", ReferentialAction.Cascade);

        public TextMediaEntityBuilder(MigrationBuilder migrationBuilder, IDatabase database) : base(migrationBuilder, database)
        {
            EntityTableName = _entityTableName;
            PrimaryKey = _primaryKey;
            ForeignKeys.Add(_messageForeignKey);
        }

        protected override TextMediaEntityBuilder BuildTable(ColumnsBuilder table)
        {
            MediaId = AddAutoIncrementColumn(table, "MediaId");
            MessageId = AddIntegerColumn(table, "MessageId");
            MediaUrl = AddStringColumn(table, "MediaUrl", 500);
            ContentType = AddStringColumn(table, "ContentType", 100);
            

            return this;
        }

        public OperationBuilder<AddColumnOperation> MediaId { get; set; }
        public OperationBuilder<AddColumnOperation> MessageId { get; set; }
        public OperationBuilder<AddColumnOperation> MediaUrl { get; set; }
        public OperationBuilder<AddColumnOperation> ContentType { get; set; }
        
    }
}
