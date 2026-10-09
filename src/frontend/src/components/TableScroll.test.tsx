// @vitest-environment jsdom
import { act, render, screen } from '@testing-library/react';
import { expect, it, vi } from 'vitest';
import { TableScroll } from './TableScroll';
it('adds a named keyboard stop only when the table overflows', () => {
 let measure!: () => void; const disconnect = vi.fn();
 vi.stubGlobal('ResizeObserver', class { constructor(callback: () => void) { measure = callback; } observe() {} disconnect = disconnect; });
 const view = render(<TableScroll><table><caption>Historial clínico</caption><tbody><tr><td>Registro</td></tr></tbody></table></TableScroll>);
 const wrapper = screen.getByRole('table').parentElement!;
 expect(wrapper).not.toHaveAttribute('tabindex');
 Object.defineProperty(wrapper, 'clientWidth', { configurable: true, value: 300 });
 Object.defineProperty(wrapper, 'scrollWidth', { configurable: true, value: 700 });
 act(() => measure());
 expect(screen.getByRole('region', { name: 'Historial clínico' })).toHaveAttribute('tabindex', '0');
 Object.defineProperty(wrapper, 'scrollWidth', { configurable: true, value: 300 });
 act(() => measure()); expect(wrapper).not.toHaveAttribute('tabindex');
 view.unmount(); expect(disconnect).toHaveBeenCalled(); vi.unstubAllGlobals();
});
