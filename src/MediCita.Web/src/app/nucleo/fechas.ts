/** Utilidades de fecha en formato ISO corto (yyyy-MM-dd), que es lo que espera la API. */

export function aIso(fecha: Date): string {
  const mes = `${fecha.getMonth() + 1}`.padStart(2, '0');
  const dia = `${fecha.getDate()}`.padStart(2, '0');
  return `${fecha.getFullYear()}-${mes}-${dia}`;
}

/** Convierte 'yyyy-MM-dd' a Date local, sin que la zona horaria corra el día. */
export function desdeIso(iso: string): Date {
  const [anio, mes, dia] = iso.split('-').map(Number);
  return new Date(anio, mes - 1, dia);
}

export function sumarDias(iso: string, dias: number): string {
  const fecha = desdeIso(iso);
  fecha.setDate(fecha.getDate() + dias);
  return aIso(fecha);
}

export function hoyIso(): string {
  return aIso(new Date());
}

/** 'a.m.'/'p.m.' escritos como en el diseño, sin depender de la configuración regional. */
export function hora12(iso: string): string {
  const fecha = new Date(iso);
  const horas = fecha.getHours();
  const minutos = `${fecha.getMinutes()}`.padStart(2, '0');
  const doce = horas % 12 === 0 ? 12 : horas % 12;

  return `${doce}:${minutos} ${horas < 12 ? 'a.m.' : 'p.m.'}`;
}

const MESES = [
  'enero', 'febrero', 'marzo', 'abril', 'mayo', 'junio',
  'julio', 'agosto', 'septiembre', 'octubre', 'noviembre', 'diciembre',
];

const DIAS = ['domingo', 'lunes', 'martes', 'miércoles', 'jueves', 'viernes', 'sábado'];

export function mesDe(iso: string): string {
  const fecha = desdeIso(iso.substring(0, 10));
  return `${MESES[fecha.getMonth()]} ${fecha.getFullYear()}`;
}

export function fechaLarga(iso: string): string {
  const fecha = new Date(iso.length <= 10 ? `${iso}T00:00:00` : iso);
  return `${DIAS[fecha.getDay()]} ${fecha.getDate()} de ${MESES[fecha.getMonth()]} de ${fecha.getFullYear()}`;
}

/** Formato compacto de las tarjetas: "MIÉ 15 JUL". */
export function fechaCorta(iso: string): string {
  const fecha = new Date(iso.length <= 10 ? `${iso}T00:00:00` : iso);
  const dia = DIAS[fecha.getDay()].substring(0, 3).toUpperCase();
  const mes = MESES[fecha.getMonth()].substring(0, 3).toUpperCase();
  return `${dia} ${fecha.getDate()} ${mes}`;
}

/** "en 2 días", "hoy", "mañana": el texto que acompaña a la próxima cita. */
export function cuandoFalta(iso: string): string {
  const objetivo = new Date(iso);
  const hoy = new Date();

  const dias = Math.round(
    (new Date(objetivo.getFullYear(), objetivo.getMonth(), objetivo.getDate()).getTime() -
      new Date(hoy.getFullYear(), hoy.getMonth(), hoy.getDate()).getTime()) /
      86_400_000
  );

  if (dias === 0) return 'hoy';
  if (dias === 1) return 'mañana';
  if (dias > 1) return `en ${dias} días`;
  if (dias === -1) return 'ayer';
  return `hace ${Math.abs(dias)} días`;
}
