// @vitest-environment jsdom
import { render, screen } from '@testing-library/react';
import { expect, it } from 'vitest';
import { Field } from './Field';
import { Input } from './ui/Controls';
it('connects a label, hint and server validation error to its input', () => {
 const view = render(<Field label="Peso" hint="En kilogramos"><Input /></Field>);
 expect(screen.getByLabelText('Peso')).toHaveAccessibleDescription('En kilogramos');
 view.rerender(<Field label="Peso" error="El peso debe ser positivo"><Input /></Field>);
 const input = screen.getByLabelText('Peso');
 expect(input).toHaveAttribute('aria-invalid', 'true');
 expect(input).toHaveAccessibleDescription('El peso debe ser positivo');
 expect(screen.queryByText('En kilogramos')).not.toBeInTheDocument();
});
