import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { finalize, forkJoin } from 'rxjs';
import { obtenerMensajeError } from '../../nucleo/http/modelo-error-api';
import { ServicioSesion } from '../../nucleo/identidad/servicio-sesion';
import { RolResumen, UsuarioResumen } from './modelos-usuarios';
import { ServicioUsuarios } from './servicio-usuarios';

@Component({
  selector: 'rd-pagina-usuarios',
  imports: [ReactiveFormsModule],
  templateUrl: './pagina-usuarios.html',
  styleUrl: './pagina-usuarios.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PaginaUsuarios implements OnInit {
  private readonly constructorFormulario = inject(FormBuilder);
  private readonly servicioUsuarios = inject(ServicioUsuarios);
  private readonly enrutador = inject(Router);
  protected readonly servicioSesion = inject(ServicioSesion);

  protected readonly usuarios = signal<UsuarioResumen[]>([]);
  protected readonly roles = signal<RolResumen[]>([]);
  protected readonly cargando = signal(true);
  protected readonly guardando = signal(false);
  protected readonly cambiandoEstado = signal(false);
  protected readonly formularioVisible = signal(false);
  protected readonly usuarioEditado = signal<UsuarioResumen | null>(null);
  protected readonly usuarioPorCambiar = signal<UsuarioResumen | null>(null);
  protected readonly mensajeError = signal<string | null>(null);
  protected readonly mensajeExito = signal<string | null>(null);
  protected readonly totalActivos = computed(
    () => this.usuarios().filter((usuario) => usuario.activo).length,
  );

  protected readonly formulario = this.constructorFormulario.nonNullable.group({
    nombreUsuario: [
      '',
      [
        Validators.required,
        Validators.minLength(3),
        Validators.maxLength(100),
        Validators.pattern(/^[\p{L}\p{N}._-]+$/u),
      ],
    ],
    rolId: [0, [Validators.required, Validators.min(1)]],
    contrasena: ['', [Validators.maxLength(128)]],
  });

  ngOnInit(): void {
    this.cargarDatos();
  }

  protected abrirNuevoUsuario(): void {
    this.limpiarMensajes();
    this.usuarioEditado.set(null);
    this.configurarValidacionContrasena(true);
    this.formulario.controls.rolId.enable();
    this.formulario.reset({
      nombreUsuario: '',
      rolId: this.roles().find((rol) => rol.activo)?.id ?? 0,
      contrasena: '',
    });
    this.formularioVisible.set(true);
  }

  protected abrirEdicion(usuario: UsuarioResumen): void {
    this.limpiarMensajes();
    this.usuarioEditado.set(usuario);
    this.configurarValidacionContrasena(false);
    if (usuario.id === this.servicioSesion.sesion()?.usuarioId) {
      this.formulario.controls.rolId.disable();
    } else {
      this.formulario.controls.rolId.enable();
    }
    this.formulario.reset({
      nombreUsuario: usuario.nombreUsuario,
      rolId: usuario.rolId,
      contrasena: '',
    });
    this.formularioVisible.set(true);
  }

  protected cerrarFormulario(): void {
    if (!this.guardando()) {
      this.formulario.controls.contrasena.reset();
      this.formularioVisible.set(false);
      this.usuarioEditado.set(null);
      this.mensajeError.set(null);
    }
  }

  protected guardarUsuario(): void {
    this.mensajeError.set(null);
    if (this.formulario.invalid) {
      this.formulario.markAllAsTouched();
      return;
    }

    const valores = this.formulario.getRawValue();
    const editado = this.usuarioEditado();
    const requiereNuevaSesion =
      editado !== null &&
      editado.id === this.servicioSesion.sesion()?.usuarioId &&
      (editado.nombreUsuario !== valores.nombreUsuario.trim().toLowerCase() ||
        Boolean(valores.contrasena));
    this.guardando.set(true);

    const operacion = editado
      ? this.servicioUsuarios.actualizarUsuario(editado.id, {
          nombreUsuario: valores.nombreUsuario,
          rolId: valores.rolId,
          nuevaContrasena: valores.contrasena || null,
        })
      : this.servicioUsuarios.crearUsuario({
          nombreUsuario: valores.nombreUsuario,
          rolId: valores.rolId,
          contrasena: valores.contrasena,
        });

    operacion.pipe(finalize(() => this.guardando.set(false))).subscribe({
      next: (usuario) => {
        this.actualizarUsuarioEnLista(usuario);
        this.formulario.controls.contrasena.reset();
        this.formularioVisible.set(false);
        this.usuarioEditado.set(null);
        if (requiereNuevaSesion) {
          this.servicioSesion.descartarSesionLocal();
          void this.enrutador.navigate(['/iniciar-sesion'], {
            queryParams: { cuentaActualizada: true },
          });
          return;
        }

        this.mensajeExito.set(
          editado
            ? 'El usuario fue actualizado correctamente.'
            : 'El usuario fue creado correctamente.',
        );
      },
      error: (error: unknown) =>
        this.mensajeError.set(
          obtenerMensajeError(error, 'No fue posible guardar el usuario. Inténtalo de nuevo.'),
        ),
    });
  }

  protected solicitarCambioEstado(usuario: UsuarioResumen): void {
    if (usuario.id === this.servicioSesion.sesion()?.usuarioId && usuario.activo) {
      return;
    }

    this.limpiarMensajes();
    this.usuarioPorCambiar.set(usuario);
  }

  protected cancelarCambioEstado(): void {
    if (!this.cambiandoEstado()) {
      this.usuarioPorCambiar.set(null);
    }
  }

  protected confirmarCambioEstado(): void {
    const usuario = this.usuarioPorCambiar();
    if (!usuario) {
      return;
    }

    const nuevoEstado = !usuario.activo;
    this.cambiandoEstado.set(true);
    this.servicioUsuarios
      .cambiarEstado(usuario.id, nuevoEstado)
      .pipe(finalize(() => this.cambiandoEstado.set(false)))
      .subscribe({
        next: (actualizado) => {
          this.actualizarUsuarioEnLista(actualizado);
          this.usuarioPorCambiar.set(null);
          this.mensajeExito.set(
            nuevoEstado ? 'El usuario fue activado.' : 'El usuario fue desactivado.',
          );
        },
        error: (error: unknown) => {
          this.usuarioPorCambiar.set(null);
          this.mensajeError.set(
            obtenerMensajeError(
              error,
              `No fue posible ${nuevoEstado ? 'activar' : 'desactivar'} el usuario.`,
            ),
          );
        },
      });
  }

  protected reintentarCarga(): void {
    this.cargarDatos();
  }

  private cargarDatos(): void {
    this.cargando.set(true);
    this.limpiarMensajes();
    forkJoin({
      usuarios: this.servicioUsuarios.listarUsuarios(),
      roles: this.servicioUsuarios.listarRoles(),
    })
      .pipe(finalize(() => this.cargando.set(false)))
      .subscribe({
        next: ({ usuarios, roles }) => {
          this.usuarios.set(usuarios);
          this.roles.set(roles);
        },
        error: (error: unknown) =>
          this.mensajeError.set(
            obtenerMensajeError(error, 'No fue posible cargar los usuarios y roles.'),
          ),
      });
  }

  private actualizarUsuarioEnLista(actualizado: UsuarioResumen): void {
    this.usuarios.update((usuarios) => {
      const existe = usuarios.some((usuario) => usuario.id === actualizado.id);
      const resultado = existe
        ? usuarios.map((usuario) => (usuario.id === actualizado.id ? actualizado : usuario))
        : [...usuarios, actualizado];

      return resultado.sort((primero, segundo) =>
        primero.nombreUsuario.localeCompare(segundo.nombreUsuario),
      );
    });
  }

  private configurarValidacionContrasena(esNuevo: boolean): void {
    const validadores = [Validators.minLength(12), Validators.maxLength(128)];
    this.formulario.controls.contrasena.setValidators(
      esNuevo ? [Validators.required, ...validadores] : validadores,
    );
    this.formulario.controls.contrasena.updateValueAndValidity();
  }

  private limpiarMensajes(): void {
    this.mensajeError.set(null);
    this.mensajeExito.set(null);
  }
}
