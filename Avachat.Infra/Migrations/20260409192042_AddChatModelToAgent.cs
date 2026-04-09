using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Avachat.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AddChatModelToAgent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "chat_model",
                table: "avachat_agents",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "gpt-4o");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "chat_model",
                table: "avachat_agents");
        }
    }
}
