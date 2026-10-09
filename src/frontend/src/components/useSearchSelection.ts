import { useEffect, useRef, useState } from 'react';
import type { Dispatch, SetStateAction } from 'react';
import { useAuth } from '../auth/AuthContext';
import type { Animal, AnimalPageResult } from '../api/livestock';
import { readAllAnimalResults } from '../api/animalSelection';
import { errorMessage } from '../api/errors';

export function useSearchSelection(filters: string, setSelected: Dispatch<SetStateAction<Animal[]>>) {
  const { request } = useAuth();
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const controller = useRef<AbortController | null>(null);
  const currentFilters = useRef(filters);
  currentFilters.current = filters;
  useEffect(() => {
    controller.current?.abort(); controller.current = null; setBusy(false); setError('');
    return () => { controller.current?.abort(); controller.current = null; };
  }, [filters]);
  async function selectAll() {
    if (controller.current) return;
    const active = new AbortController(); controller.current = active; setBusy(true); setError('');
    try {
      const animals = await readAllAnimalResults(filters, (path, options) => request<AnimalPageResult>(path, options), active.signal);
      if (active.signal.aborted || currentFilters.current !== filters) return;
      setSelected(previous => [...new Map([...previous, ...animals].map(animal => [animal.id, animal])).values()]);
    } catch (failure) {
      if (!active.signal.aborted && currentFilters.current === filters)
        setError(failure instanceof Error && /^(La búsqueda|No se pudo completar)/.test(failure.message) ? failure.message : errorMessage(failure));
    } finally {
      if (controller.current === active) { controller.current = null; setBusy(false); }
    }
  }
  return { selectAll, busy, error };
}
