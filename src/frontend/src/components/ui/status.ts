export function healthTone(status: string) {
  if (status === 'Healthy') return 'positive';
  if (status === 'Critical') return 'critical';
  if (['UnderObservation', 'InTreatment', 'Quarantine'].includes(status)) return 'warning';
  return '';
}
