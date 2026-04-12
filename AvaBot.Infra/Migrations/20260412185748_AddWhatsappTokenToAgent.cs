using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AvaBot.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AddWhatsappTokenToAgent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "whatsapp_token",
                table: "avabot_agents",
                type: "character varying(260)",
                maxLength: 260,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_avabot_agents_whatsapp_token",
                table: "avabot_agents",
                column: "whatsapp_token",
                unique: true,
                filter: "whatsapp_token IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_avabot_agents_whatsapp_token",
                table: "avabot_agents");

            migrationBuilder.DropColumn(
                name: "whatsapp_token",
                table: "avabot_agents");
        }
    }
}
