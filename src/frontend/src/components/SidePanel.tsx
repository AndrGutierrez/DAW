import { useEffect, useId, useRef } from 'react';
import type { ReactNode } from 'react';
import { Button } from './ui/Controls';
import { Icon } from './ui/Icon';

export function SidePanel({ title, onRequestClose, children }: { title: string; onRequestClose: () => void; children: ReactNode }) {
  const dialog = useRef<HTMLDialogElement>(null);
  const titleId = useId();
  useEffect(() => {
    const element = dialog.current;
    const previousOverflow = document.body.style.overflow;
    const trigger = document.activeElement instanceof HTMLElement ? document.activeElement : null;
    element?.showModal();
    document.body.style.overflow = 'hidden';
    return () => { element?.close(); document.body.style.overflow = previousOverflow; if (trigger?.isConnected) trigger.focus({ preventScroll: true }); };
  }, []);
  return <dialog ref={dialog} className="side-panel" aria-labelledby={titleId} onCancel={event => { event.preventDefault(); onRequestClose(); }} onClick={event => {
    if (event.target !== event.currentTarget) return;
    const bounds = event.currentTarget.getBoundingClientRect();
    if (event.clientX < bounds.left || event.clientX > bounds.right || event.clientY < bounds.top || event.clientY > bounds.bottom) onRequestClose();
  }}><header className="side-panel-header"><div><span className="eyebrow">OCUPACIÓN DEL POTRERO</span><h2 id={titleId}>{title}</h2></div><Button type="button" className="icon-button" aria-label="Cerrar detalle del potrero" autoFocus onClick={onRequestClose}><Icon name="close" /></Button></header><div className="side-panel-body">{children}</div></dialog>;
}
