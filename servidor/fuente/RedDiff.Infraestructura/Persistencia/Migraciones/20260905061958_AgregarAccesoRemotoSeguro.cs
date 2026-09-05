using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RedDiff.Infraestructura.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class AgregarAccesoRemotoSeguro : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "acceso_configurado_en",
                schema: "reddiff",
                table: "dispositivo",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "algoritmo_clave_host",
                schema: "reddiff",
                table: "dispositivo",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "huella_clave_host",
                schema: "reddiff",
                table: "dispositivo",
                type: "character(64)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "secreto_acceso_protegido",
                schema: "reddiff",
                table: "dispositivo",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "usuario_acceso",
                schema: "reddiff",
                table: "dispositivo",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_dispositivo_acceso_remoto",
                schema: "reddiff",
                table: "dispositivo",
                sql: "(usuario_acceso IS NULL AND secreto_acceso_protegido IS NULL AND algoritmo_clave_host IS NULL AND huella_clave_host IS NULL AND acceso_configurado_en IS NULL) OR (usuario_acceso IS NOT NULL AND secreto_acceso_protegido IS NOT NULL AND algoritmo_clave_host IS NOT NULL AND huella_clave_host IS NOT NULL AND acceso_configurado_en IS NOT NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_dispositivo_acceso_remoto",
                schema: "reddiff",
                table: "dispositivo");

            migrationBuilder.DropColumn(
                name: "acceso_configurado_en",
                schema: "reddiff",
                table: "dispositivo");

            migrationBuilder.DropColumn(
                name: "algoritmo_clave_host",
                schema: "reddiff",
                table: "dispositivo");

            migrationBuilder.DropColumn(
                name: "huella_clave_host",
                schema: "reddiff",
                table: "dispositivo");

            migrationBuilder.DropColumn(
                name: "secreto_acceso_protegido",
                schema: "reddiff",
                table: "dispositivo");

            migrationBuilder.DropColumn(
                name: "usuario_acceso",
                schema: "reddiff",
                table: "dispositivo");
        }
    }
}
