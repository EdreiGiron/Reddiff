import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Aplicacion } from './aplicacion';

describe('Aplicacion', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Aplicacion],
      providers: [provideRouter([])],
    }).compileComponents();
  });

  it('debe crearse', () => {
    const componente = TestBed.createComponent(Aplicacion).componentInstance;

    expect(componente).toBeTruthy();
  });

  it('debe incluir el punto de salida del enrutador', () => {
    const fixture = TestBed.createComponent(Aplicacion);
    fixture.detectChanges();

    const elemento = fixture.nativeElement as HTMLElement;
    expect(elemento.querySelector('router-outlet')).not.toBeNull();
  });
});
