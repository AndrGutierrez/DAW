import { useTheme } from '../theme/ThemeContext';

export function ThemeButton() {
  const { theme, toggle } = useTheme();
  return <button className="button secondary theme-button" onClick={toggle} aria-label={theme === 'light' ? 'Activar tema oscuro' : 'Activar tema claro'}>{theme === 'light' ? '☾' : '☀'}<span>{theme === 'light' ? 'Oscuro' : 'Claro'}</span></button>;
}
