// @vitest-environment jsdom
import { fireEvent, render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, expect, it, vi } from 'vitest';
import { NotificationCenter } from './NotificationCenter';
const feed = vi.hoisted(() => ({ data: { items: [{ id: 'stock:1', farmId: '1', kind: 'stock', severity: 'Critical', title: 'Reponer vacuna', message: 'Finca A · saldo 0', href: '/inventory' }, { id: 'health:1', farmId: '1', kind: 'health', severity: 'Warning', title: 'Seguimiento animal', message: 'En cuarentena', href: '/animals/1' }], total: 2, generatedAt: '2026-10-12T12:00:00Z' }, loading: false, error: '', reload: vi.fn() }));
vi.mock('../api/useResource', () => ({ useResource: () => feed }));
vi.mock('../api/useOperationalRefresh', () => ({ useOperationalRefresh: vi.fn() }));
vi.mock('./SidePanel', () => ({ SidePanel: ({ children, onRequestClose }: { children: React.ReactNode; onRequestClose: () => void }) => <div role="dialog"><button onClick={onRequestClose}>Cerrar notificaciones</button>{children}</div> }));
beforeEach(() => { feed.error = ''; feed.reload.mockClear(); });
it('opens detailed notifications, filters by category and refreshes', () => {
  render(<MemoryRouter><NotificationCenter /></MemoryRouter>); fireEvent.click(screen.getByRole('button', { name: 'Notificaciones: 2 avisos activos' })); expect(feed.reload).toHaveBeenCalledTimes(1);
  expect(screen.getByText('Finca A · saldo 0')).toBeInTheDocument(); fireEvent.click(screen.getByRole('button', { name: 'Sanidad' })); expect(screen.queryByText('Reponer vacuna')).not.toBeInTheDocument(); expect(screen.getByText('En cuarentena')).toBeInTheDocument();
  fireEvent.click(screen.getByRole('button', { name: 'Actualizar avisos' })); expect(feed.reload).toHaveBeenCalledTimes(2);
});
it('closes the notification panel after navigating to a record', () => {
  render(<MemoryRouter><NotificationCenter /></MemoryRouter>); fireEvent.click(screen.getByRole('button', { name: /Notificaciones:/ })); fireEvent.click(screen.getByRole('link', { name: 'Revisar inventario' })); expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
});
it('keeps a refresh failure visible', () => { feed.error = 'Servidor no disponible'; render(<MemoryRouter><NotificationCenter /></MemoryRouter>); fireEvent.click(screen.getByRole('button', { name: /Notificaciones:/ })); expect(screen.getByRole('alert')).toHaveTextContent('Servidor no disponible'); });
