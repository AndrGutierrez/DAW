import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { useResource } from '../api/useResource';
import { kg, today } from '../api/livestock';
import type { Animal, AnimalPageResult, CatalogItem } from '../api/livestock';
import { useFeedback } from '../components/Feedback';
import { Field } from '../components/Field';
import { WeighingForm } from '../components/WeighingForm';
import type { SavedWeighing } from '../components/WeighingForm';
export function WeighingPage() {
  const { can } = useAuth();
  const { confirm } = useFeedback();
  const [blocked, setBlocked] = useState(false);
  const [skipped, setSkipped] = useState<Animal[]>([]);
  const allowed = can('weights.create') && can('animals.list') && can('animals.get');
  const farms = useResource<CatalogItem[]>('/api/farms', allowed && can('farms.list'));
  const lots = useResource<CatalogItem[]>('/api/lots', allowed && can('lots.list'));
  const [farmId, setFarmId] = useState('');
  const [lotId, setLotId] = useState('');
  const [search, setSearch] = useState('');
  const [query, setQuery] = useState('');
  const [page, setPage] = useState(1);
  const [selected, setSelected] = useState<Animal[]>([]);
  const [queue, setQueue] = useState<Animal[] | null>(null);
  const [index, setIndex] = useState(0);
  const [weighDate, setWeighDate] = useState(today());
  const [saved, setSaved] = useState<SavedWeighing[]>([]);
  const params = new URLSearchParams({ farmId, page: String(page), pageSize: '12', status: 'Active', ...(lotId ? { lotId } : {}), ...(query ? { search: query } : {}) });
  const animals = useResource<AnimalPageResult>('/api/animals/page?' + params, allowed && !!farmId && !queue);
  useEffect(() => { if (!farmId && farms.data?.filter(item => item.data.isActive).length === 1) setFarmId(farms.data.find(item => item.data.isActive)!.id); }, [farms.data, farmId]);
  useEffect(() => {
    if (!queue || index >= queue.length) return;
    const warn = (event: BeforeUnloadEvent) => { event.preventDefault(); event.returnValue = ''; };
    window.addEventListener('beforeunload', warn); return () => window.removeEventListener('beforeunload', warn);
  }, [queue, index]);
  function choose(animal: Animal) { setSelected(items => items.some(item => item.id === animal.id) ? items.filter(item => item.id !== animal.id) : items.length < 50 ? [...items, animal] : items); }
  async function skip() {
    if (blocked || !queue?.[index] || !await confirm("Este animal se omitirá sin registrar un peso. ¿Quieres continuar con el siguiente?")) return;
    setSkipped(items => [...items, queue[index]]); setIndex(current => current + 1);
  }
  function finish(result: SavedWeighing) { setSaved(items => [...items, result]); setWeighDate(result.data.date); setIndex(current => current + 1); }
  if (!allowed) return <div className="panel empty-state"><h1>Sin acceso al pesaje consecutivo</h1><p>Necesitas permisos de consulta de animales y registro de pesos.</p><Link to="/animals">Volver a los animales</Link></div>;
  const current = queue?.[index];
  return <section><div className="page-heading"><div><span className="eyebrow">REGISTRO EN CAMPO</span><h1>Pesaje consecutivo</h1><p className="muted">Selecciona los animales y registra cada peso sin recargar la página.</p></div>{queue && <span className="tag">{saved.length} guardados · {skipped.length} omitidos · {queue.length} seleccionados</span>}</div>
    {!queue ? <><div className="panel form-section"><div className="form-grid"><Field label="Finca"><select value={farmId} onChange={event => { setFarmId(event.target.value); setLotId(''); setSelected([]); setPage(1); }}><option value="">Selecciona una finca</option>{farms.data?.filter(item => item.data.isActive).map(item => <option key={item.id} value={item.id}>{item.data.name}</option>)}</select></Field><Field label="Lote"><select value={lotId} disabled={!farmId} onChange={event => { setLotId(event.target.value); setPage(1); }}><option value="">Todos los lotes</option>{lots.data?.filter(item => item.data.farmId === farmId && item.data.isActive).map(item => <option key={item.id} value={item.id}>{item.data.name}</option>)}</select></Field><Field label="Fecha inicial"><input type="date" value={weighDate} max={today()} onChange={event => setWeighDate(event.target.value)} /></Field></div>
      <form className="compact-search" onSubmit={event => { event.preventDefault(); setQuery(search.trim()); setPage(1); }}><input aria-label="Buscar animales para pesar" value={search} maxLength={100} placeholder="Arete o nombre" onChange={event => setSearch(event.target.value)} /><button className="button secondary" disabled={!farmId}>Buscar</button></form></div>
      {(farms.error || lots.error || animals.error) && <div className="panel error-banner" role="alert"><p>{farms.error || lots.error || animals.error}</p><button className="button secondary" onClick={() => { farms.reload(); lots.reload(); animals.reload(); }}>Reintentar</button></div>}
      {animals.loading && farmId && <p role="status">Cargando animales activos…</p>}
      {animals.data && <div className="selection-grid">{animals.data.items.map(animal => <label className={'panel animal-choice ' + (selected.some(item => item.id === animal.id) ? 'selected' : '')} key={animal.id}><input type="checkbox" aria-label={'Seleccionar ' + animal.internalTag} checked={selected.some(item => item.id === animal.id)} disabled={selected.length >= 50 && !selected.some(item => item.id === animal.id)} onChange={() => choose(animal)} /><span><strong>{animal.internalTag}</strong><small>{animal.name || animal.species}</small><small>{kg(animal.currentWeightKg)} · {animal.lot || 'Sin lote'}</small></span></label>)}</div>}
      {!farmId && <div className="panel empty-state"><h2>Comienza por la finca</h2><p className="muted">Solo se muestran animales activos de tus fincas asignadas.</p></div>}
      {animals.data && !animals.data.items.length && <p className="panel empty-state">No hay animales activos en esta consulta.</p>}
      {animals.data && <div className="pagination"><p className="muted">{animals.data.total} animales · página {page}</p><div><button className="button secondary" disabled={page <= 1} onClick={() => setPage(page - 1)}>Anterior</button><button className="button secondary" disabled={page * 12 >= animals.data.total} onClick={() => setPage(page + 1)}>Siguiente</button></div></div>}
      <div className="panel form-actions"><div><strong>{selected.length} animales seleccionados</strong><p className="muted">Hasta 50 por sesión. La selección se conserva entre páginas y búsquedas.</p></div><button className="button primary" disabled={!selected.length || !weighDate || weighDate > today()} onClick={() => { setQueue([...selected]); setSaved([]); setSkipped([]); setIndex(0); }}>Comenzar pesaje</button></div>
    </> : current ? <><div className="weighing-progress" aria-label="Progreso del pesaje"><span style={{ width: (index / queue.length * 100) + '%' }} /></div><div className="panel weighing-current"><div className="section-heading"><div><span className="eyebrow">ANIMAL {index + 1} DE {queue.length}</span><h2>{current.internalTag}{current.name ? ' · ' + current.name : ''}</h2><p className="muted">{current.farm} · último peso: {kg(current.currentWeightKg)}</p></div></div><WeighingForm key={current.id} animalId={current.id} initialDate={weighDate} consecutive onSaved={finish} onPendingChange={setBlocked} /><button type="button" className="text-button" disabled={blocked} onClick={() => void skip()}>Omitir este animal</button></div><p className="muted">El siguiente animal aparece solo cuando el servidor confirma el registro. La cola pendiente se conserva mientras mantengas esta página abierta.</p></>
      : <div className="panel empty-state"><span className="completion-mark" aria-hidden="true">✓</span><h2>Pesaje completado</h2><p>{saved.length} registros confirmados en el servidor.{skipped.length > 0 && " " + skipped.length + " animales omitidos sin registrar un peso."}</p><button className="button primary" onClick={() => { setQueue(null); setSelected([]); setSaved([]); setSkipped([]); animals.reload(); }}>Iniciar otro pesaje</button></div>}
    {skipped.length > 0 && <div className="panel animal-details"><h2>Animales omitidos</h2><p className="muted">{skipped.map(animal => animal.internalTag).join(", ")}. No se creó un pesaje para estos animales.</p></div>}
    {saved.length > 0 && <div className="panel animal-details"><h2>Registros confirmados</h2><ul className="saved-weighings">{saved.map(record => <li key={record.id}><Link to={'/animals/' + record.data.animalId}>{queue?.find(animal => animal.id === record.data.animalId)?.internalTag}</Link><strong>{kg(record.data.weightKg)}</strong><span className="tag">Guardado</span></li>)}</ul></div>}
  </section>;
}
