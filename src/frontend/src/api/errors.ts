import { ApiError } from '../auth/session';
export type FieldErrors = Record<string, string>;
const messages: Record<string, string> = {
  "Move the animals and lots out of this paddock before archiving it.": "El potrero tiene animales o lotes vinculados. Trasládalos o resuelve esas dependencias antes de archivarlo.",
  "A Farm with the same identifying value already exists.": "Ya existe una finca con ese código. Revisa el código o restaura la finca archivada.",
  "A Species with the same identifying value already exists.": "Ya existe una especie con ese código o nombre.",
  "A Breed with the same identifying value already exists.": "Ya existe una raza con ese nombre para la especie seleccionada.",
  "A Lot with the same identifying value already exists.": "Ya existe un lote con ese nombre en la finca.",
  "Use a stock movement to change an existing balance.": "Registra una entrada o salida para cambiar el saldo, incluso si comienza en cero.",
  "Archive the farm's animals, inventory, paddocks and lots first.": "La finca tiene registros vinculados. Resuelve o archiva primero sus animales, existencias, potreros y lotes.",
  "This species is used by animals, breeds or lots.": "La especie tiene animales, razas o lotes vinculados. Resuelve esas dependencias antes de archivarla.",
  "This breed is used by animals.": "Hay animales con esta raza. Revisa sus fichas antes de archivarla.",
  "Move the animals out of this lot before archiving it.": "Traslada los animales a otro lote antes de archivar este grupo.",
  "A breed used by animals cannot change species.": "Esta raza está asignada a animales y debe conservar su especie.",
  "A lot used by animals cannot change species.": "Este lote contiene animales y debe conservar su especie.",
  "The paddock belongs to a different farm.": "Selecciona un potrero de la misma finca que el lote.",
  'A Product with the same identifying value already exists.': 'Ya existe un producto con ese código SKU. Revisa el código antes de guardar.',
  'A InventoryCategory with the same identifying value already exists.': 'Ya existe una categoría con ese nombre. Revisa el nombre antes de guardar.',
  'The category is inactive.': 'Selecciona una categoría activa para este producto.',
  'A stocked product cannot change its measurement unit.': 'El producto tiene inventario. Conserva su unidad para mantener coherentes los saldos y movimientos.',
  "A farm with stock movements cannot be deleted. Deactivate it instead.": "Esta finca tiene movimientos de inventario y debe conservarse. Puedes desactivarla.",
  "This animal has supply history. Deactivate it to preserve traceability.": "El animal tiene historial de insumos y debe conservarse. Puedes cambiar su estado.",
  'The stock changed. Refresh the balance before recording a movement.': 'Otro usuario cambió el saldo. Actualiza el saldo y revisa la cantidad antes de registrar el movimiento.',
  'The movement exceeds the available stock or storage limit.': 'La cantidad supera el saldo disponible o el límite de almacenamiento. Revisa la cantidad.',
  'The farm or product is inactive.': 'La finca o el producto están inactivos. Revisa su estado antes de registrar movimientos.',
  'Use a stock movement to change a traced balance.': 'Este saldo tiene historial. Registra una entrada o salida para modificarlo.',
  'A traced inventory cannot be deleted. Deactivate the product instead.': 'Este inventario tiene movimientos y debe conservarse. Puedes desactivar el producto.',
  'The submission identifier belongs to a different stock movement.': 'Este envío ya corresponde a otro movimiento. Actualiza el saldo antes de crear un nuevo registro.',
  'The report exceeds 10000 records. Narrow the period or farm before exporting.': 'El reporte supera 10.000 registros. Reduce el período o selecciona una finca para exportarlo.',
  'Choose a period of at most 367 days.': 'Selecciona un período de hasta 367 días.',

  'The paddock has reached its configured capacity.': 'El potrero alcanzó su capacidad máxima. Elige otro destino o libera espacio antes de trasladar el animal.',
  'The capacity cannot be lower than the current occupancy.': 'La capacidad no puede ser menor que la cantidad de animales activos presentes.',
  'An occupied paddock cannot be deactivated.': 'Traslada los animales activos antes de desactivar este potrero.',
  'The animal location changed. Refresh before moving it.': 'Otro usuario cambió la ubicación del animal. Actualiza la ficha y revisa el destino antes de trasladarlo.',
  'The animal is already at this location.': 'El animal ya tiene este potrero y lote. Selecciona una ubicación distinta.',
  'Only active animals can be moved.': 'Solo los animales activos pueden trasladarse.',
  'The paddock does not belong to this farm or is inactive.': 'Selecciona un potrero activo de esta finca.',
  'The farm is inactive.': 'La finca está inactiva. Revisa su estado antes de continuar.',
  'This submission identifier belongs to a different movement.': 'Este envío ya corresponde a otro traslado. Actualiza la ficha antes de registrar un nuevo movimiento.',
  'Milk and slaughter are blocked by a medication withdrawal period on this date.': 'El animal está en retiro sanitario para esa fecha. No se puede registrar leche ni sacrificio.',
  'This treatment conflicts with existing milk or slaughter during withdrawal. Review the historical records first.': 'Ya existe leche o sacrificio dentro de este período de retiro. Revisa los registros históricos antes de guardar el tratamiento.',
  'A treatment cannot shorten the product withdrawal period.': 'Los días de retiro no pueden ser menores que los del producto.',
  'Specify a known withdrawal period for this treatment.': 'Indica un período de retiro conocido para el tratamiento.',
  'A product with unknown or positive withdrawal needs a matching treatment record before vaccination or deworming.': 'Registra primero el tratamiento y su retiro para este producto. Después puedes añadir la vacunación o desparasitación de la misma administración.',
  'Only active animals can register care events.': 'Solo los animales activos pueden registrar eventos sanitarios.',
  'Reproductive events require an active female animal.': 'Los eventos reproductivos requieren una hembra activa.',
  'The event date precedes the animal birth.': 'La fecha del evento no puede ser anterior al nacimiento.',
  'An animal with care history cannot change species, sex or birth date.': 'Este animal tiene historial sanitario o reproductivo; su especie, sexo y nacimiento deben conservarse.',

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
    const field = key.split('.').pop()!.toLowerCase();
    const dateField = field.endsWith('date');
    const translated = /must not be empty|required/i.test(message) ? 'Este campo es obligatorio.'
      : /maximum length|characters.*fewer/i.test(message) ? 'El texto supera la longitud permitida.'
      : /decimal|precision|scale/i.test(message) ? 'Revisa el valor y la cantidad de decimales.'
      : /greater than/i.test(message) ? dateField ? 'La fecha debe ser posterior al inicio o a la fecha del evento.' : 'Revisa el valor mínimo permitido.'
      : /between/i.test(message) ? 'El valor está fuera del intervalo permitido.'
      : /less than or equal/i.test(message) ? dateField ? 'La fecha no puede superar el límite indicado.' : 'El valor no puede superar el máximo permitido.'
      : message;
    return [field, translated];
  }));
}
