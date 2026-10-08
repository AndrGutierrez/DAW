import { Button, Input, Select, Textarea } from '../components/ui/Controls';
import { useEffect, useState } from 'react';
import type { FormEvent, InputHTMLAttributes } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { useResource } from '../api/useResource';
import { labels, today } from '../api/livestock';
import type { AnimalDetail, CatalogItem } from '../api/livestock';
import { errorMessage, fieldErrors } from '../api/errors';
import type { FieldErrors } from '../api/errors';
import { Field } from '../components/Field';
import { ParentPicker } from '../components/ParentPicker';
import { useUnsavedChanges } from '../components/NavigationProtection';
import { useFeedback } from '../components/Feedback';

const empty = { farmId: '', speciesId: '', internalTag: '', sex: 'Female', purpose: 'Meat', breedId: '', lotId: '', paddockId: '', officialId: '', rfid: '', name: '', birthDate: '', birthWeightKg: '', color: '', markings: '', status: 'Active', origin: 'Born', healthStatus: 'Healthy', damId: '', sireId: '', notes: '' };
type Values = typeof empty;
const groups = { sex: ['Female', 'Male'], purpose: ['Meat', 'Milk', 'DualPurpose', 'Wool', 'Eggs', 'Work'], status: ['Active', 'Sold', 'Dead', 'Transferred', 'Lost'], origin: ['Born', 'Purchased'], healthStatus: ['Healthy', 'UnderObservation', 'InTreatment', 'Quarantine', 'Critical'] };

export function AnimalEditorPage() {
  const { id } = useParams();
  const { can } = useAuth();
  const detail = useResource<AnimalDetail>('/api/animals/' + id, !!id && can('animals.get'));
  const allowed = can(id ? 'animals.update' : 'animals.create');
  if (!allowed || (id && !can('animals.get'))) return <div className="panel empty-state"><h1>Sin acceso a esta acción</h1><Link to="/animals">Volver a los animales</Link></div>;
  if (id && detail.loading) return <p role="status">Cargando ficha para editar…</p>;
  if (id && detail.error) return <div className="panel empty-state"><p role="alert">{detail.error}</p><Button className="button secondary" onClick={detail.reload}>Reintentar</Button></div>;
  return <AnimalEditor key={id || 'new'} animal={detail.data || undefined} />;
}

function AnimalEditor({ animal }: { animal?: AnimalDetail }) {
  const { request, can } = useAuth();
  const { notify, confirm } = useFeedback();
  const navigate = useNavigate();
  const [values, setValues] = useState<Values>(() => animal ? Object.fromEntries(Object.keys(empty).map(key => [key, String(animal[key as keyof AnimalDetail] ?? '')])) as Values : { ...empty });
  const [errors, setErrors] = useState<FieldErrors>({});
  const [error, setError] = useState('');
  const [dirty, setDirty] = useState(false);
  const [busy, setBusy] = useState(false);
  const clearUnsaved = useUnsavedChanges(dirty);
  const farms = useResource<CatalogItem[]>('/api/farms', can('farms.list'));
  const species = useResource<CatalogItem[]>('/api/species', can('species.list'));
  const breeds = useResource<CatalogItem[]>('/api/breeds', can('breeds.list'));
  const lots = useResource<CatalogItem[]>('/api/lots', can('lots.list'));
  const paddocks = useResource<CatalogItem[]>('/api/paddocks', can('paddocks.list'));
  const resources = [farms, species, breeds, lots, paddocks];
  const catalogError = resources.find(item => item.error)?.error;
  const loading = resources.some(item => item.loading);
  useEffect(() => {
    if (!animal && !values.farmId && farms.data?.filter(item => item.data.isActive).length === 1)
      setValues(current => ({ ...current, farmId: farms.data!.find(item => item.data.isActive)!.id }));
  }, [farms.data, animal, values.farmId]);
  function change(key: keyof Values, value: string) {
    setDirty(true); setErrors(current => ({ ...current, [key.toLowerCase()]: '' }));
    setValues(current => {
      const next = { ...current, [key]: value };
      if (key === 'farmId') Object.assign(next, { lotId: '', paddockId: '', damId: '', sireId: '' });
      if (key === 'speciesId') Object.assign(next, { breedId: '', lotId: '', damId: '', sireId: '', purpose: species.data?.find(item => item.id === value)?.data.purpose || current.purpose });
      return next;
    });
  }
  function input(key: keyof Values, label: string, props: InputHTMLAttributes<HTMLInputElement> = {}) {
    return <Field label={label} error={errors[key.toLowerCase()]}><Input {...props} value={values[key]} onChange={event => change(key, event.target.value)} /></Field>;
  }
  function enums(key: keyof typeof groups, label: string) {
    return <Field label={label} error={errors[key.toLowerCase()]}><Select value={values[key]} onChange={event => change(key, event.target.value)}>{groups[key].map(value => <option key={value} value={value}>{labels[value]}</option>)}</Select></Field>;
  }
  function catalog(key: 'farmId' | 'speciesId' | 'breedId' | 'lotId' | 'paddockId', label: string, items: CatalogItem[] | null, required = false) {
    const selectedName = animal?.[key === 'farmId' ? 'farm' : key === 'speciesId' ? 'species' : key === 'breedId' ? 'breed' : key === 'lotId' ? 'lot' : 'paddock'];
    return <Field label={label + (required ? ' *' : '')} error={errors[key.toLowerCase()]}><Select value={values[key]} disabled={key === 'farmId' && !!animal || key === 'breedId' && !values.speciesId || (key === 'lotId' || key === 'paddockId') && !values.farmId} onChange={event => change(key, event.target.value)}>
      <option value="">{required ? 'Selecciona una opción' : 'Sin asignar'}</option>
      {values[key] && !items?.some(item => item.id === values[key]) && <option value={values[key]}>{selectedName || 'Asignación actual'}</option>}
      {items?.filter(item => item.data.isActive || item.id === values[key]).map(item => <option key={item.id} value={item.id}>{item.data.name}{!item.data.isActive ? ' (inactivo)' : ''}</option>)}
    </Select></Field>;
  }
  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); if (busy) return;
    const invalid: FieldErrors = {};
    for (const key of ['farmId', 'speciesId', 'internalTag'] as const) if (!values[key].trim()) invalid[key.toLowerCase()] = 'Este campo es obligatorio.';
    if (values.birthDate && values.birthDate > today()) invalid.birthdate = 'La fecha no puede ser posterior a hoy.';
    if (values.birthWeightKg && (!Number.isFinite(Number(values.birthWeightKg)) || Number(values.birthWeightKg) <= 0)) invalid.birthweightkg = 'Ingresa un peso mayor que cero.';
    if (Object.keys(invalid).length) { setErrors(invalid); setError('Revisa los campos señalados.'); requestAnimationFrame(() => document.querySelector<HTMLElement>('[aria-invalid="true"]')?.focus()); return; }
    const payload = Object.fromEntries(Object.entries(values).map(([key, value]) => [key, key === 'birthWeightKg' ? value ? Number(value) : null : value.trim() || null]));
    setBusy(true); setError(''); setErrors({});
    try {
      const result = await request<{ id: string }>('/api/animals' + (animal ? '/' + animal.id : ''), { method: animal ? 'PUT' : 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(payload) });
      clearUnsaved(); setDirty(false); notify(animal ? 'Ficha actualizada.' : 'Animal registrado. Ahora puedes añadir su fotografía y pesajes.'); navigate('/animals/' + result.id);
    } catch (failure) { setError(errorMessage(failure)); setErrors(fieldErrors(failure)); notify(errorMessage(failure), 'error'); }
    finally { setBusy(false); }
  }
  async function cancel() { if (!dirty || await confirm('Hay cambios sin guardar. ¿Quieres salir de este formulario?', { destructive: true })) { clearUnsaved(); navigate(animal ? '/animals/' + animal.id : '/animals'); } }
  return <section><div className="page-heading"><div><span className="eyebrow">{animal ? animal.internalTag : 'NUEVO EJEMPLAR'}</span><h1>{animal ? 'Editar animal' : 'Registrar animal'}</h1><p className="muted">Los campos con * son obligatorios. Las demás características se pueden completar después.</p></div></div>
    {catalogError && <div className="panel error-banner" role="alert">{catalogError}<Button className="button secondary" onClick={() => resources.forEach(item => item.reload())}>Recargar catálogos</Button></div>}
    {loading && <p role="status">Cargando fincas y catálogos…</p>}
    <form noValidate onSubmit={save} className="editor-form">
      <fieldset className="panel form-section" disabled={busy}><legend>Identificación</legend><div className="form-grid">{catalog('farmId', 'Finca', farms.data, true)}{catalog('speciesId', 'Especie', species.data, true)}
        {input('internalTag', 'Arete interno *', { maxLength: 50, autoComplete: 'off' })}{input('name', 'Nombre', { maxLength: 100 })}
        {enums('sex', 'Sexo')}{enums('purpose', 'Propósito')}{catalog('breedId', 'Raza', breeds.data?.filter(item => item.data.speciesId === values.speciesId) || null)}
        {input('officialId', 'Identificación oficial', { maxLength: 50 })}{input('rfid', 'RFID', { maxLength: 50 })}</div></fieldset>
      <fieldset className="panel form-section" disabled={busy}><legend>Origen y ubicación</legend><div className="form-grid">{enums('origin', 'Origen')}{input('birthDate', 'Fecha de nacimiento', { type: 'date', max: today() })}{input('birthWeightKg', 'Peso al nacer (kg)', { type: 'number', min: 0.0001, step: 0.0001 })}
        {catalog('lotId', 'Lote', lots.data?.filter(item => item.data.farmId === values.farmId && item.data.speciesId === values.speciesId) || null)}
        {catalog('paddockId', 'Potrero', paddocks.data?.filter(item => item.data.farmId === values.farmId) || null)}{enums('status', 'Estado')}{enums('healthStatus', 'Estado de salud')}
        {input('color', 'Color', { maxLength: 50 })}{input('markings', 'Señas particulares', { maxLength: 200 })}</div><p className="muted form-note">El lote identifica el grupo productivo; el potrero indica la ubicación física. Se seleccionan por separado.</p>{animal && <p className="muted form-note">La finca se conserva. Los registros productivos y la genealogía pueden impedir cambios de especie, sexo o nacimiento.</p>}</fieldset>
      <fieldset className="panel form-section" disabled={busy}><legend>Genealogía y observaciones</legend><div className="form-grid">{can('animals.list') && ['Female', 'Male'].map(sex => <ParentPicker disabled={busy} key={values.farmId + values.speciesId + sex} label={sex === 'Female' ? 'Madre' : 'Padre'} sex={sex} farmId={values.farmId} speciesId={values.speciesId} excludeId={animal?.id} value={values[sex === 'Female' ? 'damId' : 'sireId']} onChange={value => change(sex === 'Female' ? 'damId' : 'sireId', value)} error={errors[sex === 'Female' ? 'damid' : 'sireid']} />)}</div>
        <Field label="Observaciones" error={errors.notes}><Textarea rows={4} maxLength={2000} value={values.notes} onChange={event => change('notes', event.target.value)} /></Field></fieldset>
      <div className="form-actions panel">{error && <p className="field-error" role="alert">{error}</p>}<div className="button-row"><Button type="button" className="button secondary" disabled={busy} onClick={() => void cancel()}>Cancelar</Button><Button type="submit" className="button primary" disabled={busy || loading || !!catalogError}>{busy ? 'Guardando…' : 'Guardar animal'}</Button></div></div>
    </form></section>;
}
