import { useEffect, useRef, useState } from 'react';
import { animalLabel, createNfcReader } from '../api/identification';
import { errorMessage } from '../api/errors';
import { Button } from './ui/Controls';
import { SidePanel } from './SidePanel';

export function AnimalLabel({ id, tag }: { id: string; tag: string }) {
  const [open, setOpen] = useState(false), [qr, setQr] = useState(''), [error, setError] = useState(''), [status, setStatus] = useState(''), [writing, setWriting] = useState(false);
  const abort = useRef<AbortController | null>(null);
  const value = animalLabel(id);
  useEffect(() => { if (!open) return; let active = true; setQr(''); setError(''); void import('qrcode').then(module => module.default.toDataURL(value, { width: 320, margin: 2, errorCorrectionLevel: 'M' })).then(url => { if (active) setQr(url); }).catch(failure => { if (active) setError(errorMessage(failure)); }); return () => { active = false; abort.current?.abort(); }; }, [open, value]);
  function close() { abort.current?.abort(); setWriting(false); setOpen(false); }
  async function write() {
    abort.current?.abort(); const controller = new AbortController(); abort.current = controller; setWriting(true); setError(''); setStatus('Acerca una etiqueta NFC vacía para grabar la identificación.');
    try { await createNfcReader().write({ records: [{ recordType: 'text', data: value }] }, { overwrite: false, signal: controller.signal }); if (!controller.signal.aborted) setStatus('Etiqueta NFC grabada. Puedes comprobarla con «Leer QR / NFC».'); }
    catch (failure) { if (!controller.signal.aborted) { setError(errorMessage(failure)); setStatus(''); } }
    finally { if (!controller.signal.aborted) setWriting(false); }
  }
  return <><Button type="button" variant="secondary" onClick={() => setOpen(true)}>Etiqueta QR / NFC</Button>{open && <SidePanel title={'Etiqueta de ' + tag} eyebrow="IDENTIFICACIÓN DEL ANIMAL" closeLabel="Cerrar etiqueta" onRequestClose={close}>
    <p>La etiqueta identifica este animal. Para abrir su ficha se requiere una sesión autorizada y seleccionar su finca.</p>
    {qr ? <><img className="animal-qr" src={qr} alt={'Código QR del animal ' + tag} /><a className="button secondary" href={qr} download={'animal-' + tag.replace(/[^a-z0-9-]/gi, '_') + '.png'}>Descargar QR</a></> : <p role="status">Generando QR…</p>}
    <p className="muted">{value}</p><Button type="button" disabled={writing} onClick={() => void write()}>{writing ? 'Esperando etiqueta…' : 'Grabar en NFC vacío'}</Button>{writing && <Button type="button" onClick={() => { abort.current?.abort(); setWriting(false); setStatus('Grabación cancelada.'); }}>Cancelar grabación</Button>}
    {status && <p role="status">{status}</p>}{error && <p role="alert" className="error-banner">{error}</p>}
    <p className="muted">La grabación requiere HTTPS y un dispositivo compatible. Se conserva el contenido de etiquetas NFC que ya estén grabadas.</p>
  </SidePanel>}</>;
}
