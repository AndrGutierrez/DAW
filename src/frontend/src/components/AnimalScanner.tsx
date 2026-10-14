import { useEffect, useRef, useState } from 'react';
import type { FormEvent } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { useResource } from '../api/useResource';
import type { AnimalDetail, CatalogItem } from '../api/livestock';
import { errorMessage } from '../api/errors';
import { createNfcReader, normalizeIdentifier, readNfcIdentifier } from '../api/identification';
import { Button, Input, Select } from './ui/Controls';
import { SidePanel } from './SidePanel';
import { Field } from './Field';

export function AnimalScanner({ farmId: selectedFarm, onIdentified }: { farmId?: string; onIdentified?: (animal: AnimalDetail) => void }) {
  const { can, request } = useAuth(); const navigate = useNavigate();
  const [open, setOpen] = useState(false), [farmId, setFarmId] = useState(selectedFarm || ''), [code, setCode] = useState('');
  const [error, setError] = useState(''), [status, setStatus] = useState(''), [busy, setBusy] = useState(false), [scanning, setScanning] = useState(false);
  const [missing, setMissing] = useState(false);
  const farms = useResource<CatalogItem[]>('/api/farms', open && can('farms.list'));
  const video = useRef<HTMLVideoElement>(null); const controls = useRef<{ stop: () => void } | null>(null);
  const abort = useRef<AbortController | null>(null), generation = useRef(0);
  useEffect(() => { if (selectedFarm) setFarmId(selectedFarm); }, [selectedFarm]);
  useEffect(() => { const active = farms.data?.filter(f => f.data.isActive); if (!farmId && active?.length === 1) setFarmId(active[0].id); }, [farmId, farms.data]);
  function stop() { generation.current++; controls.current?.stop(); controls.current = null; abort.current?.abort(); abort.current = null; setScanning(false); const stream = video.current?.srcObject; if (typeof MediaStream !== "undefined" && stream instanceof MediaStream) stream.getTracks().forEach(track => track.stop()); }
  useEffect(() => { const hidden = () => { if (document.hidden) stop(); }; document.addEventListener('visibilitychange', hidden); return () => { document.removeEventListener('visibilitychange', hidden); generation.current++; controls.current?.stop(); abort.current?.abort(); }; }, []);
  function close() { stop(); setBusy(false); setOpen(false); }
  function accept(value: string) { try { const normalized = normalizeIdentifier(value); stop(); setCode(normalized); setError(''); setMissing(false); setStatus('Identificación leída. Busca el animal en la finca seleccionada.'); } catch (failure) { stop(); setError(errorMessage(failure)); } }
  async function camera() {
    stop(); setError(''); setStatus(''); setScanning(true); const current = generation.current;
    try {
      if (!window.isSecureContext || !navigator.mediaDevices?.getUserMedia) throw new Error('La cámara requiere HTTPS o localhost y permiso del navegador. Puedes cargar una imagen QR.');
      const { BrowserQRCodeReader } = await import('@zxing/browser');
      if (current !== generation.current || !video.current) return;
      const result = await new BrowserQRCodeReader().decodeFromConstraints({ video: { facingMode: 'environment' }, audio: false }, video.current, (result, _error, scanner) => { if (result && current === generation.current) { scanner.stop(); accept(result.getText()); } });
      if (current !== generation.current) result.stop(); else controls.current = result;
    } catch (failure) { if (current === generation.current) { stop(); setError(errorMessage(failure)); } }
  }
  async function image(file?: File) {
    if (!file) return; stop(); setError(''); setStatus(''); const current = generation.current;
    if (!['image/png', 'image/jpeg', 'image/webp'].includes(file.type) || file.size > 10 * 1024 * 1024) { setError('Carga una imagen PNG, JPEG o WebP de hasta 10 MB.'); return; }
    setBusy(true); const url = URL.createObjectURL(file);
    try { const { BrowserQRCodeReader } = await import('@zxing/browser'); const result = await new BrowserQRCodeReader().decodeFromImageUrl(url); if (current === generation.current) accept(result.getText()); }
    catch { if (current === generation.current) setError('No se encontró un QR legible en la imagen. Prueba una fotografía más nítida.'); }
    finally { URL.revokeObjectURL(url); if (current === generation.current || current + 1 === generation.current) setBusy(false); }
  }
  async function nfc() {
    stop(); setError(''); setStatus(''); const current = generation.current; abort.current = new AbortController();
    try { const reader = createNfcReader(); reader.onreading = event => { if (current === generation.current) { try { accept(readNfcIdentifier(event)); } catch (failure) { stop(); setError(errorMessage(failure)); } } }; reader.onreadingerror = () => { if (current === generation.current) setError('No se pudo leer la etiqueta NFC. Acércala de nuevo.'); }; await reader.scan({ signal: abort.current.signal }); if (current === generation.current) { setScanning(true); setStatus('Acerca la etiqueta NFC al dispositivo.'); } }
    catch (failure) { if (current === generation.current) { stop(); setError(errorMessage(failure)); } }
  }
  async function identify(event: FormEvent) {
    event.preventDefault(); if (busy || !farmId) return; stop(); const current = generation.current; const controller = new AbortController(); abort.current = controller; setBusy(true); setError(''); setMissing(false);
    try { const identifier = normalizeIdentifier(code); const animal = await request<AnimalDetail>('/api/animals/identify?' + new URLSearchParams({ farmId, identifier }), { signal: controller.signal }); if (current !== generation.current) return; close(); if (onIdentified) onIdentified(animal); else navigate('/animals/' + animal.id); }
    catch (failure) { if (current === generation.current) { setError(errorMessage(failure)); setMissing((failure as { status?: number }).status === 404); } }
    finally { if (current === generation.current) setBusy(false); }
  }
  if (!can('animals.list') || !can('animals.get') || !can('farms.list')) return null;
  return <><Button type="button" variant="secondary" onClick={() => { setOpen(true); setError(''); setStatus(''); }}>Leer QR / NFC</Button>{open && <SidePanel title="Identificar animal" eyebrow="QR, NFC O LECTOR EXTERNO" closeLabel="Cerrar lector" onRequestClose={close}>
    <p className="muted">Lee un QR con la cámara, carga su fotografía o acerca una etiqueta NFC. También puedes pegar la identificación de un lector externo.</p>
    <form onSubmit={event => void identify(event)}><Field label="Finca de la etiqueta"><Select required value={farmId} disabled={!!selectedFarm || busy} onChange={event => { stop(); setFarmId(event.target.value); setMissing(false); }}><option value="">Selecciona una finca</option>{farms.data?.filter(f => f.data.isActive).map(f => <option value={f.id} key={f.id}>{f.data.name}</option>)}</Select></Field>
      <Field label="Identificación leída"><Input required maxLength={160} value={code} disabled={busy} onChange={event => { setCode(event.target.value); setMissing(false); }} placeholder="Arete, RFID o etiqueta del animal" /></Field>
      <div className="button-row"><Button type="button" disabled={busy} onClick={() => void camera()}>Abrir cámara QR</Button><Button type="button" disabled={busy} onClick={() => void nfc()}>Leer NFC</Button>{scanning && <Button type="button" onClick={stop}>Detener lectura</Button>}</div>
      <Field label="Cargar imagen QR"><Input type="file" accept="image/png,image/jpeg,image/webp" disabled={busy} onChange={event => { const file = event.target.files?.[0]; event.target.value = ''; void image(file); }} /></Field>
      <video ref={video} className={'scanner-preview' + (scanning ? '' : ' scanner-hidden')} muted playsInline aria-label="Vista de cámara QR" />
      {status && <p role="status">{status}</p>}{(error || farms.error) && <p role="alert" className="error-banner">{error || farms.error}</p>}
      <Button type="submit" variant="primary" disabled={busy || !farmId || !code.trim()}>{busy ? 'Buscando…' : 'Buscar animal identificado'}</Button>
    </form><p className="muted">NFC necesita HTTPS, etiquetas NDEF y un dispositivo compatible. Una etiqueta leída no sustituye tus permisos de acceso.</p>
    {missing && can('animals.create') && !/^daw:animal:/i.test(code) && code.trim().length <= 50 && <Link className="button secondary" to={'/animals/new?' + new URLSearchParams({ farm: farmId, rfid: code.trim() })} onClick={close}>Registrar animal con esta identificación</Link>}
  </SidePanel>}</>;
}
