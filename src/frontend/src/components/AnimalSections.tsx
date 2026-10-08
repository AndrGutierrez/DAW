import { useEffect, useState } from 'react';
import { Icon } from './ui/Icon';
import type { IconName } from './ui/Icon';

const sections: { id: string; label: string; icon: IconName }[] = [
  { id: 'identity', label: 'Resumen', icon: 'animal' },
  { id: 'clinical', label: 'Sanidad', icon: 'heart' },
  { id: 'reproduction', label: 'Reproducción', icon: 'leaf' },
  { id: 'production', label: 'Producción', icon: 'droplet' },
  { id: 'genealogy', label: 'Genealogía', icon: 'genealogy' },
  { id: 'location', label: 'Ubicación', icon: 'location' },
  { id: 'growth', label: 'Crecimiento', icon: 'growth' },
  { id: 'photos', label: 'Fotografías', icon: 'photo' },
];
export function AnimalSections() {
  const [active, setActive] = useState('identity');
  useEffect(() => {
    const observer = new IntersectionObserver(entries => {
      const first = entries.filter(entry => entry.isIntersecting).sort((a, b) => a.boundingClientRect.top - b.boundingClientRect.top)[0];
      if (first) setActive(first.target.id);
    }, { rootMargin: '-100px 0px -55% 0px' });
    sections.forEach(section => { const element = document.getElementById(section.id); if (element) observer.observe(element); });
    return () => observer.disconnect();
  }, []);
  return <nav className="animal-section-links" aria-label="Secciones de la ficha">{sections.map(section => <a href={'#' + section.id} key={section.id} aria-current={active === section.id ? 'location' : undefined} onClick={() => setActive(section.id)}><Icon name={section.icon} size={17} />{section.label}</a>)}</nav>;
}
