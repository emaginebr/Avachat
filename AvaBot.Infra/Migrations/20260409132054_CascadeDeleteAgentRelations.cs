using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AvaBot.Infra.Migrations
{
    /// <inheritdoc />
    public partial class CascadeDeleteAgentRelations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop existing foreign keys
            migrationBuilder.DropForeignKey(
                name: "avabot_fk_agents_knowledge_files",
                table: "avabot_knowledge_files");

            migrationBuilder.DropForeignKey(
                name: "avabot_fk_agents_chat_sessions",
                table: "avabot_chat_sessions");

            migrationBuilder.DropForeignKey(
                name: "avabot_fk_chat_sessions_chat_messages",
                table: "avabot_chat_messages");

            // Re-create with cascade delete
            migrationBuilder.AddForeignKey(
                name: "avabot_fk_agents_knowledge_files",
                table: "avabot_knowledge_files",
                column: "agent_id",
                principalTable: "avabot_agents",
                principalColumn: "agent_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "avabot_fk_agents_chat_sessions",
                table: "avabot_chat_sessions",
                column: "agent_id",
                principalTable: "avabot_agents",
                principalColumn: "agent_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "avabot_fk_chat_sessions_chat_messages",
                table: "avabot_chat_messages",
                column: "chat_session_id",
                principalTable: "avabot_chat_sessions",
                principalColumn: "chat_session_id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "avabot_fk_agents_knowledge_files",
                table: "avabot_knowledge_files");

            migrationBuilder.DropForeignKey(
                name: "avabot_fk_agents_chat_sessions",
                table: "avabot_chat_sessions");

            migrationBuilder.DropForeignKey(
                name: "avabot_fk_chat_sessions_chat_messages",
                table: "avabot_chat_messages");

            migrationBuilder.AddForeignKey(
                name: "avabot_fk_agents_knowledge_files",
                table: "avabot_knowledge_files",
                column: "agent_id",
                principalTable: "avabot_agents",
                principalColumn: "agent_id");

            migrationBuilder.AddForeignKey(
                name: "avabot_fk_agents_chat_sessions",
                table: "avabot_chat_sessions",
                column: "agent_id",
                principalTable: "avabot_agents",
                principalColumn: "agent_id");

            migrationBuilder.AddForeignKey(
                name: "avabot_fk_chat_sessions_chat_messages",
                table: "avabot_chat_messages",
                column: "chat_session_id",
                principalTable: "avabot_chat_sessions",
                principalColumn: "chat_session_id");
        }
    }
}
