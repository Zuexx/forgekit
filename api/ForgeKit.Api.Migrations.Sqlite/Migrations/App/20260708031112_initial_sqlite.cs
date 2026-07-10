using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ForgeKit.Api.Migrations.Sqlite.Migrations.App
{
    /// <inheritdoc />
    public partial class initial_sqlite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "workspace",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    workspaceCode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    workspaceName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    isActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    ownerId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    createdAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    createdBy = table.Column<string>(type: "TEXT", nullable: true),
                    updatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    version = table.Column<int>(type: "INTEGER", nullable: false),
                    isDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    deletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    deletedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pkWorkspace", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "category",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    workspaceId = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    categoryCode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    categoryName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    color = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    parentCategoryId = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    displayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    isActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    createdAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    createdBy = table.Column<string>(type: "TEXT", nullable: true),
                    updatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    version = table.Column<int>(type: "INTEGER", nullable: false),
                    isDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    deletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    deletedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pkCategory", x => x.id);
                    table.ForeignKey(
                        name: "fkCategoryCategoryParentCategoryId",
                        column: x => x.parentCategoryId,
                        principalTable: "category",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fkCategoryWorkspacesWorkspaceId",
                        column: x => x.workspaceId,
                        principalTable: "workspace",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "dailyActivitySnapshot",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    snapshotDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    workspaceId = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    todosCreated = table.Column<int>(type: "INTEGER", nullable: false),
                    todosCompleted = table.Column<int>(type: "INTEGER", nullable: false),
                    todosDeleted = table.Column<int>(type: "INTEGER", nullable: false),
                    activeMembers = table.Column<int>(type: "INTEGER", nullable: false),
                    createdAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    createdBy = table.Column<string>(type: "TEXT", nullable: true),
                    updatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    version = table.Column<int>(type: "INTEGER", nullable: false),
                    isDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    deletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    deletedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pkDailyActivitySnapshot", x => x.id);
                    table.ForeignKey(
                        name: "fkDailyActivitySnapshotWorkspacesWorkspaceId",
                        column: x => x.workspaceId,
                        principalTable: "workspace",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "label",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    workspaceId = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    labelCode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    labelName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    color = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    isActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    createdAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    createdBy = table.Column<string>(type: "TEXT", nullable: true),
                    updatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    version = table.Column<int>(type: "INTEGER", nullable: false),
                    isDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    deletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    deletedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pkLabel", x => x.id);
                    table.ForeignKey(
                        name: "fkLabelWorkspacesWorkspaceId",
                        column: x => x.workspaceId,
                        principalTable: "workspace",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "member",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    workspaceId = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    userId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    role = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    joinedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    createdAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    createdBy = table.Column<string>(type: "TEXT", nullable: true),
                    updatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    version = table.Column<int>(type: "INTEGER", nullable: false),
                    isDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    deletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    deletedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pkMember", x => x.id);
                    table.ForeignKey(
                        name: "fkMemberWorkspacesWorkspaceId",
                        column: x => x.workspaceId,
                        principalTable: "workspace",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "workspaceAnalytics",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    workspaceId = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    workspaceName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    periodStart = table.Column<DateTime>(type: "TEXT", nullable: false),
                    periodEnd = table.Column<DateTime>(type: "TEXT", nullable: false),
                    totalTodos = table.Column<int>(type: "INTEGER", nullable: false),
                    completedTodos = table.Column<int>(type: "INTEGER", nullable: false),
                    overdueTodos = table.Column<int>(type: "INTEGER", nullable: false),
                    cancelledTodos = table.Column<int>(type: "INTEGER", nullable: false),
                    averageCompletionDays = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    activeMembers = table.Column<int>(type: "INTEGER", nullable: false),
                    metricsJson = table.Column<string>(type: "text", nullable: true),
                    createdAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    createdBy = table.Column<string>(type: "TEXT", nullable: true),
                    updatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    version = table.Column<int>(type: "INTEGER", nullable: false),
                    isDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    deletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    deletedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pkWorkspaceAnalytics", x => x.id);
                    table.ForeignKey(
                        name: "fkWorkspaceAnalyticsWorkspacesWorkspaceId",
                        column: x => x.workspaceId,
                        principalTable: "workspace",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "categoryLabel",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    categoryId = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    labelId = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    createdAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    createdBy = table.Column<string>(type: "TEXT", nullable: true),
                    updatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    version = table.Column<int>(type: "INTEGER", nullable: false),
                    isDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    deletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    deletedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pkCategoryLabel", x => x.id);
                    table.ForeignKey(
                        name: "fkCategoryLabelCategoryCategoryId",
                        column: x => x.categoryId,
                        principalTable: "category",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fkCategoryLabelLabelsLabelId",
                        column: x => x.labelId,
                        principalTable: "label",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "todoItem",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    workspaceId = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    assignedToMemberId = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    categoryId = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    priority = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    currentStatus = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    dueDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    completedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    metadataJson = table.Column<string>(type: "text", nullable: true),
                    createdAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    createdBy = table.Column<string>(type: "TEXT", nullable: true),
                    updatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    version = table.Column<int>(type: "INTEGER", nullable: false),
                    isDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    deletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    deletedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pkTodoItem", x => x.id);
                    table.ForeignKey(
                        name: "fkTodoItemCategoryCategoryId",
                        column: x => x.categoryId,
                        principalTable: "category",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fkTodoItemMemberAssignedToMemberId",
                        column: x => x.assignedToMemberId,
                        principalTable: "member",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fkTodoItemWorkspaceWorkspaceId",
                        column: x => x.workspaceId,
                        principalTable: "workspace",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "todoStatusHistory",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    todoItemId = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    changedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    createdAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    createdBy = table.Column<string>(type: "TEXT", nullable: true),
                    updatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    version = table.Column<int>(type: "INTEGER", nullable: false),
                    isDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    deletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    deletedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pkTodoStatusHistory", x => x.id);
                    table.ForeignKey(
                        name: "fkTodoStatusHistoryTodoItemTodoItemId",
                        column: x => x.todoItemId,
                        principalTable: "todoItem",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ixCategoryCategoryCode",
                table: "category",
                column: "categoryCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ixCategoryParentCategoryId",
                table: "category",
                column: "parentCategoryId");

            migrationBuilder.CreateIndex(
                name: "ixCategoryWorkspaceId",
                table: "category",
                column: "workspaceId");

            migrationBuilder.CreateIndex(
                name: "ixCategoryWorkspaceIdIsDeleted",
                table: "category",
                columns: new[] { "workspaceId", "isDeleted" });

            migrationBuilder.CreateIndex(
                name: "ixCategoryLabelCategoryIdLabelId",
                table: "categoryLabel",
                columns: new[] { "categoryId", "labelId" });

            migrationBuilder.CreateIndex(
                name: "ixCategoryLabelLabelId",
                table: "categoryLabel",
                column: "labelId");

            migrationBuilder.CreateIndex(
                name: "ixDailyActivitySnapshotSnapshotDate",
                table: "dailyActivitySnapshot",
                column: "snapshotDate");

            migrationBuilder.CreateIndex(
                name: "ixDailyActivitySnapshotSnapshotDateWorkspaceId",
                table: "dailyActivitySnapshot",
                columns: new[] { "snapshotDate", "workspaceId" });

            migrationBuilder.CreateIndex(
                name: "ixDailyActivitySnapshotSnapshotDateWorkspaceIdIsDeleted",
                table: "dailyActivitySnapshot",
                columns: new[] { "snapshotDate", "workspaceId", "isDeleted" });

            migrationBuilder.CreateIndex(
                name: "ixDailyActivitySnapshotWorkspaceId",
                table: "dailyActivitySnapshot",
                column: "workspaceId");

            migrationBuilder.CreateIndex(
                name: "ixLabelLabelCode",
                table: "label",
                column: "labelCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ixLabelWorkspaceId",
                table: "label",
                column: "workspaceId");

            migrationBuilder.CreateIndex(
                name: "ixMemberUserId",
                table: "member",
                column: "userId");

            migrationBuilder.CreateIndex(
                name: "ixMemberWorkspaceIdUserId",
                table: "member",
                columns: new[] { "workspaceId", "userId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ixTodoItemAssignedToMemberId",
                table: "todoItem",
                column: "assignedToMemberId");

            migrationBuilder.CreateIndex(
                name: "ixTodoItemCategoryId",
                table: "todoItem",
                column: "categoryId");

            migrationBuilder.CreateIndex(
                name: "ixTodoItemDueDate",
                table: "todoItem",
                column: "dueDate");

            migrationBuilder.CreateIndex(
                name: "ixTodoItemWorkspaceIdCurrentStatusIsDeleted",
                table: "todoItem",
                columns: new[] { "workspaceId", "currentStatus", "isDeleted" });

            migrationBuilder.CreateIndex(
                name: "ixTodoStatusHistoryTodoItemIdTimestamp",
                table: "todoStatusHistory",
                columns: new[] { "todoItemId", "timestamp" });

            migrationBuilder.CreateIndex(
                name: "ixWorkspaceOwnerId",
                table: "workspace",
                column: "ownerId");

            migrationBuilder.CreateIndex(
                name: "ixWorkspaceWorkspaceCode",
                table: "workspace",
                column: "workspaceCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ixWorkspaceAnalyticsPeriodStartPeriodEnd",
                table: "workspaceAnalytics",
                columns: new[] { "periodStart", "periodEnd" });

            migrationBuilder.CreateIndex(
                name: "ixWorkspaceAnalyticsPeriodStartWorkspaceIdIsDeleted",
                table: "workspaceAnalytics",
                columns: new[] { "periodStart", "workspaceId", "isDeleted" });

            migrationBuilder.CreateIndex(
                name: "ixWorkspaceAnalyticsWorkspaceIdPeriodStart",
                table: "workspaceAnalytics",
                columns: new[] { "workspaceId", "periodStart" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "categoryLabel");

            migrationBuilder.DropTable(
                name: "dailyActivitySnapshot");

            migrationBuilder.DropTable(
                name: "todoStatusHistory");

            migrationBuilder.DropTable(
                name: "workspaceAnalytics");

            migrationBuilder.DropTable(
                name: "label");

            migrationBuilder.DropTable(
                name: "todoItem");

            migrationBuilder.DropTable(
                name: "category");

            migrationBuilder.DropTable(
                name: "member");

            migrationBuilder.DropTable(
                name: "workspace");
        }
    }
}
