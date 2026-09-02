import { TestBed } from '@angular/core/testing';
import { PaginaInicio } from './pagina-inicio';

describe('PaginaInicio', () => {
  it('debe mostrar el propósito principal del sistema', () => {
    const fixture = TestBed.createComponent(PaginaInicio);
    fixture.detectChanges();

    const elemento = fixture.nativeElement as HTMLElement;
    expect(elemento.querySelector('h1')?.textContent).toContain(
      'Gestión y versionado de configuraciones Cisco',
    );
  });
});
