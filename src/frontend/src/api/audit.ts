export type AuditEntry = {
  id: string; occurredAt: string; action: string; entityName: string; entityId: string | null;
  userId: string | null; actorName: string | null; farmId: string | null; farmName: string | null;
};
export type AuditDetail = { entry: AuditEntry; ipAddress: string | null; oldValues: string | null; newValues: string | null };
export type AuditOptions = { actions: string[]; entities: string[]; farms: { id: string; name: string }[] };
export const auditActions: Record<string, string> = {
  Added: 'Creación', Modified: 'Modificación', Deleted: 'Eliminación', UserCreated: 'Cuenta creada',
  UserUpdated: 'Cuenta actualizada', RolePermissionGranted: 'Permiso concedido', RolePermissionRevoked: 'Permiso retirado', PasswordReset: 'Contraseña restablecida', LoginSucceeded: 'Inicio de sesión',
  LoginRejected: 'Acceso rechazado', Logout: 'Cierre de sesión', RegistrationSucceeded: 'Registro de cuenta',
};
const entities: Record<string, string> = {
  RolePermission: 'Permiso de rol', Treatment: 'Tratamiento', Vaccination: 'Vacunación', AlertRule: 'Regla de alerta',
  Authentication: 'Acceso al sistema', ApplicationUser: 'Cuenta de usuario', Animal: 'Animal', Farm: 'Finca',
  Paddock: 'Potrero', Lot: 'Lote', Species: 'Especie', Breed: 'Raza', InventoryCategory: 'Categoría de insumos',
  Product: 'Insumo', FarmInventory: 'Existencia', StockMovement: 'Movimiento de inventario', WeightRecord: 'Pesaje',
  HealthStatusChange: 'Estado sanitario', ClinicalEvent: 'Evento clínico', MedicationAdministration: 'Medicación',
  BreedingEvent: 'Servicio reproductivo', PregnancyCheck: 'Diagnóstico de gestación', Calving: 'Parto',
  AnimalMovement: 'Traslado', AnimalProduction: 'Producción', AnimalPhoto: 'Fotografía', ExchangeRate: 'Tasa de referencia',
  GrowthGoal: 'Meta de crecimiento', Alert: 'Alerta', TaskItem: 'Tarea',
};
export const auditEntity = (name: string) => entities[name] || name.replace(/([a-z])([A-Z])/g, '$1 $2');
export const auditActor = (entry: AuditEntry) => entry.actorName || (entry.userId ? 'Usuario no disponible' : entry.action === 'LoginRejected' ? 'Identidad no validada' : 'Sin usuario registrado');
export const auditTime = (value: string) => new Date(value).toLocaleString('es-VE', { dateStyle: 'medium', timeStyle: 'medium' });
const fields: Record<string, string> = {
  Id: 'Identificador', FarmId: 'Finca (ID)', AnimalId: 'Animal (ID)', InternalTag: 'Identificación interna', Name: 'Nombre',
  OfficialId: 'Identificación oficial', Rfid: 'RFID', BreedId: 'Raza (ID)', SpeciesId: 'Especie (ID)', LotId: 'Lote (ID)',
  PaddockId: 'Potrero (ID)', Sex: 'Sexo', Status: 'Estado', HealthStatus: 'Estado sanitario', BirthDate: 'Nacimiento',
  BirthWeightKg: 'Peso al nacer (kg)', WeightKg: 'Peso (kg)', BodyConditionScore: 'Condición corporal', Purpose: 'Propósito',
  Origin: 'Origen', DamId: 'Madre (ID)', SireId: 'Padre (ID)', Notes: 'Notas', UpdatedAt: 'Última actualización', CreatedAt: 'Creación',
  UserId: 'Usuario (ID)', Username: 'Usuario', FullName: 'Nombre completo', Email: 'Correo', IsActive: 'Activo',
  IsSuperuser: 'Cuenta del sistema', Roles: 'Roles', FarmIds: 'Fincas (ID)', DirectPermissions: 'Permisos adicionales',
  Markings: 'Marcas distintivas', TargetDailyGainKg: 'Meta de GDP (kg/día)', Assigned: 'Asignado', Role: 'Rol', Permission: 'Permiso', PermissionId: 'Permiso (ID)', SizeBytes: 'Tamaño (bytes)', FileName: 'Archivo', UploadedAt: 'Fecha de carga', UploadedByUserId: 'Usuario que cargó (ID)',
  Version: 'Versión', ProductId: 'Insumo (ID)', SKU: 'SKU', Price: 'Precio (USD)', CostPrice: 'Costo (USD)', Stock: 'Existencia',
  MinStock: 'Existencia mínima', MaxStock: 'Existencia máxima', Quantity: 'Cantidad', Unit: 'Unidad', Date: 'Fecha',
  Reason: 'Motivo', SessionsRevoked: 'Sesiones revocadas', SessionWasActive: 'Sesión vigente al cerrarse',
  FromPaddockId: 'Potrero anterior (ID)', ToPaddockId: 'Potrero de destino (ID)', FromLotId: 'Lote anterior (ID)',
  ToLotId: 'Lote de destino (ID)', PreviousStatus: 'Estado anterior', NewStatus: 'Estado nuevo',
};
export function auditField(key: string) { const normalized = key.charAt(0).toUpperCase() + key.slice(1); return fields[normalized] || normalized.replace(/([a-z])([A-Z])/g, '$1 $2'); }
const enums: Record<string, string[]> = {
  Sex: ['Macho', 'Hembra'], HealthStatus: ['Sano', 'En observación', 'En tratamiento', 'Cuarentena', 'Crítico'],
  PreviousStatus: ['Sano', 'En observación', 'En tratamiento', 'Cuarentena', 'Crítico'], NewStatus: ['Sano', 'En observación', 'En tratamiento', 'Cuarentena', 'Crítico'],
  Purpose: ['Carne', 'Leche', 'Lana', 'Huevos', 'Doble propósito', 'Trabajo'], Origin: ['Nacimiento', 'Compra'],
  Unit: ['kg', 'L', 'unidad', 'dosis', 'saco', 'micrón', '%', 'ha'],
};
export function auditValue(value: unknown, field: string, entity: string): string {
  if (value == null) return 'Sin registro';
  if (typeof value === 'boolean') return value ? 'Sí' : 'No';
  const key = field.charAt(0).toUpperCase() + field.slice(1);
  const enumValues = key === 'Status' && entity === 'Animal' ? ['Activo', 'Vendido', 'Fallecido', 'Transferido', 'Extraviado'] : enums[key];
  if (typeof value === 'number') return enumValues?.[value] || new Intl.NumberFormat('es-VE', { maximumFractionDigits: 4 }).format(value);
  if (Array.isArray(value)) return value.length ? value.map(v => typeof v === 'object' ? JSON.stringify(v) : String(v)).join(', ') : 'Ninguno';
  if (typeof value === 'object') return JSON.stringify(value, null, 2);
  if (value === 'CredentialsRejected') return 'Credenciales rechazadas';
  if (value === 'InactiveAccount') return 'Cuenta inactiva';
  return String(value);
}
type Snapshot = Record<string, unknown>;
function snapshot(json: string | null): Snapshot {
  if (json === null) return {};
  try { const value: unknown = JSON.parse(json); return value && typeof value === 'object' && !Array.isArray(value) ? value as Snapshot : { Value: value }; }
  catch { return { Unavailable: 'No se pudo interpretar el registro histórico.' }; }
}
function canonical(value: unknown): string | undefined {
  if (Array.isArray(value)) return JSON.stringify(value.map(canonical));
  if (value && typeof value === 'object') return JSON.stringify(Object.entries(value).sort(([a], [b]) => a.localeCompare(b)).map(([k,v]) => [k,canonical(v)]));
  return JSON.stringify(value);
}
export function auditChanges(detail: AuditDetail) {
  const before = snapshot(detail.oldValues), after = snapshot(detail.newValues);
  return [...new Set([...Object.keys(before), ...Object.keys(after)])].map(field => ({ field, before: before[field], after: after[field],
    hadBefore: Object.hasOwn(before, field), hasAfter: Object.hasOwn(after, field), changed: canonical(before[field]) !== canonical(after[field]) || Object.hasOwn(before, field) !== Object.hasOwn(after, field) }));
}
