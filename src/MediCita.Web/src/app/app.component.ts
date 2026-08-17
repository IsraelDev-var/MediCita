import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { BarraSuperiorComponent } from './componentes/barra-superior.component';
import { AutenticacionService } from './nucleo/autenticacion.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, BarraSuperiorComponent],
  template: `
    @if (autenticado()) {
      <mc-barra-superior />
    }
    <router-outlet />
  `,
})
export class AppComponent {
  readonly autenticado = inject(AutenticacionService).autenticado;
}
