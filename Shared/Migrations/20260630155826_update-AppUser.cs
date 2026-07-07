using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shared.Migrations
{
    /// <inheritdoc />
    public partial class updateAppUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_security_logs",
                table: "security_logs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_audit_logs",
                table: "audit_logs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_activity_logs",
                table: "activity_logs");

            migrationBuilder.RenameTable(
                name: "security_logs",
                newName: "SecurityLogs");

            migrationBuilder.RenameTable(
                name: "audit_logs",
                newName: "AuditLogs");

            migrationBuilder.RenameTable(
                name: "activity_logs",
                newName: "ActivityLogs");

            migrationBuilder.RenameIndex(
                name: "IX_security_logs_UserId_CreatedAt",
                table: "SecurityLogs",
                newName: "IX_SecurityLogs_UserId_CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_security_logs_UserId",
                table: "SecurityLogs",
                newName: "IX_SecurityLogs_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_security_logs_IsSuccess",
                table: "SecurityLogs",
                newName: "IX_SecurityLogs_IsSuccess");

            migrationBuilder.RenameIndex(
                name: "IX_security_logs_CreatedAt",
                table: "SecurityLogs",
                newName: "IX_SecurityLogs_CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_security_logs_ActionType_IsSuccess",
                table: "SecurityLogs",
                newName: "IX_SecurityLogs_ActionType_IsSuccess");

            migrationBuilder.RenameIndex(
                name: "IX_security_logs_ActionType",
                table: "SecurityLogs",
                newName: "IX_SecurityLogs_ActionType");

            migrationBuilder.RenameIndex(
                name: "IX_audit_logs_UserId",
                table: "AuditLogs",
                newName: "IX_AuditLogs_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_audit_logs_TableName_RecordId",
                table: "AuditLogs",
                newName: "IX_AuditLogs_TableName_RecordId");

            migrationBuilder.RenameIndex(
                name: "IX_audit_logs_TableName",
                table: "AuditLogs",
                newName: "IX_AuditLogs_TableName");

            migrationBuilder.RenameIndex(
                name: "IX_audit_logs_CreatedAt",
                table: "AuditLogs",
                newName: "IX_AuditLogs_CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_audit_logs_ActionType_CreatedAt",
                table: "AuditLogs",
                newName: "IX_AuditLogs_ActionType_CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_audit_logs_ActionType",
                table: "AuditLogs",
                newName: "IX_AuditLogs_ActionType");

            migrationBuilder.RenameIndex(
                name: "IX_activity_logs_UserId",
                table: "ActivityLogs",
                newName: "IX_ActivityLogs_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_activity_logs_EntityType_EntityId",
                table: "ActivityLogs",
                newName: "IX_ActivityLogs_EntityType_EntityId");

            migrationBuilder.RenameIndex(
                name: "IX_activity_logs_CreatedAt",
                table: "ActivityLogs",
                newName: "IX_ActivityLogs_CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_activity_logs_ActivityType_CreatedAt",
                table: "ActivityLogs",
                newName: "IX_ActivityLogs_ActivityType_CreatedAt");

            migrationBuilder.AlterColumn<long>(
                name: "PermissionVersion",
                table: "Users",
                type: "bigint",
                nullable: false,
                defaultValue: 1L,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 1);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsLocked",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TokenVersion",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_SecurityLogs",
                table: "SecurityLogs",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AuditLogs",
                table: "AuditLogs",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ActivityLogs",
                table: "ActivityLogs",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_SecurityLogs",
                table: "SecurityLogs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AuditLogs",
                table: "AuditLogs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ActivityLogs",
                table: "ActivityLogs");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsLocked",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TokenVersion",
                table: "Users");

            migrationBuilder.RenameTable(
                name: "SecurityLogs",
                newName: "security_logs");

            migrationBuilder.RenameTable(
                name: "AuditLogs",
                newName: "audit_logs");

            migrationBuilder.RenameTable(
                name: "ActivityLogs",
                newName: "activity_logs");

            migrationBuilder.RenameIndex(
                name: "IX_SecurityLogs_UserId_CreatedAt",
                table: "security_logs",
                newName: "IX_security_logs_UserId_CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_SecurityLogs_UserId",
                table: "security_logs",
                newName: "IX_security_logs_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_SecurityLogs_IsSuccess",
                table: "security_logs",
                newName: "IX_security_logs_IsSuccess");

            migrationBuilder.RenameIndex(
                name: "IX_SecurityLogs_CreatedAt",
                table: "security_logs",
                newName: "IX_security_logs_CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_SecurityLogs_ActionType_IsSuccess",
                table: "security_logs",
                newName: "IX_security_logs_ActionType_IsSuccess");

            migrationBuilder.RenameIndex(
                name: "IX_SecurityLogs_ActionType",
                table: "security_logs",
                newName: "IX_security_logs_ActionType");

            migrationBuilder.RenameIndex(
                name: "IX_AuditLogs_UserId",
                table: "audit_logs",
                newName: "IX_audit_logs_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_AuditLogs_TableName_RecordId",
                table: "audit_logs",
                newName: "IX_audit_logs_TableName_RecordId");

            migrationBuilder.RenameIndex(
                name: "IX_AuditLogs_TableName",
                table: "audit_logs",
                newName: "IX_audit_logs_TableName");

            migrationBuilder.RenameIndex(
                name: "IX_AuditLogs_CreatedAt",
                table: "audit_logs",
                newName: "IX_audit_logs_CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_AuditLogs_ActionType_CreatedAt",
                table: "audit_logs",
                newName: "IX_audit_logs_ActionType_CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_AuditLogs_ActionType",
                table: "audit_logs",
                newName: "IX_audit_logs_ActionType");

            migrationBuilder.RenameIndex(
                name: "IX_ActivityLogs_UserId",
                table: "activity_logs",
                newName: "IX_activity_logs_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_ActivityLogs_EntityType_EntityId",
                table: "activity_logs",
                newName: "IX_activity_logs_EntityType_EntityId");

            migrationBuilder.RenameIndex(
                name: "IX_ActivityLogs_CreatedAt",
                table: "activity_logs",
                newName: "IX_activity_logs_CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_ActivityLogs_ActivityType_CreatedAt",
                table: "activity_logs",
                newName: "IX_activity_logs_ActivityType_CreatedAt");

            migrationBuilder.AlterColumn<int>(
                name: "PermissionVersion",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldDefaultValue: 1L);

            migrationBuilder.AddPrimaryKey(
                name: "PK_security_logs",
                table: "security_logs",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_audit_logs",
                table: "audit_logs",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_activity_logs",
                table: "activity_logs",
                column: "Id");
        }
    }
}
