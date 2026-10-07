import { useEffect, useState } from 'react';
import type { FormEvent } from 'react';
import { Link, useParams, useSearchParams } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { useResource } from '../api/useResource';

import { labels, text, kg, date } from '../api/livestock';
import type { AnimalDetail, AnimalPageResult } from '../api/livestock';
import { AnimalLocation } from '../components/AnimalLocation';
import { AnimalClinical } from '../components/AnimalClinical';
import { AnimalReproduction } from '../components/AnimalReproduction';
import { AnimalProduction } from '../components/AnimalProduction';
import { AnimalGenealogy } from '../components/AnimalGenealogy';
import { AnimalGrowth } from '../components/AnimalGrowth';
import { PhotoUploader } from '../components/PhotoUploader';
import { WeighingForm } from '../components/WeighingForm';
import { useFeedback } from '../components/Feedback';
import { errorMessage } from '../api/errors';

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
  const { data, loading, error, reload } = useResource<AnimalPageResult>('/api/animals/page?' + query.toString(), can('animals.list'));
  function searchAnimals(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    setParams({ page: '1', search: String(form.get('search')).trim(), status: String(form.get('status')) });
  }
  if (!can('animals.list')) return <div className="panel empty-state"><h1>Acceso pendiente</h1><p>Tu cuenta no tiene permiso para consultar el ganado. Solicita la asignación de permisos y fincas al administrador.</p></div>;
  return <section>
    <div className="page-heading"><div><span className="eyebrow">EL CENTRO DE TU FINCA</span><h1>Animales</h1><p className="muted">Conoce cada ejemplar y consulta su información.</p></div><div className="heading-actions">{can("weights.create") && can("animals.get") && <Link className="button secondary" to="/weighing">Pesaje consecutivo</Link>}{can("animals.create") && <Link className="button primary" to="/animals/new">Registrar animal</Link>}<div className="record-counter"><strong>{data?.total ?? '—'}</strong><span>en esta consulta</span></div></div></div>
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
  const { can, request, isAdmin } = useAuth();
  const { notify, confirm } = useFeedback();
  const [photoBusy, setPhotoBusy] = useState(false);
  const { data: animal, loading, error, reload } = useResource<AnimalDetail>('/api/animals/' + id, can('animals.get'));
  if (!can('animals.get')) return <div className="panel empty-state"><h1>Sin acceso a esta ficha</h1><p>Solicita el permiso de consulta al administrador.</p></div>;
  if (loading) return <div className="panel detail-skeleton" role="status" aria-label="Cargando ficha"><div className="skeleton photo-skeleton" /><div className="skeleton text-skeleton" /></div>;
  if (error) return <ResourceError error={error} reload={reload} />;
  if (!animal) return null;
  async function removePhoto(photoId: string) {
    if (!await confirm("¿Quieres eliminar esta fotografía de la ficha?")) return;
    setPhotoBusy(true);
    try { await request("/api/animals/" + id + "/photos/" + photoId, { method: "DELETE" }); notify("Fotografía eliminada."); reload(); }
    catch (failure) { notify(errorMessage(failure), "error"); }
    finally { setPhotoBusy(false); }
  }
  const fields = [
    ['Identificación oficial', animal.officialId], ['RFID', animal.rfid], ['Especie', animal.species], ['Raza', animal.breed],
    ['Sexo', text(animal.sex)], ['Nacimiento', date(animal.birthDate)], ['Peso al nacer', kg(animal.birthWeightKg)],
    ['Origen', text(animal.origin)], ['Propósito', text(animal.purpose)], ['Finca', animal.farm], ['Lote', animal.lot],
    ['Potrero', animal.paddock], ['Condición corporal', animal.bodyConditionScore?.toString()], ['Color', animal.color],
  ];
  return <section><Link className="back-link" to="/animals">← Volver a los animales</Link><div className="page-heading"><div><span className="eyebrow">{animal.internalTag}</span><h1>{animal.name || 'Ficha del animal'}</h1><p className="muted">{animal.species} · {animal.farm}</p></div><div className="heading-actions"><span className={'status-pill ' + (animal.status === 'Active' ? 'positive' : '')}>{text(animal.status)}</span>{can('animals.update') && <Link className="button secondary" to={'/animals/' + animal.id + '/edit'}>Editar ficha</Link>}{can('weights.create') && animal.status === 'Active' && <a className="button primary" href="#weigh">Registrar peso</a>}</div></div>
    <div className="detail-top"><div className="panel detail-cover">{animal.photos.length && can('photos.get') ? <ProtectedPhoto path={animal.photos[0].url} alt={'Fotografía de ' + animal.internalTag} /> : <div className="detail-monogram">{animal.internalTag}<small>Sin fotografía disponible</small></div>}</div><div className="panel detail-summary"><span className="eyebrow">SEGUIMIENTO ACTUAL</span><h2>{kg(animal.currentWeightKg)}</h2><p className="muted">Último peso registrado</p><div className="summary-health"><span className="status-dot" />{text(animal.healthStatus)}</div><p className="muted">Última actualización: {date(animal.updatedAt)}</p></div></div>
    <nav className="animal-section-links" aria-label="Secciones de la ficha"><a href="#clinical">Sanidad</a><a href="#reproduction">Reproducción</a><a href="#production">Producción</a><a href="#genealogy">Genealogía</a><a href="#location">Ubicación</a></nav>
    <div id="clinical"><AnimalClinical key={animal.id} animal={animal} onAnimalChanged={reload} /></div>
    <div id="reproduction"><AnimalReproduction key={animal.id} animal={animal} /></div>
    <div id="production"><AnimalProduction key={animal.id} animal={animal} onAnimalChanged={reload} /></div>
    <div id="genealogy"><AnimalGenealogy key={animal.id} animal={animal} /></div>
    <AnimalLocation key={animal.id} animal={animal} onUpdated={reload} />
    <AnimalGrowth key={animal.id} animalId={animal.id} />
    {can("weights.create") && animal.status === "Active" && <section id="weigh" className="panel animal-details"><h2>Registrar un pesaje</h2><WeighingForm animalId={animal.id} onSaved={reload} /></section>}
    <div className="panel animal-details"><h2>Identificación y ubicación</h2><dl className="detail-grid">{fields.map(([label, value]) => <div key={label}><dt>{label}</dt><dd>{value || 'Sin registro'}</dd></div>)}</dl></div>
    {animal.notes && <div className="panel animal-details"><h2>Observaciones</h2><p className="animal-notes">{animal.notes}</p></div>}
    <section className="panel animal-details"><h2>Fotografías</h2>{can("photos.create") && <PhotoUploader animalId={animal.id} onSaved={reload} />}{can("photos.get") && animal.photos.length > 0 && <div className="photo-gallery">{animal.photos.map(photo => <figure key={photo.id}><ProtectedPhoto path={photo.url} alt={"Fotografía de " + animal.internalTag} /><figcaption><span>{date(photo.uploadedAt)}</span>{isAdmin && can("photos.delete") && <button className="text-button danger-text" disabled={photoBusy} onClick={() => void removePhoto(photo.id)} aria-label="Eliminar fotografía">Eliminar</button>}</figcaption></figure>)}</div>}{!animal.photos.length && <p className="muted">Aún no hay fotografías registradas.</p>}</section>
  </section>;
}
