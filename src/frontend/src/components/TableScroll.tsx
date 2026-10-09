import { useLayoutEffect, useRef, useState } from 'react';
import type { HTMLAttributes } from 'react';
export function TableScroll({ children, className = 'table-scroll', ...props }: HTMLAttributes<HTMLDivElement>) {
 const ref = useRef<HTMLDivElement>(null);
 const [overflow, setOverflow] = useState(false);
 const [label, setLabel] = useState('Tabla con desplazamiento horizontal');
 useLayoutEffect(() => {
  const element = ref.current!;
  const update = () => {
   setOverflow(element.scrollWidth > element.clientWidth + 1);
   setLabel(element.querySelector('caption')?.textContent || element.querySelector('table')?.getAttribute('aria-label') || element.closest('article')?.querySelector('h2')?.textContent || 'Tabla con desplazamiento horizontal');
  };
  update();
  if (typeof ResizeObserver === 'undefined') return;
  const observer = new ResizeObserver(update); observer.observe(element);
  if (element.firstElementChild) observer.observe(element.firstElementChild);
  return () => observer.disconnect();
 }, [children]);
 return <div {...props} ref={ref} className={className} tabIndex={overflow ? 0 : undefined} role={overflow ? 'region' : undefined} aria-label={overflow ? label : undefined}>{children}</div>;
}
