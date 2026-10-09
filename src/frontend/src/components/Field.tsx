import { useId } from 'react';
import type { ReactElement } from 'react';
import { cloneElement } from 'react';
export function Field({ label, error, hint, children }: { label: string; error?: string; hint?: string; children: ReactElement<Record<string, unknown>> }) {
  const id = useId();
  return <div className={'form-field ' + (error ? 'has-error' : '')}><label htmlFor={id}>{label}</label>{cloneElement(children, { id, 'aria-invalid': !!error, 'aria-describedby': error || hint ? id + '-help' : undefined })}
    {error ? <span id={id + '-help'} className="field-error">{error}</span> : hint ? <small id={id + '-help'} className="muted">{hint}</small> : null}</div>;
}
