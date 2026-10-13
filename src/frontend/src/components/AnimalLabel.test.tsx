// @vitest-environment jsdom
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, expect, it, vi } from 'vitest';
import { AnimalLabel } from './AnimalLabel';
import type { NfcReader } from '../api/identification';
vi.mock('qrcode', () => ({ default: { toDataURL: vi.fn(async () => 'data:image/png;base64,fixture') } }));
vi.mock('./SidePanel', () => ({ SidePanel: ({ children, onRequestClose }: { children: React.ReactNode; onRequestClose: () => void }) => <div role="dialog"><button onClick={onRequestClose}>Cerrar etiqueta</button>{children}</div> }));
afterEach(() => vi.unstubAllGlobals());
it('generates an animal QR and writes its canonical identifier without overwriting an NFC tag', async () => {
  const reader: NfcReader = { scan: vi.fn(), write: vi.fn(async () => {}), onreading: null, onreadingerror: null }; vi.stubGlobal('isSecureContext', true); vi.stubGlobal('NDEFReader', function () { return reader; });
  render(<AnimalLabel id="123" tag="TAG-1" />); fireEvent.click(screen.getByRole('button', { name: 'Etiqueta QR / NFC' })); expect(await screen.findByRole('img')).toHaveAttribute('alt', 'Código QR del animal TAG-1'); expect(screen.getByRole('link', { name: 'Descargar QR' })).toHaveAttribute('download', 'animal-TAG-1.png');
  fireEvent.click(screen.getByRole('button', { name: 'Grabar en NFC vacío' })); await waitFor(() => expect(reader.write).toHaveBeenCalled()); expect(vi.mocked(reader.write).mock.calls[0][0]).toEqual({ records: [{ recordType: 'text', data: 'daw:animal:123' }] }); expect(vi.mocked(reader.write).mock.calls[0][1].overwrite).toBe(false);
  expect(await screen.findByText(/Etiqueta NFC grabada/)).toBeInTheDocument(); fireEvent.click(screen.getByRole('button', { name: 'Cerrar etiqueta' })); expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
});
it('keeps NFC failure visible and allows the QR to remain usable', async () => {
  render(<AnimalLabel id="123" tag="TAG-1" />); fireEvent.click(screen.getByRole('button', { name: 'Etiqueta QR / NFC' })); await screen.findByRole('img'); fireEvent.click(screen.getByRole('button', { name: 'Grabar en NFC vacío' })); expect(await screen.findByRole('alert')).toHaveTextContent('NFC requiere HTTPS'); expect(screen.getByRole('link', { name: 'Descargar QR' })).toBeVisible();
});
