using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DigitRecognitionLibrary.Migrations
{
    public partial class init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ImageDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Image = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImageDetails", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LabelObjs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Label = table.Column<int>(type: "INTEGER", nullable: false),
                    StatCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabelObjs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ImageObjs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Path = table.Column<string>(type: "TEXT", nullable: true),
                    ImageDetailsId = table.Column<int>(type: "INTEGER", nullable: true),
                    Confidence = table.Column<float>(type: "REAL", nullable: false),
                    LabelObjectId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImageObjs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImageObjs_ImageDetails_ImageDetailsId",
                        column: x => x.ImageDetailsId,
                        principalTable: "ImageDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ImageObjs_LabelObjs_LabelObjectId",
                        column: x => x.LabelObjectId,
                        principalTable: "LabelObjs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ImageObjs_ImageDetailsId",
                table: "ImageObjs",
                column: "ImageDetailsId");

            migrationBuilder.CreateIndex(
                name: "IX_ImageObjs_LabelObjectId",
                table: "ImageObjs",
                column: "LabelObjectId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImageObjs");

            migrationBuilder.DropTable(
                name: "ImageDetails");

            migrationBuilder.DropTable(
                name: "LabelObjs");
        }
    }
}
