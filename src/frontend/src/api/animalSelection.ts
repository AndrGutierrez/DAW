import type { Animal, AnimalPageResult } from './livestock';

export async function readAllAnimalResults(
  filters: string,
  read: (path: string, options: { signal: AbortSignal }) => Promise<AnimalPageResult>,
  signal: AbortSignal,
): Promise<Animal[]> {
  const params = new URLSearchParams(filters);
  params.set('pageSize', '100');
  const animals = new Map<string, Animal>();
  let total: number | undefined;
  for (let page = 1; ; page++) {
    params.set('page', String(page));
    const result = await read('/api/animals/page?' + params, { signal });
    if (signal.aborted) throw new DOMException('Selection cancelled', 'AbortError');
    if (total !== undefined && total !== result.total) throw new Error('La búsqueda cambió mientras seleccionábamos. Actualiza y vuelve a intentarlo.');
    total = result.total;
    for (const animal of result.items) {
      if (animals.has(animal.id)) throw new Error('La búsqueda cambió mientras seleccionábamos. Actualiza y vuelve a intentarlo.');
      animals.set(animal.id, animal);
    }
    if (page * 100 >= total) break;
    if (!result.items.length) throw new Error('No se pudo completar la selección. Actualiza y vuelve a intentarlo.');
  }
  if (animals.size !== total) throw new Error('No se pudo completar la selección. Actualiza y vuelve a intentarlo.');
  return [...animals.values()];
}
