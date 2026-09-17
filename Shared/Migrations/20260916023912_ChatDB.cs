using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Shared.Migrations
{
    /// <inheritdoc />
    public partial class ChatDB : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "chat_contacts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: true),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    AvatarUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CustomAttributesJson = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chat_contacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_chat_contacts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "chat_inboxes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ChannelType = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    SettingsJson = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chat_inboxes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "chat_labels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Color = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chat_labels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "chat_teams",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AvatarUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chat_teams", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "chat_contact_inboxes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ContactId = table.Column<long>(type: "bigint", nullable: false),
                    InboxId = table.Column<long>(type: "bigint", nullable: false),
                    SourceId = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chat_contact_inboxes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_chat_contact_inboxes_chat_contacts_ContactId",
                        column: x => x.ContactId,
                        principalTable: "chat_contacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_chat_contact_inboxes_chat_inboxes_InboxId",
                        column: x => x.InboxId,
                        principalTable: "chat_inboxes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "chat_conversations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InboxId = table.Column<long>(type: "bigint", nullable: false),
                    ContactId = table.Column<long>(type: "bigint", nullable: false),
                    AssignedUserId = table.Column<string>(type: "text", nullable: true),
                    TeamId = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    Subject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    LastMessageAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chat_conversations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_chat_conversations_Users_AssignedUserId",
                        column: x => x.AssignedUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_chat_conversations_chat_contacts_ContactId",
                        column: x => x.ContactId,
                        principalTable: "chat_contacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_chat_conversations_chat_inboxes_InboxId",
                        column: x => x.InboxId,
                        principalTable: "chat_inboxes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_chat_conversations_chat_teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "chat_teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "chat_team_members",
                columns: table => new
                {
                    TeamId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chat_team_members", x => new { x.TeamId, x.UserId });
                    table.ForeignKey(
                        name: "FK_chat_team_members_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_chat_team_members_chat_teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "chat_teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "chat_conversation_labels",
                columns: table => new
                {
                    ConversationId = table.Column<long>(type: "bigint", nullable: false),
                    LabelId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chat_conversation_labels", x => new { x.ConversationId, x.LabelId });
                    table.ForeignKey(
                        name: "FK_chat_conversation_labels_chat_conversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "chat_conversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_chat_conversation_labels_chat_labels_LabelId",
                        column: x => x.LabelId,
                        principalTable: "chat_labels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "chat_messages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ConversationId = table.Column<long>(type: "bigint", nullable: false),
                    InboxId = table.Column<long>(type: "bigint", nullable: false),
                    ContactId = table.Column<long>(type: "bigint", nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: true),
                    MessageType = table.Column<int>(type: "integer", nullable: false),
                    ContentType = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SenderType = table.Column<int>(type: "integer", nullable: false),
                    Content = table.Column<string>(type: "character varying(150000)", maxLength: 150000, nullable: true),
                    IsPrivate = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    MetadataJson = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chat_messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_chat_messages_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_chat_messages_chat_contacts_ContactId",
                        column: x => x.ContactId,
                        principalTable: "chat_contacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_chat_messages_chat_conversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "chat_conversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_chat_messages_chat_inboxes_InboxId",
                        column: x => x.InboxId,
                        principalTable: "chat_inboxes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "chat_attachments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MessageId = table.Column<long>(type: "bigint", nullable: false),
                    MediaFileId = table.Column<long>(type: "bigint", nullable: true),
                    FileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chat_attachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_chat_attachments_MediaFiles_MediaFileId",
                        column: x => x.MediaFileId,
                        principalTable: "MediaFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_chat_attachments_chat_messages_MessageId",
                        column: x => x.MessageId,
                        principalTable: "chat_messages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_chat_attachments_MediaFileId",
                table: "chat_attachments",
                column: "MediaFileId");

            migrationBuilder.CreateIndex(
                name: "IX_chat_attachments_MessageId",
                table: "chat_attachments",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_chat_contact_inboxes_ContactId_InboxId",
                table: "chat_contact_inboxes",
                columns: new[] { "ContactId", "InboxId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_chat_contact_inboxes_InboxId",
                table: "chat_contact_inboxes",
                column: "InboxId");

            migrationBuilder.CreateIndex(
                name: "IX_chat_contacts_Email",
                table: "chat_contacts",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_chat_contacts_Phone",
                table: "chat_contacts",
                column: "Phone");

            migrationBuilder.CreateIndex(
                name: "IX_chat_contacts_UserId",
                table: "chat_contacts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_chat_conversation_labels_LabelId",
                table: "chat_conversation_labels",
                column: "LabelId");

            migrationBuilder.CreateIndex(
                name: "IX_chat_conversations_AssignedUserId",
                table: "chat_conversations",
                column: "AssignedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_chat_conversations_AssignedUserId_Status_LastMessageAt",
                table: "chat_conversations",
                columns: new[] { "AssignedUserId", "Status", "LastMessageAt" });

            migrationBuilder.CreateIndex(
                name: "IX_chat_conversations_ContactId",
                table: "chat_conversations",
                column: "ContactId");

            migrationBuilder.CreateIndex(
                name: "IX_chat_conversations_CreatedAt",
                table: "chat_conversations",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_chat_conversations_InboxId",
                table: "chat_conversations",
                column: "InboxId");

            migrationBuilder.CreateIndex(
                name: "IX_chat_conversations_InboxId_Status_LastMessageAt",
                table: "chat_conversations",
                columns: new[] { "InboxId", "Status", "LastMessageAt" });

            migrationBuilder.CreateIndex(
                name: "IX_chat_conversations_LastMessageAt",
                table: "chat_conversations",
                column: "LastMessageAt");

            migrationBuilder.CreateIndex(
                name: "IX_chat_conversations_Priority",
                table: "chat_conversations",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_chat_conversations_Status",
                table: "chat_conversations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_chat_conversations_TeamId",
                table: "chat_conversations",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_chat_conversations_TeamId_Status_LastMessageAt",
                table: "chat_conversations",
                columns: new[] { "TeamId", "Status", "LastMessageAt" });

            migrationBuilder.CreateIndex(
                name: "IX_chat_inboxes_ChannelType",
                table: "chat_inboxes",
                column: "ChannelType");

            migrationBuilder.CreateIndex(
                name: "IX_chat_inboxes_IsActive",
                table: "chat_inboxes",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_chat_inboxes_IsActive_ChannelType",
                table: "chat_inboxes",
                columns: new[] { "IsActive", "ChannelType" });

            migrationBuilder.CreateIndex(
                name: "IX_chat_labels_Name",
                table: "chat_labels",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_chat_messages_ContactId",
                table: "chat_messages",
                column: "ContactId");

            migrationBuilder.CreateIndex(
                name: "IX_chat_messages_ConversationId",
                table: "chat_messages",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_chat_messages_ConversationId_CreatedAt",
                table: "chat_messages",
                columns: new[] { "ConversationId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_chat_messages_ConversationId_IsPrivate_CreatedAt",
                table: "chat_messages",
                columns: new[] { "ConversationId", "IsPrivate", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_chat_messages_CreatedAt",
                table: "chat_messages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_chat_messages_InboxId",
                table: "chat_messages",
                column: "InboxId");

            migrationBuilder.CreateIndex(
                name: "IX_chat_messages_InboxId_CreatedAt",
                table: "chat_messages",
                columns: new[] { "InboxId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_chat_messages_UserId",
                table: "chat_messages",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_chat_team_members_UserId",
                table: "chat_team_members",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_chat_teams_Name",
                table: "chat_teams",
                column: "Name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "chat_attachments");

            migrationBuilder.DropTable(
                name: "chat_contact_inboxes");

            migrationBuilder.DropTable(
                name: "chat_conversation_labels");

            migrationBuilder.DropTable(
                name: "chat_team_members");

            migrationBuilder.DropTable(
                name: "chat_messages");

            migrationBuilder.DropTable(
                name: "chat_labels");

            migrationBuilder.DropTable(
                name: "chat_conversations");

            migrationBuilder.DropTable(
                name: "chat_contacts");

            migrationBuilder.DropTable(
                name: "chat_inboxes");

            migrationBuilder.DropTable(
                name: "chat_teams");
        }
    }
}
