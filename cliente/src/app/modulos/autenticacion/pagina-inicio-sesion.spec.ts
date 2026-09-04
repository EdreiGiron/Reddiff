import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { PaginaInicioSesion } from './pagina-inicio-sesion';

describe('PaginaInicioSesion', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [PaginaInicioSesion],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    });
  });

  it('debe solicitar usuario y contraseña sin exponer la contraseña', () => {
    const fixture = TestBed.createComponent(PaginaInicioSesion);
    fixture.detectChanges();

    const elemento = fixture.nativeElement as HTMLElement;
    const contrasena = elemento.querySelector<HTMLInputElement>('#contrasena');

    expect(elemento.querySelector('h2')?.textContent).toContain('Iniciar sesión');
    expect(contrasena?.type).toBe('password');
    expect(contrasena?.getAttribute('autocomplete')).toBe('current-password');
  });

  it('debe mostrar validaciones al enviar un formulario vacío', () => {
    const fixture = TestBed.createComponent(PaginaInicioSesion);
    fixture.detectChanges();

    const formulario = fixture.nativeElement.querySelector('form') as HTMLFormElement;
    formulario.dispatchEvent(new Event('submit'));
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Ingresa un nombre de usuario válido');
    expect(fixture.nativeElement.textContent).toContain('al menos 12 caracteres');
  });
});
