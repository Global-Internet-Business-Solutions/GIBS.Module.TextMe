using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Migrations.Operations.Builders;
using Oqtane.Databases.Interfaces;
using Oqtane.Migrations;
using Oqtane.Migrations.EntityBuilders;

namespace GIBS.Module.TextMe.Migrations.EntityBuilders
{
    public class TextMeEntityBuilder : AuditableBaseEntityBuilder<TextMeEntityBuilder>
    {
        private const string _entityTableName = "GIBSTextMe";
        private readonly PrimaryKey<TextMeEntityBuilder> _primaryKey = new("PK_GIBSTextMe", x => x.TextMeId);
        private readonly ForeignKey<TextMeEntityBuilder> _moduleForeignKey = new("FK_GIBSTextMe_Module", x => x.ModuleId, "Module", "ModuleId", ReferentialAction.Cascade);

        public TextMeEntityBuilder(MigrationBuilder migrationBuilder, IDatabase database) : base(migrationBuilder, database)
        {
            EntityTableName = _entityTableName;
            PrimaryKey = _primaryKey;
            ForeignKeys.Add(_moduleForeignKey);
        }

        protected override TextMeEntityBuilder BuildTable(ColumnsBuilder table)
        {
            TextMeId = AddAutoIncrementColumn(table,"TextMeId");
            ModuleId = AddIntegerColumn(table,"ModuleId");
            Name = AddMaxStringColumn(table,"Name");
            AddAuditableColumns(table);
            return this;
        }

        public OperationBuilder<AddColumnOperation> TextMeId { get; set; }
        public OperationBuilder<AddColumnOperation> ModuleId { get; set; }
        public OperationBuilder<AddColumnOperation> Name { get; set; }
    }
}
