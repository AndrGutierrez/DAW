import { useTheme } from '../theme/ThemeContext';
import { Button } from './ui/Controls';
import { Icon } from './ui/Icon';

export function ThemeButton() {
  const { theme, toggle } = useTheme();
  return <Button variant="secondary" className="theme-button" onClick={toggle} aria-label={theme === 'light' ? 'Activar tema oscuro' : 'Activar tema claro'}><Icon name={theme === 'light' ? 'moon' : 'sun'} /><span>{theme === 'light' ? 'Oscuro' : 'Claro'}</span></Button>;
}
