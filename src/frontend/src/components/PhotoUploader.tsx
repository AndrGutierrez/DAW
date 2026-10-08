import { Button, Input } from './ui/Controls';
import { useEffect, useRef, useState } from 'react';
import { useAuth } from '../auth/AuthContext';
import { errorMessage } from '../api/errors';
import { compressPhoto } from '../photos/compress';
import { number } from '../api/livestock';
import { useFeedback } from './Feedback';
export function PhotoUploader({ animalId, onSaved }: { animalId: string; onSaved: () => void }) {
  const { request } = useAuth();
  const { notify } = useFeedback();
  const [photo, setPhoto] = useState<File | null>(null);
  const [originalSize, setOriginalSize] = useState(0);
  const [preview, setPreview] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const sequence = useRef(0);
  useEffect(() => {
    if (!photo) { setPreview(''); return; }
    const url = URL.createObjectURL(photo); setPreview(url); return () => URL.revokeObjectURL(url);
  }, [photo]);
  useEffect(() => () => { sequence.current++; }, []);
  async function prepare(file?: File) {
    const revision = ++sequence.current;
    setPhoto(null); setError('');
    if (!file) return;
    setBusy(true); setOriginalSize(file.size);
    try { const result = await compressPhoto(file); if (revision === sequence.current) setPhoto(result); }
    catch (failure) { if (revision === sequence.current) setError(errorMessage(failure)); }
    finally { if (revision === sequence.current) setBusy(false); }
  }
  async function upload() {
    if (!photo || busy) return;
    const form = new FormData(); form.append('file', photo);
    setBusy(true); setError('');
    try { await request('/api/animals/' + animalId + '/photo', { method: 'POST', body: form }); setPhoto(null); notify('Fotografía añadida a la ficha.'); onSaved(); }
    catch (failure) { setError(errorMessage(failure)); notify(errorMessage(failure), 'error'); }
    finally { setBusy(false); }
  }
  return <div className="photo-uploader"><label className="upload-label">Añadir fotografía<Input type="file" accept="image/jpeg,image/png,image/webp" disabled={busy} onChange={event => { void prepare(event.target.files?.[0]); event.target.value = ''; }} /></label><p className="muted">JPG, PNG o WebP · hasta 20 MB. Se prepara una copia de hasta 1920 px sin los metadatos del archivo original.</p>
    {busy && <p role="status">Preparando o subiendo fotografía…</p>}{error && <p className="field-error" role="alert">{error}</p>}
    {photo && <div className="upload-preview">{preview && <img src={preview} alt="Vista previa de la fotografía preparada" />}<div><strong>Lista para subir</strong><p className="muted">Original: {number(originalSize / 1024)} KB · copia: {number(photo.size / 1024)} KB</p><div className="button-row"><Button className="button secondary" disabled={busy} onClick={() => setPhoto(null)}>Descartar</Button><Button className="button primary" disabled={busy} onClick={() => void upload()}>Subir fotografía</Button></div></div></div>}
  </div>;
}
