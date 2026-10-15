import { useEffect, useRef, useState } from 'react';
import type { FormEvent } from 'react';
import { useAuth } from '../auth/AuthContext';
import { useResource } from '../api/useResource';
import { validatePaddockPlan } from '../api/clientValidation';
import { errorMessage, fieldErrors } from '../api/errors';
import type { FieldErrors } from '../api/errors';
import type { CatalogItem } from '../api/livestock';
import type { PaddockData, PaddockGeometry, PaddockSnapshot } from '../api/paddocks';
import { useFeedback } from './Feedback';
import { useUnsavedChanges } from './NavigationProtection';
import { Field } from './Field';
import { Button, Input, Select } from './ui/Controls';
import { constrainRect, PaddockLayout, suggestPlacement } from './PaddockLayout';
import type { Rect } from './paddock-scene/model';

const mapKeys = ['mapX', 'mapY', 'mapWidth', 'mapHeight'] as const;
type Coordinates = Record<typeof mapKeys[number], string>;
const coordinates = (rect: Rect | null): Coordinates => ({ mapX: rect ? String(rect.x) : '', mapY: rect ? String(rect.y) : '', mapWidth: rect ? String(rect.width) : '', mapHeight: rect ? String(rect.height) : '' });
type GeometryResource = { data: PaddockGeometry[] | null; loading: boolean; error: string; reload: () => void };
type Props = { geometryResource?: GeometryResource; item?: PaddockSnapshot; farms: CatalogItem[]; initialFarmId: string; onSaved: () => void; onCancel: () => void };

export function PaddockEditor({ item, geometryResource, farms, initialFarmId, onSaved, onCancel }: Props) {
  const { request, can } = useAuth();
  const { notify, confirm } = useFeedback();
  const [busy, setBusy] = useState(false), [error, setError] = useState(''), [errors, setErrors] = useState<FieldErrors>({});
  const [dirty, setDirty] = useState(false), [name, setName] = useState(item?.data.name || '');
  const [farm, setFarm] = useState(item?.data.farmId || initialFarmId || (farms.filter(f => f.data.isActive).length === 1 ? farms.find(f => f.data.isActive)!.id : ''));
  const [coords, setCoords] = useState<Coordinates>(() => item
    ? Object.fromEntries(mapKeys.map(key => [key, item.data[key] == null ? '' : String(item.data[key])])) as Coordinates
    : coordinates(suggestPlacement([])));
  const touchedLayout = useRef(false);
  const ownPeers = useResource<PaddockGeometry[]>('/api/paddocks', !geometryResource && !!farm && can('paddocks.list'));
  const peers = geometryResource || ownPeers;
  const farmPeers = (peers.data || []).filter(peer => peer.data.farmId === farm);
  const clearUnsaved = useUnsavedChanges(dirty);
  useEffect(() => {
    if (!item && peers.data && !touchedLayout.current) setCoords(coordinates(suggestPlacement(peers.data.filter(peer => peer.data.farmId === farm))));
  }, [peers.data, item, farm]);
  const values = mapKeys.map(key => coords[key] === '' ? null : Number(coords[key]));
  const rect = values.every(value => value !== null && Number.isFinite(value)) && values[2]! > 0 && values[3]! > 0
    ? { x: values[0]!, y: values[1]!, width: values[2]!, height: values[3]! } : null;
  function place(next: Rect | null) { touchedLayout.current = true; setCoords(coordinates(next ? constrainRect(next) : null)); setDirty(true); }
  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); if (busy) return;
    const element = event.currentTarget, form = new FormData(element);
    const value = (key: string) => String(form.get(key) || '').trim();
    const data: PaddockData = { farmId: farm, name: name.trim(), code: value('code') || null,
      areaHectares: value('area') ? Number(value('area')) : null, capacity: value('capacity') ? Number(value('capacity')) : null,
      isActive: form.get('active') === 'on', maxStayDays: value('maxStayDays') ? Number(value('maxStayDays')) : null,
      mapX: values[0], mapY: values[1], mapWidth: values[2], mapHeight: values[3] };
    const localErrors = validatePaddockPlan(data);
    if (Object.keys(localErrors).length) {
      setErrors(localErrors); setError('Revisa la ubicación del potrero.');
      const advanced = element.querySelector<HTMLDetailsElement>('.paddock-plan-editor'); if (advanced) advanced.open = true;
      requestAnimationFrame(() => element.querySelector<HTMLElement>('[aria-invalid="true"]')?.focus()); return;
    }
    setBusy(true); setError(''); setErrors({});
    try { await request('/api/paddocks' + (item ? '/' + item.id : ''), { method: item ? 'PUT' : 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(data) }); clearUnsaved(); setDirty(false); notify('Potrero guardado.'); onSaved(); }
    catch (failure) { setError(errorMessage(failure)); setErrors(fieldErrors(failure)); notify(errorMessage(failure), 'error'); }
    finally { setBusy(false); }
  }
  return <section className="panel paddock-editor" aria-labelledby="paddock-editor"><div className="section-heading"><div><span className="eyebrow">DALE UN LUGAR EN TU FINCA</span><h2 id="paddock-editor">{item ? 'Editar potrero' : 'Registrar potrero'}</h2></div><span className="tag">Vista previa · aún sin guardar</span></div>
    <form className="care-form" onSubmit={submit} onChange={() => setDirty(true)} aria-busy={busy}><fieldset disabled={busy}>
      <div className="paddock-editor-workspace"><div className="paddock-editor-fields"><h3>Identifica el potrero</h3><div className="form-grid">
        <Field label="Finca del potrero *" error={errors.farmid}><Select name="farm" required disabled={!!item} value={farm} onChange={event => { setFarm(event.target.value); touchedLayout.current = false; }}><option value="">Selecciona una finca</option>{farms.filter(f => f.data.isActive || f.id === item?.data.farmId).map(f => <option value={f.id} key={f.id}>{f.data.name}</option>)}</Select></Field>
        <Field label="Nombre del potrero *" error={errors.name}><Input name="name" required maxLength={150} value={name} onChange={event => setName(event.target.value)} placeholder="Por ejemplo, Potrero Norte" /></Field>
      </div><h3>Define su manejo</h3><div className="form-grid">
        <Field label="Superficie (ha)" error={errors.areahectares}><Input name="area" type="number" min="0.0001" max="9999999999.9999" step="0.0001" defaultValue={item?.data.areaHectares ?? ''} /></Field>
        <Field label="Capacidad máxima (animales)" error={errors.capacity} hint="Limita las entradas al potrero."><Input name="capacity" type="number" min="1" max="2147483647" step="1" defaultValue={item?.data.capacity ?? ''} /></Field>
        <Field label="Permanencia máxima (días)" error={errors.maxstaydays} hint="La estimación visual compara este período con la entrada más antigua registrada."><Input name="maxStayDays" type="number" min="1" max="3650" step="1" defaultValue={item?.data.maxStayDays ?? ''} /></Field>
        <Field label="Código del potrero" error={errors.code}><Input name="code" maxLength={30} defaultValue={item?.data.code || ''} /></Field>
      </div><p className="muted">Si faltan fechas de entrada o permanencia máxima, el plano mostrará «Sin estimación». No se calcula la cantidad real de pasto.</p>
      <label className="check-field"><Input name="active" type="checkbox" defaultChecked={item?.data.isActive ?? true} /> Potrero activo</label></div>
      <div className="paddock-editor-map"><div className="section-heading"><h3>Colócalo en el terreno</h3><Button type="button" disabled={!farm || busy} onClick={() => place(rect ? null : suggestPlacement(farmPeers, item?.id))}>{rect ? 'Usar distribución automática' : 'Colocar en el plano'}</Button></div>
        {peers.error && <p className="error-banner" role="alert">{peers.error} <Button type="button" onClick={peers.reload}>Reintentar plano</Button></p>}
        <PaddockLayout items={farmPeers} excludeId={item?.id} rect={rect} name={name} disabled={busy || !farm || peers.loading} onChange={place} />
        <details className="paddock-plan-editor"><summary>Coordenadas avanzadas (opcional)</summary><p className="muted">Plano de 100 × 100. El dibujo no modifica las hectáreas. Completa los cuatro valores o deja todos vacíos.</p><div className="form-grid">{mapKeys.map((key, i) => <Field key={key} label={['Posición X (%)', 'Posición Y (%)', 'Ancho (%)', 'Alto (%)'][i]} error={errors[key.toLowerCase()]}><Input name={key} type="number" min={i < 2 ? '0' : '0.0001'} max="100" step="0.0001" value={coords[key]} onChange={event => { touchedLayout.current = true; setCoords(previous => ({ ...previous, [key]: event.target.value })); }} /></Field>)}</div></details>
      </div></div>
    </fieldset>{error && <p className="error-banner" role="alert">{error}</p>}<div className="button-row"><Button className="button primary" disabled={busy}>{busy ? 'Guardando…' : 'Guardar potrero'}</Button><Button type="button" variant="secondary" disabled={busy} onClick={async () => { if (!dirty || await confirm('¿Descartar los cambios del potrero?', { destructive: true })) { clearUnsaved(); onCancel(); } }}>Cancelar</Button></div></form>
  </section>;
}
