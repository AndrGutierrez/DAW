import { Link } from 'react-router-dom';
import { useResource } from '../api/useResource';
import type { AnimalDetail } from '../api/livestock';

function Ancestor({ id, label, depth }: { id: string | null; label: string; depth: number }) {
  const result = useResource<AnimalDetail>('/api/animals/' + id, !!id);
  return <li className="genealogy-branch"><div className="genealogy-node"><small className="muted">{label}</small>{result.data ? <Link to={'/animals/' + id}>{result.data.name || result.data.internalTag}<span className="tag">{result.data.internalTag}</span></Link> : <span>{!id ? 'Sin registro' : result.loading ? 'Cargando…' : 'Sin acceso a esta ficha'}</span>}</div>
    {result.data && depth > 0 && <ul><Ancestor id={result.data.damId} label="Madre" depth={depth - 1} /><Ancestor id={result.data.sireId} label="Padre" depth={depth - 1} /></ul>}
  </li>;
}
export function AnimalGenealogy({ animal }: { animal: AnimalDetail }) {
  return <section className="panel animal-details" aria-label="Genealogía"><h2>Genealogía</h2><p className="muted">Dos generaciones de ascendientes. Abre una ficha para seguir explorando su genealogía.</p><div className="genealogy-root"><strong>{animal.name || animal.internalTag}</strong><span className="tag">{animal.internalTag}</span></div><ul className="genealogy-tree"><Ancestor id={animal.damId} label="Madre" depth={1} /><Ancestor id={animal.sireId} label="Padre" depth={1} /></ul></section>;
}
