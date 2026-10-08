import { forwardRef, useId, useImperativeHandle, useRef, useState } from 'react';
import type { InputHTMLAttributes } from 'react';
import { Button, Input } from './Controls';
import { Icon } from './Icon';

export const PasswordInput = forwardRef<HTMLInputElement, Omit<InputHTMLAttributes<HTMLInputElement>, 'type'>>(function PasswordInput({ id: suppliedId, 'aria-describedby': describedBy, ...props }, ref) {
  const generatedId = useId();
  const id = suppliedId || generatedId;
  const input = useRef<HTMLInputElement>(null);
  useImperativeHandle(ref, () => input.current!);
  const [visible, setVisible] = useState(false);
  const [capsLock, setCapsLock] = useState(false);
  return <div className="password-field">
    <div className="password-control"><Input {...props} id={id} ref={input} type={visible ? 'text' : 'password'} aria-describedby={[describedBy, capsLock ? id + '-caps' : ''].filter(Boolean).join(' ') || undefined}
      onKeyDown={event => { setCapsLock(event.getModifierState('CapsLock')); props.onKeyDown?.(event); }}
      onKeyUp={event => { setCapsLock(event.getModifierState('CapsLock')); props.onKeyUp?.(event); }}
      onBlur={event => { setCapsLock(false); props.onBlur?.(event); }} />
      <Button className="icon-button password-toggle" type="button" disabled={props.disabled || props.readOnly} aria-label={visible ? 'Ocultar contraseña' : 'Mostrar contraseña'} aria-pressed={visible} aria-controls={id}
        onPointerDown={event => event.preventDefault()} onClick={event => { const start = input.current?.selectionStart; const end = input.current?.selectionEnd; setVisible(!visible); if (event.detail > 0) { input.current?.focus(); requestAnimationFrame(() => { if (start != null && end != null) input.current?.setSelectionRange(start, end); }); } }}>
        <Icon name={visible ? 'eyeOff' : 'eye'} />
      </Button>
    </div>
    {capsLock && <small id={id + '-caps'} className="password-hint" role="status"><Icon name="alert" size={16} />Bloq Mayús está activado.</small>}
  </div>;
});
