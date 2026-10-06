import { ApiError } from '../auth/session';
export type FieldErrors = Record<string, string>;
const messages: Record<string, string> = {
  'Birth date cannot follow an existing weighing date.': 'La fecha de nacimiento no puede ser posterior a un pesaje ya registrado.',
  'The weighing date precedes the animal birth.': 'La fecha del pesaje no puede ser anterior al nacimiento del animal.',
  'Only active animals can change health status.': 'Solo los animales activos pueden cambiar su estado de salud.',
  'A recorded parent cannot change species, sex or birth date while offspring reference it.': 'Este animal tiene descendientes registrados; su especie, sexo y nacimiento deben conservarse.',
  'A slaughtered animal cannot become active again.': 'Un animal sacrificado debe conservar su estado fallecido.',
  'The parent relationship would create a cycle.': 'La genealogía seleccionada crea un ciclo. Revisa los progenitores.',
  'The requested animal was not found.': 'El animal no existe o no tienes acceso a su finca.',
  'The requested record was not found.': 'El registro no existe o no tienes acceso.',
  'Only active animals can be weighed in this workflow.': 'Este animal ya no está activo. Actualiza la lista antes de pesarlo.',
  'The submission identifier has already been used for different data.': 'Este envío ya fue guardado con otros valores. Inicia un nuevo registro para cambiar el peso.',
  'The record conflicts with an existing unique value.': 'Ya existe un registro con esa identificación. Revisa el arete, la identificación oficial y el RFID.',
  'A concurrent operation changed this data. Refresh and retry the request.': 'Otro usuario modificó estos datos. Reintenta el envío para comprobar el resultado.',
  'Moving a record between farms requires a dedicated transfer workflow.': 'El cambio de finca requiere un traslado; no se puede hacer desde esta ficha.',
};
export function errorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    const detail = error.problem.detail || '';
    if (messages[detail]) return messages[detail];
    if (/unique|same identifying value|already.*(tag|official|rfid)/i.test(detail)) return 'Ya existe un animal con esa identificación en la finca.';
    if (/breed.*species|lot.*species/i.test(detail)) return 'La raza o el lote no corresponde a la especie seleccionada.';
    if (/farm/i.test(detail) && /match|belong/i.test(detail)) return 'La ubicación seleccionada no corresponde a esta finca.';
    if (/parent|dam|sire|genealog|ancestor/i.test(detail)) return 'Revisa la genealogía: los padres deben pertenecer a la misma finca y especie, tener el sexo indicado y ser mayores que el animal.';
    if (/slaughter/i.test(detail)) return 'No se puede registrar un peso vivo en esa fecha porque existe un sacrificio.';
    if (/reactivat/i.test(detail)) return 'Un animal sacrificado no puede volver al estado activo.';
    if (/production|referenced.*parent/i.test(detail)) return 'La especie, el sexo o el nacimiento no se pueden cambiar porque existen registros dependientes.';
    if (error.status === 403) return 'Tu cuenta no tiene permiso para esta acción.';
    if (error.status === 404) return 'El registro no existe o no tienes acceso a su finca.';
    if (error.status >= 500) return 'El servidor no pudo completar la operación. Conservamos tus datos para reintentar.';
    if (error.problem.errors) return 'Revisa los campos señalados antes de continuar.';
    return detail || 'No se pudo completar la operación.';
  }
  return error instanceof Error && error.name !== 'TypeError' ? error.message : 'No se pudo confirmar el resultado. Reintenta el mismo envío cuando vuelva la conexión.';
}
export function fieldErrors(error: unknown): FieldErrors {
  if (!(error instanceof ApiError)) return {};
  return Object.fromEntries(Object.entries(error.problem.errors || {}).map(([key, values]) => {
    const message = values[0] || '';
    const translated = /must not be empty|required/i.test(message) ? 'Este campo es obligatorio.'
      : /maximum length|characters.*fewer/i.test(message) ? 'El texto supera la longitud permitida.'
      : /decimal|precision|scale/i.test(message) ? 'Revisa el valor y la cantidad de decimales.'
      : /greater than/i.test(message) ? 'Ingresa un valor mayor que cero.'
      : /between/i.test(message) ? 'El valor está fuera del intervalo permitido.'
      : /less than or equal/i.test(message) ? 'La fecha no puede ser posterior a hoy.'
      : message;
    return [key.split('.').pop()!.toLowerCase(), translated];
  }));
}
