import { lazy, Suspense } from 'react';
import type { PickerProps } from './SearchPickerContent';
import { Button } from './Controls';
import { Icon } from './Icon';
const Picker = lazy(() => import('./SearchPickerContent'));
export function SearchPicker(props: PickerProps) {
  return <Suspense fallback={<Button id={props.id} type="button" className="picker-trigger" disabled aria-label={props.label}><Icon name="animal" /><span>{props.selectedLabel || props.emptyLabel}</span><Icon name="spinner" className="loading-icon" size={18} /></Button>}><Picker {...props} /></Suspense>;
}
