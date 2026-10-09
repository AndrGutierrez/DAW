// @vitest-environment jsdom
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, expect, it, vi } from 'vitest';
import { WeighingForm } from './WeighingForm';
const request = vi.hoisted(() => vi.fn());
vi.mock('../auth/AuthContext', () => ({ useAuth: () => ({ request }) }));
vi.mock('./Feedback', () => ({ useFeedback: () => ({ notify: vi.fn() }) }));
beforeEach(() => request.mockReset());
it('rejects a non-positive weight before sending it to the API', async () => {
 const user = userEvent.setup(); render(<WeighingForm animalId="animal-a" onSaved={vi.fn()} />);
 fireEvent.change(screen.getByLabelText('Peso vivo (kg) *'), { target: { value: '-1' } });
 await user.click(screen.getByRole('button', { name: 'Guardar pesaje' }));
 expect(request).not.toHaveBeenCalled();
 expect(screen.getByLabelText('Peso vivo (kg) *')).toHaveAttribute('aria-invalid', 'true');
});
it('retries an uncertain submission with the same identity and blocks editing', async () => {
 const saved = vi.fn(); request.mockRejectedValueOnce(new TypeError('Connection lost')).mockResolvedValueOnce({ id: 'weight-a', replayed: true, data: { animalId: 'animal-a', date: '2026-10-08', weightKg: 420 } });
 const user = userEvent.setup(); render(<WeighingForm animalId="animal-a" onSaved={saved} />);
 fireEvent.change(screen.getByLabelText('Peso vivo (kg) *'), { target: { value: '420' } });
 await user.click(screen.getByRole('button', { name: 'Guardar pesaje' }));
 await screen.findByRole('button', { name: 'Reintentar y confirmar pesaje' });
 expect(screen.getByLabelText('Peso vivo (kg) *')).toBeDisabled();
 const first = JSON.parse(request.mock.calls[0][1].body);
 await user.click(screen.getByRole('button', { name: 'Reintentar y confirmar pesaje' }));
 await waitFor(() => expect(saved).toHaveBeenCalled());
 const second = JSON.parse(request.mock.calls[1][1].body);
 expect(second.submissionId).toBe(first.submissionId);
 expect(second.weightKg).toBe(420);
});
