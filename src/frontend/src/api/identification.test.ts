import { describe, expect, it } from 'vitest';
import { animalLabel, normalizeIdentifier, readNfcIdentifier } from './identification';
const id = '12345678-1234-1234-1234-123456789abc';
describe('animal identifiers', () => {
  it('accepts the animal label and a same-origin animal URL', () => { expect(normalizeIdentifier(' ' + animalLabel(id) + ' ', 'https://farm.test')).toBe(animalLabel(id)); expect(normalizeIdentifier('https://farm.test/animals/' + id, 'https://farm.test')).toBe(animalLabel(id)); });
  it.each(['https://other.test/animals/' + id, 'javascript:alert(1)', 'data:text/plain,animal', 'daw:animal:invalid', '', 'A'.repeat(161)])('rejects unsafe or malformed content: %s', value => { expect(() => normalizeIdentifier(value, 'https://farm.test')).toThrow(); });
  it('decodes a UTF-8 NFC NDEF text record', () => { const bytes = new TextEncoder().encode('RFID-123'); expect(readNfcIdentifier({ message: { records: [{ recordType: 'text', encoding: 'utf-8', data: new DataView(bytes.buffer) }] } })).toBe('RFID-123'); });
  it('uses an NFC serial number when no NDEF identifier is available', () => { expect(readNfcIdentifier({ message: { records: [] }, serialNumber: '04:AB:12' })).toBe('04:AB:12'); expect(() => readNfcIdentifier({ message: { records: [] } })).toThrow(); });
});
