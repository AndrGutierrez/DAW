// @vitest-environment jsdom
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, beforeEach, expect, it, vi } from 'vitest';
import { AnimalScanner } from './AnimalScanner';
import type { NfcReader } from '../api/identification';
const decoder = vi.hoisted(() => ({ image: vi.fn(), camera: vi.fn() }));
vi.mock('@zxing/browser', () => ({ BrowserQRCodeReader: class { decodeFromImageUrl = decoder.image; decodeFromConstraints = decoder.camera; } }));
const auth = vi.hoisted(() => ({ request: vi.fn(), can: vi.fn(() => true) }));
vi.mock('../auth/AuthContext', () => ({ useAuth: () => auth }));
vi.mock('../api/useResource', () => ({ useResource: () => ({ data: [{ id: 'farm-1', data: { name: 'Finca A', isActive: true } }], error: '', loading: false }) }));
vi.mock('./SidePanel', () => ({ SidePanel: ({ children, onRequestClose }: { children: React.ReactNode; onRequestClose: () => void }) => <div role="dialog"><button onClick={onRequestClose}>Cerrar lector</button>{children}</div> }));
beforeEach(() => { auth.request.mockReset(); auth.can.mockReset().mockReturnValue(true); });
it('resolves a manual reader identifier in the selected farm', async () => {
  const animal = { id: 'animal-1', farmId: 'farm-1' }; auth.request.mockResolvedValue(animal); const selected = vi.fn();
  render(<MemoryRouter><AnimalScanner farmId="farm-1" onIdentified={selected} /></MemoryRouter>); fireEvent.click(screen.getByRole('button', { name: 'Leer QR / NFC' })); fireEvent.change(screen.getByLabelText('Identificación leída'), { target: { value: 'RFID-123' } }); fireEvent.click(screen.getByRole('button', { name: 'Buscar animal identificado' }));
  await waitFor(() => expect(selected).toHaveBeenCalledWith(animal)); expect(auth.request.mock.calls[0][0]).toContain('farmId=farm-1&identifier=RFID-123'); expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
});
it('rejects an external QR URL before contacting the API', async () => {
  render(<MemoryRouter><AnimalScanner farmId="farm-1" /></MemoryRouter>); fireEvent.click(screen.getByRole('button', { name: 'Leer QR / NFC' })); fireEvent.change(screen.getByLabelText('Identificación leída'), { target: { value: 'https://external.test/animals/12345678-1234-1234-1234-123456789abc' } }); fireEvent.click(screen.getByRole('button', { name: 'Buscar animal identificado' })); await screen.findByRole('alert'); expect(auth.request).not.toHaveBeenCalled();
});
it('supports registration of an unknown RFID without dropping the scanned value', async () => {
  auth.request.mockRejectedValue({ status: 404 }); render(<MemoryRouter><AnimalScanner farmId="farm-1" /></MemoryRouter>); fireEvent.click(screen.getByRole('button', { name: 'Leer QR / NFC' })); fireEvent.change(screen.getByLabelText('Identificación leída'), { target: { value: 'RFID-NEW' } }); fireEvent.click(screen.getByRole('button', { name: 'Buscar animal identificado' }));
  expect(await screen.findByRole('link', { name: 'Registrar animal con esta identificación' })).toHaveAttribute('href', '/animals/new?farm=farm-1&rfid=RFID-NEW');
});
it('reports unsupported NFC and hides the reader without access permissions', async () => {
  const result = render(<MemoryRouter><AnimalScanner /></MemoryRouter>); fireEvent.click(screen.getByRole('button', { name: 'Leer QR / NFC' })); fireEvent.click(screen.getByRole('button', { name: 'Leer NFC' })); expect(await screen.findByRole('alert')).toHaveTextContent('NFC requiere HTTPS'); result.unmount(); auth.can.mockReturnValue(false); render(<MemoryRouter><AnimalScanner /></MemoryRouter>); expect(screen.queryByRole('button', { name: 'Leer QR / NFC' })).not.toBeInTheDocument();
});

afterEach(() => vi.unstubAllGlobals());
it('accepts an NDEF reading and aborts the reader when the panel closes', async () => {
  let signal: AbortSignal | undefined;
  const reader: NfcReader = { scan: vi.fn(async options => { signal = options.signal; }), write: vi.fn(), onreading: null, onreadingerror: null };
  vi.stubGlobal('isSecureContext', true); vi.stubGlobal('NDEFReader', function () { return reader; });
  render(<MemoryRouter><AnimalScanner farmId="farm-1" /></MemoryRouter>); fireEvent.click(screen.getByRole('button', { name: 'Leer QR / NFC' })); fireEvent.click(screen.getByRole('button', { name: 'Leer NFC' }));
  await waitFor(() => expect(reader.scan).toHaveBeenCalled());
  const bytes = new TextEncoder().encode('RFID-NFC');
  const { act } = await import('@testing-library/react'); act(() => reader.onreading?.({ message: { records: [{ recordType: 'text', data: new DataView(bytes.buffer) }] } }));
  expect(screen.getByLabelText('Identificación leída')).toHaveValue('RFID-NFC'); expect(signal!.aborted).toBe(true); fireEvent.click(screen.getByRole('button', { name: 'Cerrar lector' })); expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
});

it('decodes an uploaded QR locally and releases its object URL', async () => {
  Object.defineProperty(URL, 'createObjectURL', { value: vi.fn(() => 'blob:qr-fixture'), configurable: true }); Object.defineProperty(URL, 'revokeObjectURL', { value: vi.fn(), configurable: true });
  decoder.image.mockResolvedValue({ getText: () => 'RFID-IMAGE' }); render(<MemoryRouter><AnimalScanner farmId="farm-1" /></MemoryRouter>); fireEvent.click(screen.getByRole('button', { name: 'Leer QR / NFC' })); fireEvent.change(screen.getByLabelText('Cargar imagen QR'), { target: { files: [new File(['qr'], 'qr.png', { type: 'image/png' })] } });
  await waitFor(() => expect(screen.getByLabelText('Identificación leída')).toHaveValue('RFID-IMAGE')); expect(decoder.image).toHaveBeenCalledWith('blob:qr-fixture'); expect(URL.revokeObjectURL).toHaveBeenCalledWith('blob:qr-fixture'); expect(auth.request).not.toHaveBeenCalled();
});
it('rejects a non-raster upload without invoking the decoder', () => {
  decoder.image.mockClear(); render(<MemoryRouter><AnimalScanner farmId="farm-1" /></MemoryRouter>); fireEvent.click(screen.getByRole('button', { name: 'Leer QR / NFC' })); fireEvent.change(screen.getByLabelText('Cargar imagen QR'), { target: { files: [new File(['svg'], 'qr.svg', { type: 'image/svg+xml' })] } }); expect(screen.getByRole('alert')).toHaveTextContent('PNG, JPEG o WebP'); expect(decoder.image).not.toHaveBeenCalled();
});
it('stops the camera after decoding a QR and reports camera permission failures', async () => {
  vi.stubGlobal('isSecureContext', true); vi.stubGlobal('navigator', { mediaDevices: { getUserMedia: vi.fn() } }); const stop = vi.fn();
  decoder.camera.mockImplementation(async (_constraints, _video, callback) => { callback({ getText: () => 'RFID-CAMERA' }, undefined, { stop }); return { stop }; });
  render(<MemoryRouter><AnimalScanner farmId="farm-1" /></MemoryRouter>); fireEvent.click(screen.getByRole('button', { name: 'Leer QR / NFC' })); fireEvent.click(screen.getByRole('button', { name: 'Abrir cámara QR' })); await waitFor(() => expect(screen.getByLabelText('Identificación leída')).toHaveValue('RFID-CAMERA')); expect(stop).toHaveBeenCalled();
  decoder.camera.mockRejectedValue(new Error('Camera permission denied')); fireEvent.click(screen.getByRole('button', { name: 'Abrir cámara QR' })); expect(await screen.findByRole('alert')).toHaveTextContent('Camera permission denied');
});
it('reports an unreadable QR image and restores the search form', async () => {
  decoder.image.mockRejectedValue(new Error('QR not found')); render(<MemoryRouter><AnimalScanner farmId="farm-1" /></MemoryRouter>); fireEvent.click(screen.getByRole('button', { name: 'Leer QR / NFC' })); fireEvent.change(screen.getByLabelText('Cargar imagen QR'), { target: { files: [new File(['bad'], 'qr.png', { type: 'image/png' })] } }); expect(await screen.findByRole('alert')).toHaveTextContent('No se encontró un QR legible'); expect(screen.getByLabelText('Identificación leída')).toBeEnabled();
});
