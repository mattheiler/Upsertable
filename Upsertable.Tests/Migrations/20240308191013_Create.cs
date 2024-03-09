using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Upsertable.SqlServer.Tests.Migrations
{
    /// <inheritdoc />
    public partial class Create : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Baz",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Baz", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Foos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Zot = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Foos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Qux",
                columns: table => new
                {
                    BazId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Qux", x => x.BazId);
                    table.ForeignKey(
                        name: "FK_Qux_Baz_BazId",
                        column: x => x.BazId,
                        principalTable: "Baz",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Ack",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Bar_Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Bar_Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FooId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ack", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ack_Foos_FooId",
                        column: x => x.FooId,
                        principalTable: "Foos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Fub",
                columns: table => new
                {
                    FooId = table.Column<int>(type: "int", nullable: false),
                    BazId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fub", x => new { x.FooId, x.BazId });
                    table.ForeignKey(
                        name: "FK_Fub_Baz_BazId",
                        column: x => x.BazId,
                        principalTable: "Baz",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Fub_Foos_FooId",
                        column: x => x.FooId,
                        principalTable: "Foos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Fum",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OwnerId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fum", x => new { x.OwnerId, x.Id });
                    table.ForeignKey(
                        name: "FK_Fum_Qux_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Qux",
                        principalColumn: "BazId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Ack_FooId",
                table: "Ack",
                column: "FooId");

            migrationBuilder.CreateIndex(
                name: "IX_Fub_BazId",
                table: "Fub",
                column: "BazId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Ack");

            migrationBuilder.DropTable(
                name: "Fub");

            migrationBuilder.DropTable(
                name: "Fum");

            migrationBuilder.DropTable(
                name: "Foos");

            migrationBuilder.DropTable(
                name: "Qux");

            migrationBuilder.DropTable(
                name: "Baz");
        }
    }
}
