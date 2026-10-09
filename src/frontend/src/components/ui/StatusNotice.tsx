import { forwardRef, useId } from 'react';
import type { ReactNode } from 'react';
import { Icon } from './Icon';

export const StatusNotice = forwardRef<HTMLDivElement, { title: string; children: ReactNode; kind?: 'error' | 'info' | 'warning' }>(function StatusNotice({ title, children, kind = 'error' }, ref) {
  const id = useId();
  return <div ref={ref} className={'status-notice ' + kind} role={kind === 'error' ? 'alert' : 'status'} tabIndex={-1} aria-labelledby={id}>
    <Icon name={kind === 'info' ? 'info' : 'alert'} size={23} />
    <div><strong id={id}>{title}</strong>{children}</div>
  </div>;
});
