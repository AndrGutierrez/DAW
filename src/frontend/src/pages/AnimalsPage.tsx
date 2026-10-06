import { useEffect, useState } from 'react';
import type { FormEvent } from 'react';
import { Link, useParams, useSearchParams } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { useResource } from '../api/useResource';

type Animal = {
  id: string; internalTag: string; name: string | null; species: string; breed: string | null;
  sex: string; status: string; healthStatus: string; birthDate: string | null; currentWeightKg: number | null;
  lot: string | null; farm: string; coverPhotoUrl: string | null; photoCount: number; updatedAt: string;
};
type AnimalDetail = Animal & {
  officialId: string | null; rfid: string | null; origin: string; purpose: string; birthWeightKg: number | null;
  bodyConditionScore: number | null; color: string | null; markings: string | null; paddock: string | null;
  photos: { id: string; url: string; uploadedAt: string }[]; notes: string | null; damId: string | null; sireId: string | null;
};
type AnimalPage = { items: Animal[]; total: number; page: number; pageSize: number };
const labels: Record<string, string> = {
  Active: 'Activo', Sold: 'Vendido', Dead: 'Fallecido', Transferred: 'Transferido', Lost: 'Extraviado',
  Healthy: 'Sano', UnderObservation: 'En observación', InTreatment: 'En tratamiento', Quarantine: 'Cuarentena', Critical: 'Crítico',
  Male: 'Macho', Female: 'Hembra', Born: 'Nacimiento', Purchased: 'Compra',
  Meat: 'Carne', Milk: 'Leche', Wool: 'Lana', Eggs: 'Huevos', DualPurpose: 'Doble propósito', Work: 'Trabajo',
};
const text = (value: string | null | undefined) => value ? labels[value] || value : 'Sin registro';
const kg = (value: number | null) => value === null ? 'Sin pesaje' : new Intl.NumberFormat('es-VE', { maximumFractionDigits: 2 }).format(value) + ' kg';
const date = (value: string | null) => value ? new Date(value.length === 10 ? value + 'T12:00:00' : value).toLocaleDateString('es-VE') : 'Sin registro';

function ProtectedPhoto({ path, alt }: { path: string; alt: string }) {
  const { request } = useAuth();
  const [src, setSrc] = useState('');
  const [failed, setFailed] = useState(false);
  useEffect(() => {
    const controller = new AbortController();
    let url = '';
    setSrc(''); setFailed(false);
    void request<Blob>(path, { signal: controller.signal }, true, 'blob').then(blob => {
      if (controller.signal.aborted) return;
      url = URL.createObjectURL(blob); setSrc(url);
    }).catch(() => { if (!controller.signal.aborted) setFailed(true); });
    return () => { controller.abort(); if (url) URL.revokeObjectURL(url); };
  }, [path, request]);
  return src ? <img src={src} alt={alt} loading="lazy" /> : <span className="photo-placeholder" aria-label={failed ? 'No se pudo cargar la fotografía' : 'Cargando fotografía'}>{failed ? '◇' : '…'}</span>;
}

function ResourceError({ error, reload }: { error: string; reload: () => void }) {
  return <div className="panel empty-state"><p role="alert">{error}</p><button className="button secondary" onClick={reload}>Reintentar</button></div>;
}

export function AnimalsPage() {
  const { can } = useAuth();
  const [params, setParams] = useSearchParams();
  const page = params.get('page') || '1';
  const search = params.get('search') || '';
  const status = params.get('status') || '';
  const query = new URLSearchParams({ page, pageSize: '12', ...(search ? { search } : {}), ...(status ? { status } : {}) });
  const { data, loading, error, reload } = useResource<AnimalPage>('/api/animals/page?' + query.toString(), can('animals.list'));
  function searchAnimals(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    setParams({ page: '1', search: String(form.get('search')).trim(), status: String(form.get('status')) });
  }
  if (!can('animals.list')) return <div className="panel empty-state"><h1>Acceso pendiente</h1><p>Tu cuenta no tiene permiso para consultar el ganado. Solicita la asignación de permisos y fincas al administrador.</p></div>;
  return <section>
    <div className="page-heading"><div><span className="eyebrow">EL CENTRO DE TU FINCA</span><h1>Animales</h1><p className="muted">Conoce cada ejemplar y consulta su información.</p></div><div className="record-counter"><strong>{data?.total ?? '—'}</strong><span>en esta consulta</span></div></div>
    <form className="panel search-bar" onSubmit={searchAnimals} key={search + status}>
      <div className="search-field"><label htmlFor="search">Buscar animal</label><input id="search" name="search" defaultValue={search} maxLength={100} placeholder="Arete, nombre o identificación oficial" /></div>
      <div><label htmlFor="status">Estado</label><select id="status" name="status" defaultValue={status}><option value="">Todos los estados</option>{['Active', 'Sold', 'Dead', 'Transferred', 'Lost'].map(value => <option key={value} value={value}>{labels[value]}</option>)}</select></div>
      <button className="button primary" type="submit">Buscar</button>
    </form>
    {loading ? <div className="animal-grid" role="status" aria-label="Cargando animales">{Array.from({ length: 6 }, (_, index) => <div className="panel animal-card skeleton-card" key={index}><div className="skeleton photo-skeleton" /><div className="skeleton text-skeleton" /><div className="skeleton text-skeleton short" /></div>)}</div>
      : error ? <ResourceError error={error} reload={reload} />
      : data?.items.length ? <><div className="animal-grid">{data.items.map(animal => <article className="panel animal-card" key={animal.id}>
        <div className="animal-card-photo">{animal.coverPhotoUrl && can('photos.get') ? <ProtectedPhoto path={animal.coverPhotoUrl} alt={'Fotografía de ' + (animal.name || animal.internalTag)} /> : <div className="animal-monogram" aria-hidden="true">{animal.internalTag.slice(-3)}</div>}<span className={'status-pill ' + (animal.status === 'Active' ? 'positive' : '')}>{text(animal.status)}</span></div>
        <div className="animal-card-content"><div className="animal-card-title"><h2>{animal.name || animal.internalTag}</h2><span className="tag">{animal.internalTag}</span></div><p className="muted">{animal.species} · {text(animal.sex)}</p>
          <dl className="animal-card-facts"><div><dt>Peso actual</dt><dd>{kg(animal.currentWeightKg)}</dd></div><div><dt>Salud</dt><dd>{text(animal.healthStatus)}</dd></div></dl>
          <div className="animal-card-footer"><span>{animal.farm}</span>{can('animals.get') && <Link to={'/animals/' + animal.id} aria-label={'Ver ficha de ' + animal.internalTag}>Ver ficha <span aria-hidden="true">↗</span></Link>}</div>
        </div>
      </article>)}</div><div className="pagination"><p className="muted">Página {data.page} de {Math.max(1, Math.ceil(data.total / data.pageSize))} · {data.total} animales</p><div><button className="button secondary" disabled={data.page <= 1} onClick={() => setParams({ page: String(data.page - 1), search, status })}>Anterior</button><button className="button secondary" disabled={data.page * data.pageSize >= data.total} onClick={() => setParams({ page: String(data.page + 1), search, status })}>Siguiente</button></div></div></>
      : <div className="panel empty-state"><span className="empty-symbol" aria-hidden="true">◇</span><h2>No hay animales en esta consulta</h2><p className="muted">Prueba otro nombre o estado. Solo se muestran las fincas que tienes asignadas.</p></div>}
  </section>;
}

export function AnimalPage() {
  const { id } = useParams();
  const { can } = useAuth();
  const { data: animal, loading, error, reload } = useResource<AnimalDetail>('/api/animals/' + id, can('animals.get'));
  if (!can('animals.get')) return <div className="panel empty-state"><h1>Sin acceso a esta ficha</h1><p>Solicita el permiso de consulta al administrador.</p></div>;
  if (loading) return <div className="panel detail-skeleton" role="status" aria-label="Cargando ficha"><div className="skeleton photo-skeleton" /><div className="skeleton text-skeleton" /></div>;
  if (error) return <ResourceError error={error} reload={reload} />;
  if (!animal) return null;
  const fields = [
    ['Identificación oficial', animal.officialId], ['RFID', animal.rfid], ['Especie', animal.species], ['Raza', animal.breed],
    ['Sexo', text(animal.sex)], ['Nacimiento', date(animal.birthDate)], ['Peso al nacer', kg(animal.birthWeightKg)],
    ['Origen', text(animal.origin)], ['Propósito', text(animal.purpose)], ['Finca', animal.farm], ['Lote', animal.lot],
    ['Potrero', animal.paddock], ['Condición corporal', animal.bodyConditionScore?.toString()], ['Color', animal.color],
  ];
  return <section><Link className="back-link" to="/animals">← Volver a los animales</Link><div className="page-heading"><div><span className="eyebrow">{animal.internalTag}</span><h1>{animal.name || 'Ficha del animal'}</h1><p className="muted">{animal.species} · {animal.farm}</p></div><span className={'status-pill ' + (animal.status === 'Active' ? 'positive' : '')}>{text(animal.status)}</span></div>
    <div className="detail-top"><div className="panel detail-cover">{animal.photos.length && can('photos.get') ? <ProtectedPhoto path={animal.photos[0].url} alt={'Fotografía de ' + animal.internalTag} /> : <div className="detail-monogram">{animal.internalTag}<small>Sin fotografía disponible</small></div>}</div><div className="panel detail-summary"><span className="eyebrow">SEGUIMIENTO ACTUAL</span><h2>{kg(animal.currentWeightKg)}</h2><p className="muted">Último peso registrado</p><div className="summary-health"><span className="status-dot" />{text(animal.healthStatus)}</div><p className="muted">Última actualización: {date(animal.updatedAt)}</p></div></div>
    <div className="panel animal-details"><h2>Identificación y ubicación</h2><dl className="detail-grid">{fields.map(([label, value]) => <div key={label}><dt>{label}</dt><dd>{value || 'Sin registro'}</dd></div>)}</dl></div>
    <div className="panel animal-details"><h2>Genealogía</h2><div className="parent-links">{animal.damId ? <Link className="button secondary" to={'/animals/' + animal.damId}>Consultar madre ↗</Link> : <span className="muted">Madre sin registro</span>}{animal.sireId ? <Link className="button secondary" to={'/animals/' + animal.sireId}>Consultar padre ↗</Link> : <span className="muted">Padre sin registro</span>}</div></div>
    {animal.notes && <div className="panel animal-details"><h2>Observaciones</h2><p className="animal-notes">{animal.notes}</p></div>}
    {animal.photos.length > 1 && can('photos.get') && <div className="panel animal-details"><h2>Fotografías</h2><div className="photo-gallery">{animal.photos.slice(1).map(photo => <ProtectedPhoto key={photo.id} path={photo.url} alt={'Fotografía de ' + animal.internalTag} />)}</div></div>}
  </section>;
}
