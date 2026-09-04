using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RedDiff.Infraestructura.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "reddiff");

            migrationBuilder.CreateTable(
                name: "dispositivo",
                schema: "reddiff",
                columns: table => new
                {
                    dispositivo_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    host = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    tipo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    modelo = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    protocolo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    puerto = table.Column<int>(type: "integer", nullable: false),
                    fuente_eventos = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    estado = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dispositivo", x => x.dispositivo_id);
                });

            migrationBuilder.CreateTable(
                name: "rol",
                schema: "reddiff",
                columns: table => new
                {
                    rol_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    estado = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rol", x => x.rol_id);
                });

            migrationBuilder.CreateTable(
                name: "evento_cambio",
                schema: "reddiff",
                columns: table => new
                {
                    evento_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    dispositivo_id = table.Column<long>(type: "bigint", nullable: false),
                    fuente = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    tipo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    fecha = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    huella = table.Column<string>(type: "character(64)", nullable: false),
                    contenido_evento = table.Column<string>(type: "text", nullable: false),
                    estado = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_evento_cambio", x => x.evento_id);
                    table.ForeignKey(
                        name: "fk_evento_cambio_dispositivo",
                        column: x => x.dispositivo_id,
                        principalSchema: "reddiff",
                        principalTable: "dispositivo",
                        principalColumn: "dispositivo_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "usuario",
                schema: "reddiff",
                columns: table => new
                {
                    usuario_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    rol_id = table.Column<long>(type: "bigint", nullable: false),
                    usuario = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    contrasena_hash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    estado = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_usuario", x => x.usuario_id);
                    table.ForeignKey(
                        name: "fk_usuario_rol",
                        column: x => x.rol_id,
                        principalSchema: "reddiff",
                        principalTable: "rol",
                        principalColumn: "rol_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "auditoria",
                schema: "reddiff",
                columns: table => new
                {
                    auditoria_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuario_id = table.Column<long>(type: "bigint", nullable: true),
                    accion = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    entidad = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    entidad_id = table.Column<long>(type: "bigint", nullable: true),
                    fecha = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    estado = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    detalle = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_auditoria", x => x.auditoria_id);
                    table.ForeignKey(
                        name: "fk_auditoria_usuario",
                        column: x => x.usuario_id,
                        principalSchema: "reddiff",
                        principalTable: "usuario",
                        principalColumn: "usuario_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "captura",
                schema: "reddiff",
                columns: table => new
                {
                    captura_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    dispositivo_id = table.Column<long>(type: "bigint", nullable: false),
                    evento_id = table.Column<long>(type: "bigint", nullable: true),
                    usuario_id = table.Column<long>(type: "bigint", nullable: true),
                    disparador = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    protocolo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    estado = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    fecha = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_captura", x => x.captura_id);
                    table.CheckConstraint("ck_captura_origen", "(disparador = 'BajoDemanda' AND usuario_id IS NOT NULL AND evento_id IS NULL) OR (disparador = 'Evento' AND usuario_id IS NULL AND evento_id IS NOT NULL)");
                    table.ForeignKey(
                        name: "fk_captura_dispositivo",
                        column: x => x.dispositivo_id,
                        principalSchema: "reddiff",
                        principalTable: "dispositivo",
                        principalColumn: "dispositivo_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_captura_evento_cambio",
                        column: x => x.evento_id,
                        principalSchema: "reddiff",
                        principalTable: "evento_cambio",
                        principalColumn: "evento_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_captura_usuario",
                        column: x => x.usuario_id,
                        principalSchema: "reddiff",
                        principalTable: "usuario",
                        principalColumn: "usuario_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "version_config",
                schema: "reddiff",
                columns: table => new
                {
                    version_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    dispositivo_id = table.Column<long>(type: "bigint", nullable: false),
                    captura_id = table.Column<long>(type: "bigint", nullable: false),
                    numero = table.Column<int>(type: "integer", nullable: false),
                    origen = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    contenido = table.Column<string>(type: "text", nullable: false),
                    hash = table.Column<string>(type: "character(64)", nullable: false),
                    capturada_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    comentario = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    estado = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    estable = table.Column<bool>(type: "boolean", nullable: false),
                    validada_por_usuario_id = table.Column<long>(type: "bigint", nullable: true),
                    validada_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_version_config", x => x.version_id);
                    table.CheckConstraint("ck_version_config_estable", "(estable = FALSE AND validada_por_usuario_id IS NULL AND validada_en IS NULL) OR (estable = TRUE AND validada_por_usuario_id IS NOT NULL AND validada_en IS NOT NULL)");
                    table.ForeignKey(
                        name: "fk_version_config_captura",
                        column: x => x.captura_id,
                        principalSchema: "reddiff",
                        principalTable: "captura",
                        principalColumn: "captura_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_version_config_dispositivo",
                        column: x => x.dispositivo_id,
                        principalSchema: "reddiff",
                        principalTable: "dispositivo",
                        principalColumn: "dispositivo_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_version_config_usuario_validador",
                        column: x => x.validada_por_usuario_id,
                        principalSchema: "reddiff",
                        principalTable: "usuario",
                        principalColumn: "usuario_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "baseline",
                schema: "reddiff",
                columns: table => new
                {
                    baseline_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    dispositivo_id = table.Column<long>(type: "bigint", nullable: true),
                    tipo_dispositivo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    version_id = table.Column<long>(type: "bigint", nullable: true),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    activa = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_baseline", x => x.baseline_id);
                    table.CheckConstraint("ck_baseline_destino", "(dispositivo_id IS NOT NULL AND tipo_dispositivo IS NULL) OR (dispositivo_id IS NULL AND tipo_dispositivo IS NOT NULL)");
                    table.ForeignKey(
                        name: "fk_baseline_dispositivo",
                        column: x => x.dispositivo_id,
                        principalSchema: "reddiff",
                        principalTable: "dispositivo",
                        principalColumn: "dispositivo_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_baseline_version_config",
                        column: x => x.version_id,
                        principalSchema: "reddiff",
                        principalTable: "version_config",
                        principalColumn: "version_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "comparacion",
                schema: "reddiff",
                columns: table => new
                {
                    comparacion_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    version_origen_id = table.Column<long>(type: "bigint", nullable: false),
                    version_destino_id = table.Column<long>(type: "bigint", nullable: false),
                    usuario_id = table.Column<long>(type: "bigint", nullable: false),
                    fecha = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    estado = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_comparacion", x => x.comparacion_id);
                    table.CheckConstraint("ck_comparacion_versiones_diferentes", "version_origen_id <> version_destino_id");
                    table.ForeignKey(
                        name: "fk_comparacion_usuario",
                        column: x => x.usuario_id,
                        principalSchema: "reddiff",
                        principalTable: "usuario",
                        principalColumn: "usuario_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_comparacion_version_destino",
                        column: x => x.version_destino_id,
                        principalSchema: "reddiff",
                        principalTable: "version_config",
                        principalColumn: "version_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_comparacion_version_origen",
                        column: x => x.version_origen_id,
                        principalSchema: "reddiff",
                        principalTable: "version_config",
                        principalColumn: "version_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "regla_baseline",
                schema: "reddiff",
                columns: table => new
                {
                    regla_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    baseline_id = table.Column<long>(type: "bigint", nullable: false),
                    criterio = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    esperado = table.Column<string>(type: "text", nullable: false),
                    obligatorio = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_regla_baseline", x => x.regla_id);
                    table.ForeignKey(
                        name: "fk_regla_baseline_baseline",
                        column: x => x.baseline_id,
                        principalSchema: "reddiff",
                        principalTable: "baseline",
                        principalColumn: "baseline_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "verificacion",
                schema: "reddiff",
                columns: table => new
                {
                    verificacion_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    baseline_id = table.Column<long>(type: "bigint", nullable: false),
                    version_id = table.Column<long>(type: "bigint", nullable: false),
                    usuario_id = table.Column<long>(type: "bigint", nullable: false),
                    fecha = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    estado = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_verificacion", x => x.verificacion_id);
                    table.ForeignKey(
                        name: "fk_verificacion_baseline",
                        column: x => x.baseline_id,
                        principalSchema: "reddiff",
                        principalTable: "baseline",
                        principalColumn: "baseline_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_verificacion_usuario",
                        column: x => x.usuario_id,
                        principalSchema: "reddiff",
                        principalTable: "usuario",
                        principalColumn: "usuario_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_verificacion_version_config",
                        column: x => x.version_id,
                        principalSchema: "reddiff",
                        principalTable: "version_config",
                        principalColumn: "version_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "detalle_diferencia",
                schema: "reddiff",
                columns: table => new
                {
                    diferencia_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    comparacion_id = table.Column<long>(type: "bigint", nullable: false),
                    linea = table.Column<int>(type: "integer", nullable: false),
                    tipo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    texto_anterior = table.Column<string>(type: "text", nullable: true),
                    texto_nuevo = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_detalle_diferencia", x => x.diferencia_id);
                    table.ForeignKey(
                        name: "fk_detalle_diferencia_comparacion",
                        column: x => x.comparacion_id,
                        principalSchema: "reddiff",
                        principalTable: "comparacion",
                        principalColumn: "comparacion_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "resultado_regla",
                schema: "reddiff",
                columns: table => new
                {
                    resultado_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    verificacion_id = table.Column<long>(type: "bigint", nullable: false),
                    regla_id = table.Column<long>(type: "bigint", nullable: false),
                    estado = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    evidencia = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_resultado_regla", x => x.resultado_id);
                    table.ForeignKey(
                        name: "fk_resultado_regla_regla_baseline",
                        column: x => x.regla_id,
                        principalSchema: "reddiff",
                        principalTable: "regla_baseline",
                        principalColumn: "regla_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_resultado_regla_verificacion",
                        column: x => x.verificacion_id,
                        principalSchema: "reddiff",
                        principalTable: "verificacion",
                        principalColumn: "verificacion_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_auditoria_entidad_fecha",
                schema: "reddiff",
                table: "auditoria",
                columns: new[] { "entidad", "entidad_id", "fecha" });

            migrationBuilder.CreateIndex(
                name: "ix_auditoria_fecha",
                schema: "reddiff",
                table: "auditoria",
                column: "fecha");

            migrationBuilder.CreateIndex(
                name: "ix_auditoria_usuario_fecha",
                schema: "reddiff",
                table: "auditoria",
                columns: new[] { "usuario_id", "fecha" });

            migrationBuilder.CreateIndex(
                name: "ix_baseline_dispositivo_activa",
                schema: "reddiff",
                table: "baseline",
                columns: new[] { "dispositivo_id", "activa" });

            migrationBuilder.CreateIndex(
                name: "ix_baseline_tipo_activa",
                schema: "reddiff",
                table: "baseline",
                columns: new[] { "tipo_dispositivo", "activa" });

            migrationBuilder.CreateIndex(
                name: "ix_baseline_version_config",
                schema: "reddiff",
                table: "baseline",
                column: "version_id");

            migrationBuilder.CreateIndex(
                name: "ix_captura_dispositivo_disparador_fecha",
                schema: "reddiff",
                table: "captura",
                columns: new[] { "dispositivo_id", "disparador", "fecha" });

            migrationBuilder.CreateIndex(
                name: "ix_captura_dispositivo_fecha",
                schema: "reddiff",
                table: "captura",
                columns: new[] { "dispositivo_id", "fecha" });

            migrationBuilder.CreateIndex(
                name: "ix_captura_estado",
                schema: "reddiff",
                table: "captura",
                column: "estado");

            migrationBuilder.CreateIndex(
                name: "ix_captura_usuario",
                schema: "reddiff",
                table: "captura",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "ux_captura_evento",
                schema: "reddiff",
                table: "captura",
                column: "evento_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_comparacion_usuario_fecha",
                schema: "reddiff",
                table: "comparacion",
                columns: new[] { "usuario_id", "fecha" });

            migrationBuilder.CreateIndex(
                name: "ix_comparacion_version_destino",
                schema: "reddiff",
                table: "comparacion",
                column: "version_destino_id");

            migrationBuilder.CreateIndex(
                name: "ix_comparacion_versiones_fecha",
                schema: "reddiff",
                table: "comparacion",
                columns: new[] { "version_origen_id", "version_destino_id", "fecha" });

            migrationBuilder.CreateIndex(
                name: "ix_detalle_diferencia_comparacion_linea",
                schema: "reddiff",
                table: "detalle_diferencia",
                columns: new[] { "comparacion_id", "linea" });

            migrationBuilder.CreateIndex(
                name: "ux_dispositivo_host_puerto",
                schema: "reddiff",
                table: "dispositivo",
                columns: new[] { "host", "puerto" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_dispositivo_nombre",
                schema: "reddiff",
                table: "dispositivo",
                column: "nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_evento_cambio_estado_fecha",
                schema: "reddiff",
                table: "evento_cambio",
                columns: new[] { "estado", "fecha" });

            migrationBuilder.CreateIndex(
                name: "ux_evento_cambio_dispositivo_huella",
                schema: "reddiff",
                table: "evento_cambio",
                columns: new[] { "dispositivo_id", "huella" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_regla_baseline_baseline",
                schema: "reddiff",
                table: "regla_baseline",
                column: "baseline_id");

            migrationBuilder.CreateIndex(
                name: "ix_resultado_regla_regla_baseline",
                schema: "reddiff",
                table: "resultado_regla",
                column: "regla_id");

            migrationBuilder.CreateIndex(
                name: "ux_resultado_regla_verificacion_regla",
                schema: "reddiff",
                table: "resultado_regla",
                columns: new[] { "verificacion_id", "regla_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_rol_nombre",
                schema: "reddiff",
                table: "rol",
                column: "nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_usuario_rol",
                schema: "reddiff",
                table: "usuario",
                column: "rol_id");

            migrationBuilder.CreateIndex(
                name: "ux_usuario_nombre",
                schema: "reddiff",
                table: "usuario",
                column: "usuario",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_verificacion_baseline_version_fecha",
                schema: "reddiff",
                table: "verificacion",
                columns: new[] { "baseline_id", "version_id", "fecha" });

            migrationBuilder.CreateIndex(
                name: "ix_verificacion_usuario_fecha",
                schema: "reddiff",
                table: "verificacion",
                columns: new[] { "usuario_id", "fecha" });

            migrationBuilder.CreateIndex(
                name: "ix_verificacion_version_config",
                schema: "reddiff",
                table: "verificacion",
                column: "version_id");

            migrationBuilder.CreateIndex(
                name: "ix_version_config_dispositivo_estado_fecha",
                schema: "reddiff",
                table: "version_config",
                columns: new[] { "dispositivo_id", "estado", "capturada_en" });

            migrationBuilder.CreateIndex(
                name: "ix_version_config_dispositivo_fecha",
                schema: "reddiff",
                table: "version_config",
                columns: new[] { "dispositivo_id", "capturada_en" });

            migrationBuilder.CreateIndex(
                name: "ix_version_config_dispositivo_origen_fecha",
                schema: "reddiff",
                table: "version_config",
                columns: new[] { "dispositivo_id", "origen", "capturada_en" });

            migrationBuilder.CreateIndex(
                name: "ix_version_config_hash",
                schema: "reddiff",
                table: "version_config",
                column: "hash");

            migrationBuilder.CreateIndex(
                name: "ix_version_config_usuario_validador",
                schema: "reddiff",
                table: "version_config",
                column: "validada_por_usuario_id");

            migrationBuilder.CreateIndex(
                name: "ux_version_config_captura",
                schema: "reddiff",
                table: "version_config",
                column: "captura_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_version_config_dispositivo_numero",
                schema: "reddiff",
                table: "version_config",
                columns: new[] { "dispositivo_id", "numero" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "auditoria",
                schema: "reddiff");

            migrationBuilder.DropTable(
                name: "detalle_diferencia",
                schema: "reddiff");

            migrationBuilder.DropTable(
                name: "resultado_regla",
                schema: "reddiff");

            migrationBuilder.DropTable(
                name: "comparacion",
                schema: "reddiff");

            migrationBuilder.DropTable(
                name: "regla_baseline",
                schema: "reddiff");

            migrationBuilder.DropTable(
                name: "verificacion",
                schema: "reddiff");

            migrationBuilder.DropTable(
                name: "baseline",
                schema: "reddiff");

            migrationBuilder.DropTable(
                name: "version_config",
                schema: "reddiff");

            migrationBuilder.DropTable(
                name: "captura",
                schema: "reddiff");

            migrationBuilder.DropTable(
                name: "evento_cambio",
                schema: "reddiff");

            migrationBuilder.DropTable(
                name: "usuario",
                schema: "reddiff");

            migrationBuilder.DropTable(
                name: "dispositivo",
                schema: "reddiff");

            migrationBuilder.DropTable(
                name: "rol",
                schema: "reddiff");
        }
    }
}
