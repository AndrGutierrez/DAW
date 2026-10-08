export type ManagedUser = { id: string; username: string; email: string; fullName: string; isActive: boolean; isSuperuser: boolean; roles: string[]; farmIds: string[]; directPermissions: string[]; version: string };
export type AccessRole = { id: string; name: string; description: string | null; permissions: string[] };
export type AccessPermission = { id: string; name: string };
export const roleLabels: Record<string, string> = { Admin: 'Administrador', Administrador: 'Administrador (rol alternativo)', Employee: 'Operador', SoloLectura: 'Solo lectura', Veterinario: 'Veterinario', Capataz: 'Capataz', Operario: 'Operario' };
