import { Routes } from '@angular/router';
import { guardRol, guardSesion } from './nucleo/guards';

/**
 * Una ruta por pantalla de los mockups. Las vistas se cargan de forma diferida
 * para que el paciente no descargue el panel de administración.
 */
export const routes: Routes = [
  {
    path: 'acceso',
    title: 'MediCita · Acceso',
    loadComponent: () => import('./paginas/acceso/acceso.component').then((m) => m.AccesoComponent),
  },
  {
    path: 'citas',
    title: 'MediCita · Mis citas',
    canActivate: [guardSesion, guardRol('Paciente')],
    loadComponent: () => import('./paginas/mis-citas/mis-citas.component').then((m) => m.MisCitasComponent),
  },
  {
    path: 'citas/nueva',
    title: 'MediCita · Agendar cita',
    canActivate: [guardSesion, guardRol('Paciente')],
    loadComponent: () => import('./paginas/agendar/agendar.component').then((m) => m.AgendarComponent),
  },
  {
    path: 'perfil',
    title: 'MediCita · Mi perfil',
    canActivate: [guardSesion],
    loadComponent: () => import('./paginas/perfil/perfil.component').then((m) => m.PerfilComponent),
  },
  {
    path: 'medico/agenda',
    title: 'MediCita · Agenda del día',
    canActivate: [guardSesion, guardRol('Medico')],
    loadComponent: () =>
      import('./paginas/agenda-medico/agenda-medico.component').then((m) => m.AgendaMedicoComponent),
  },
  {
    path: 'admin',
    title: 'MediCita · Administración',
    canActivate: [guardSesion, guardRol('Administrador')],
    loadComponent: () => import('./paginas/admin/admin.component').then((m) => m.AdminComponent),
  },
  { path: '', pathMatch: 'full', redirectTo: 'acceso' },
  { path: '**', redirectTo: 'acceso' },
];
