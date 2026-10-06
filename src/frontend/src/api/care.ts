export type CarePage<T> = { items: T[]; total: number; page: number; pageSize: number };
export type ClinicalData = { submissionId: string; kind: string; date: string; notes: string | null; productId: string | null; dose: number | null; route: string; endDate: string | null; withdrawalDays: number | null; nextDueDate: string | null; severity: string | null; isContagious: boolean; reason: string | null; cost: number | null };
export type ClinicalRecord = { id: string; createdAt: string; data: ClinicalData; withdrawalEndDate: string | null; productName: string | null };
export type ClinicalHistory = { history: CarePage<ClinicalRecord>; withdrawal: { blocked: boolean; lastRestrictedDate: string | null; releaseDate: string | null; treatments: number }; statusChanges: { changedAt: string; previousStatus: string; newStatus: string; reason: string | null }[] };
export type ReproductiveData = { kind: string; date: string; notes: string | null; sireId: string | null; result: string | null; method: string | null; expectedCalvingDate: string | null; offspringCount: number | null; stillbornCount: number | null; difficulty: string; offspringId: string | null; weightKg: number | null; reason: string | null };
export type ReproductiveHistory = { history: CarePage<{ id: string; createdAt: string; data: ReproductiveData }>; status: { state: string; date: string | null; expectedCalvingDate: string | null } };
export type ProductionData = { date: string; productType: string; method: string; quantity: number; unit: string; notes: string | null };
export const careLabels: Record<string, string> = {
  Treatment: 'Tratamiento', Vaccination: 'Vacunación', Deworming: 'Desparasitación', DiseaseCase: 'Caso clínico', Quarantine: 'Cuarentena', Mortality: 'Mortalidad',
  Heat: 'Celo', Mating: 'Monta natural', Insemination: 'Inseminación', PregnancyCheck: 'Diagnóstico de gestación', Calving: 'Parto', Weaning: 'Destete', Abortion: 'Aborto',
  Oral: 'Oral', Subcutaneous: 'Subcutánea', Intramuscular: 'Intramuscular', Intravenous: 'Intravenosa', Topical: 'Tópica', Other: 'Otra',
  Positive: 'Positivo', Negative: 'Negativo', Uncertain: 'Incierto', Easy: 'Sin asistencia', Assisted: 'Asistido', Difficult: 'Difícil', Cesarean: 'Cesárea',
  Pregnant: 'Gestación confirmada', NotPregnant: 'Sin gestación confirmada', Calved: 'Parto registrado', Aborted: 'Aborto registrado', Unknown: 'Sin diagnóstico registrado', NotApplicable: 'No aplica',
  Milk: 'Leche', Meat: 'Carne', Hide: 'Piel', Wool: 'Lana', Eggs: 'Huevos', Liter: 'L', Kilogram: 'kg', Unit: 'unidades',
  Milking: 'Ordeño', Slaughter: 'Sacrificio', Shearing: 'Esquila', Collection: 'Recolección',
};
export const careText = (value: string) => careLabels[value] || value;
export function formValues(form: HTMLFormElement) {
  const data = new FormData(form);
  return {
    text: (key: string) => String(data.get(key) || '').trim() || null,
    number: (key: string) => data.get(key) === null || data.get(key) === '' ? null : Number(data.get(key)),
  };
}
