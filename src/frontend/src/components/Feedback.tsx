import { createContext, useCallback, useContext, useEffect, useRef, useState } from 'react';
import type { ReactNode } from 'react';
type Notice = { id: number; message: string; kind: 'success' | 'error' };
type Feedback = { notify: (message: string, kind?: Notice['kind']) => void; confirm: (message: string) => Promise<boolean> };
const Context = createContext<Feedback | null>(null);
export function FeedbackProvider({ children }: { children: ReactNode }) {
  const [notices, setNotices] = useState<Notice[]>([]);
  const [question, setQuestion] = useState('');
  const resolve = useRef<((answer: boolean) => void) | null>(null);
  const dialog = useRef<HTMLDialogElement>(null);
  const timers = useRef<ReturnType<typeof setTimeout>[]>([]);
  const sequence = useRef(0);
  const notify = useCallback((message: string, kind: Notice['kind'] = 'success') => {
    const id = ++sequence.current;
    setNotices(items => [...items.slice(-2), { id, message, kind }]);
    timers.current.push(setTimeout(() => setNotices(items => items.filter(item => item.id !== id)), 7000));
  }, []);
  const confirm = useCallback((message: string) => new Promise<boolean>(done => {
    resolve.current?.(false); resolve.current = done; setQuestion(message);
  }), []);
  useEffect(() => { if (question && !dialog.current?.open) dialog.current?.showModal(); }, [question]);
  useEffect(() => () => { timers.current.forEach(clearTimeout); resolve.current?.(false); }, []);
  function answer(value: boolean) { resolve.current?.(value); resolve.current = null; dialog.current?.close(); setQuestion(''); }
  return <Context.Provider value={{ notify, confirm }}>{children}
    <div className="toast-stack" aria-live="polite" aria-atomic="false">{notices.map(notice => <div key={notice.id} className={'toast ' + notice.kind} role={notice.kind === 'error' ? 'alert' : 'status'}><span>{notice.message}</span><button type="button" aria-label="Cerrar notificación" onClick={() => setNotices(items => items.filter(item => item.id !== notice.id))}>×</button></div>)}</div>
    <dialog ref={dialog} className="confirm-dialog" aria-labelledby="confirm-title" onCancel={event => { event.preventDefault(); answer(false); }}><h2 id="confirm-title">Confirmar acción</h2><p>{question}</p><div className="button-row"><button className="button secondary" onClick={() => answer(false)} autoFocus>Cancelar</button><button className="button danger" onClick={() => answer(true)}>Confirmar</button></div></dialog>
  </Context.Provider>;
}
export function useFeedback() {
  const value = useContext(Context);
  if (!value) throw new Error('FeedbackProvider is required.');
  return value;
}
