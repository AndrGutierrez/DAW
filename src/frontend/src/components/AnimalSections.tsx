import { useRef } from 'react';
import type { KeyboardEvent, ReactNode } from 'react';
import { Button } from './ui/Controls';
import { Icon } from './ui/Icon';
import type { IconName } from './ui/Icon';

export const animalSections: { id: string; label: string; icon: IconName }[] = [
  { id: 'identity', label: 'Resumen', icon: 'animal' },
  { id: 'clinical', label: 'Sanidad', icon: 'heart' },
  { id: 'reproduction', label: 'Reproducción', icon: 'leaf' },
  { id: 'production', label: 'Producción', icon: 'droplet' },
  { id: 'genealogy', label: 'Genealogía', icon: 'genealogy' },
  { id: 'location', label: 'Ubicación', icon: 'location' },
  { id: 'growth', label: 'Crecimiento', icon: 'growth' },
  { id: 'photos', label: 'Fotografías', icon: 'photo' },
];
export function AnimalSections({ active, onChange }: { active: string; onChange: (id: string) => void }) {
  const tabs = useRef<(HTMLButtonElement | null)[]>([]);
  function navigate(event: KeyboardEvent<HTMLButtonElement>, index: number) {
    const next = event.key === 'ArrowRight' ? (index + 1) % animalSections.length
      : event.key === 'ArrowLeft' ? (index + animalSections.length - 1) % animalSections.length
      : event.key === 'Home' ? 0 : event.key === 'End' ? animalSections.length - 1 : null;
    if (next === null) return;
    event.preventDefault();
    const tab = tabs.current[next];
    tab?.focus({ preventScroll: true });
    if (tab?.parentElement) {
      const parent = tab.parentElement, bounds = parent.getBoundingClientRect(), item = tab.getBoundingClientRect();
      if (item.left < bounds.left) parent.scrollLeft += item.left - bounds.left - 8;
      if (item.right > bounds.right) parent.scrollLeft += item.right - bounds.right + 8;
    }
  }
  return <div className="animal-section-links" role="tablist" aria-label="Secciones de la ficha">{animalSections.map((section, index) => <Button type="button" role="tab" className="animal-tab" id={'animal-tab-' + section.id} aria-controls={'animal-panel-' + section.id} aria-selected={active === section.id} tabIndex={active === section.id ? 0 : -1} ref={element => { tabs.current[index] = element; }} key={section.id} onKeyDown={event => navigate(event, index)} onClick={() => onChange(section.id)}><Icon name={section.icon} size={17} />{section.label}</Button>)}</div>;
}
export function AnimalPanel({ id, active, children }: { id: string; active: string; children: ReactNode }) {
  return <div className="animal-tab-panel" id={'animal-panel-' + id} role="tabpanel" aria-labelledby={'animal-tab-' + id} tabIndex={0} hidden={active !== id}>{children}</div>;
}
