import { expect, it, vi } from 'vitest';
import { readAllAnimalResults } from './animalSelection';
import type { Animal } from './livestock';
const animal = (id: string) => ({ id } as Animal);
it('collects more than one page with the applied farm, lot and search', async () => {
  const read = vi.fn().mockResolvedValueOnce({ items: Array.from({ length: 100 }, (_, i) => animal(String(i))), total: 103 }).mockResolvedValueOnce({ items: [animal('100'), animal('101'), animal('102')], total: 103 });
  const result = await readAllAnimalResults('farmId=farm&lotId=lot&search=Luna&status=Active', read, new AbortController().signal);
  expect(result).toHaveLength(103);
  const query = new URLSearchParams(read.mock.calls[1][0].split('?')[1]);
  expect(Object.fromEntries(query)).toEqual({ farmId: 'farm', lotId: 'lot', search: 'Luna', status: 'Active', pageSize: '100', page: '2' });
});
it('rejects a changing population instead of returning an incomplete selection', async () => {
  const read = vi.fn().mockResolvedValueOnce({ items: Array.from({ length: 100 }, (_, i) => animal(String(i))), total: 101 }).mockResolvedValueOnce({ items: [animal('100')], total: 102 });
  await expect(readAllAnimalResults('farmId=farm', read, new AbortController().signal)).rejects.toThrow('La búsqueda cambió');
});
it('does not accept duplicate or missing rows across pages', async () => {
  const duplicate = vi.fn().mockResolvedValueOnce({ items: Array.from({ length: 100 }, (_, i) => animal(String(i))), total: 101 }).mockResolvedValueOnce({ items: [animal('0')], total: 101 });
  await expect(readAllAnimalResults('', duplicate, new AbortController().signal)).rejects.toThrow('La búsqueda cambió');
  await expect(readAllAnimalResults('', vi.fn().mockResolvedValue({ items: [], total: 1 }), new AbortController().signal)).rejects.toThrow('No se pudo completar');
});
it('discards an aborted selection', async () => {
  const controller = new AbortController();
  const read = vi.fn().mockImplementation(async () => { controller.abort(); return { items: [animal('1')], total: 1 }; });
  await expect(readAllAnimalResults('', read, controller.signal)).rejects.toHaveProperty('name', 'AbortError');
});
