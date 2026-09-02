import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

@Component({
  selector: 'rd-aplicacion',
  imports: [RouterOutlet],
  templateUrl: './aplicacion.html',
  styleUrl: './aplicacion.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Aplicacion {}
