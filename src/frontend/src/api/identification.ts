export const animalLabel = (id: string) => 'daw:animal:' + id;
export function normalizeIdentifier(value: string, origin = window.location.origin): string {
  const text = value.trim();
  if (!text || text.length > 160 || /[\u0000-\u001f]/.test(text)) throw new Error('La etiqueta no contiene una identificación válida.');
  if (/^https?:\/\//i.test(text)) {
    const url = new URL(text); const match = url.pathname.match(/^\/animals\/([0-9a-f-]{36})\/?$/i);
    if (url.origin !== origin || !match) throw new Error('El QR pertenece a otro sitio o no es una ficha de este sistema.');
    return animalLabel(match[1]);
  }
  if (/^(javascript|data|file):/i.test(text)) throw new Error('El contenido de la etiqueta no es una identificación de animal.');
  if (/^daw:animal:/i.test(text) && !/^daw:animal:[0-9a-f]{8}(-[0-9a-f]{4}){3}-[0-9a-f]{12}$/i.test(text)) throw new Error('La etiqueta del animal está incompleta.');
  return text;
}
export type NfcRecord = { recordType: string; encoding?: string; data: DataView };
export type NfcEvent = { message: { records: NfcRecord[] }; serialNumber?: string };
export type NfcReader = { scan: (options: { signal: AbortSignal }) => Promise<void>; write: (message: { records: { recordType: string; data: string }[] }, options: { overwrite: boolean; signal: AbortSignal }) => Promise<void>; onreading: ((event: NfcEvent) => void) | null; onreadingerror: (() => void) | null };
export function createNfcReader(): NfcReader {
  const Reader = (window as unknown as { NDEFReader?: new () => NfcReader }).NDEFReader;
  if (!Reader || !window.isSecureContext) throw new Error('NFC requiere HTTPS y un navegador compatible en un dispositivo con NFC.');
  return new Reader();
}
export function readNfcIdentifier(event: NfcEvent) {
  const record = event.message.records.find(r => r.recordType === 'text' || r.recordType === 'url');
  if (record) return new TextDecoder(record.encoding || 'utf-8').decode(record.data);
  if (event.serialNumber) return event.serialNumber;
  throw new Error('La etiqueta NFC no contiene texto, URL ni identificador legible.');
}
