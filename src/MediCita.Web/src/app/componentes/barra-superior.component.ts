import { Component, computed, inject } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AutenticacionService } from '../nucleo/autenticacion.service';

interface Enlace {
  ruta: string;
  texto: string;
}

/** Barra de navegación de los mockups: enlaces por rol y la ficha del usuario a la derecha. */
@Component({
  selector: 'mc-barra-superior',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
  template: `
    <header class="barra">
      <div class="interior">
        <a class="marca" [routerLink]="inicio()">MediCita</a>

        <nav>
          @for (enlace of enlaces(); track enlace.ruta) {
            <a [routerLink]="enlace.ruta" routerLinkActive="activo">{{ enlace.texto }}</a>
          }
        </nav>

        <div class="fila">
          <span class="ficha">{{ usuario()?.nombreCompleto }} · {{ usuario()?.rolNombre }}</span>
          <button type="button" class="boton-fantasma" (click)="salir()">Salir</button>
        </div>
      </div>
    </header>
  `,
  styles: [
    `
      .barra {
        background: var(--superficie);
        border-bottom: 1px solid var(--borde);
      }

      .interior {
        max-width: var(--ancho);
        margin: 0 auto;
        padding: 14px 24px;
        display: flex;
        align-items: center;
        gap: 24px;
        flex-wrap: wrap;
      }

      .marca {
        font-size: 20px;
        font-weight: 600;
        color: var(--texto);
        letter-spacing: -0.02em;
      }

      .marca:hover {
        text-decoration: none;
      }

      nav {
        display: flex;
        gap: 22px;
        margin-left: auto;
        flex-wrap: wrap;
      }

      nav a {
        color: var(--texto);
        font-size: 15px;
        padding: 4px 0;
      }

      nav a.activo {
        color: var(--azul-texto);
        font-weight: 600;
      }

      .ficha {
        background: var(--azul-tenue);
        color: var(--azul-texto);
        padding: 6px 12px;
        border-radius: var(--radio);
        font-size: 13px;
        white-space: nowrap;
      }

      @media (max-width: 720px) {
        .interior {
          gap: 12px;
        }

        nav {
          order: 3;
          width: 100%;
          margin-left: 0;
          gap: 16px;
        }

        .ficha {
          max-width: 45vw;
          overflow: hidden;
          text-overflow: ellipsis;
        }
      }
    `,
  ],
})
export class BarraSuperiorComponent {
  private readonly autenticacion = inject(AutenticacionService);

  readonly usuario = this.autenticacion.usuario;

  readonly inicio = computed(() => {
    const rol = this.autenticacion.rol();
    return rol ? this.autenticacion.rutaInicial(rol) : '/acceso';
  });

  readonly enlaces = computed<Enlace[]>(() => {
    switch (this.autenticacion.rol()) {
      case 'Paciente':
        return [
          { ruta: '/citas/nueva', texto: 'Agendar' },
          { ruta: '/citas', texto: 'Mis citas' },
          { ruta: '/perfil', texto: 'Mi perfil' },
        ];
      case 'Medico':
        return [
          { ruta: '/medico/agenda', texto: 'Mi agenda' },
          { ruta: '/perfil', texto: 'Mi perfil' },
        ];
      case 'Administrador':
        return [
          { ruta: '/admin', texto: 'Resumen' },
          { ruta: '/perfil', texto: 'Mi perfil' },
        ];
      default:
        return [];
    }
  });

  salir(): void {
    this.autenticacion.cerrarSesion();
  }
}
