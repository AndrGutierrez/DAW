import type { SVGProps } from 'react';

const paths = {
  inventory: "M3 7l9-4 9 4v13H3V7Zm0 0 9 4 9-4m-9 4v9m-5-6h2",
  report: "M6 3h9l4 4v14H6V3Zm9 0v5h4M9 12h7m-7 4h7",
  leaf: 'M20 4c-7-1-14 1-14 8a6 6 0 0 0 6 6c7 0 9-7 8-14ZM4 20l11-11',
  animal: 'M7 8 4 5H2v5l4 2m11-4 3-3h2v5l-4 2M8 6h8l2 6-2 8H8l-2-8 2-6Zm1 9h6m-5-4h.01m4 0h.01',
  scale: 'M5 8h14v12H5V8Zm3 0a4 4 0 0 1 8 0m-4 3 2-2m-5 5h6',
  paddock: 'M3 8 12 3l9 5v12H3V8Zm0 6h18M8 6v14m8-14v14',
  user: 'M16 7a4 4 0 1 1-8 0 4 4 0 0 1 8 0ZM4 21v-2a8 8 0 0 1 16 0v2',
  moon: 'M20 14A8 8 0 0 1 10 4a8 8 0 1 0 10 10Z',
  sun: 'M16 12a4 4 0 1 1-8 0 4 4 0 0 1 8 0ZM12 2v2m0 16v2M2 12h2m16 0h2M5 5l1.5 1.5m11 11L19 19M5 19l1.5-1.5m11-11L19 5',
  plus: 'M12 5v14M5 12h14',
  search: 'M16 10a6 6 0 1 1-12 0 6 6 0 0 1 12 0Zm-1.5 4.5L21 21',
  arrow: 'M4 12h16m-6-6 6 6-6 6',
  back: 'M20 12H4m6-6-6 6 6 6',
  close: 'm6 6 12 12M6 18 18 6',
  check: 'm5 12 4 4L19 6',
  alert: 'm12 3 10 18H2L12 3Zm0 6v5m0 3h.01',
  heart: 'M20 5a5 5 0 0 0-8 1 5 5 0 0 0-8-1c-5 5 1 10 8 16 7-6 13-11 8-16Z',
  droplet: 'M12 2C9 7 5 11 5 15a7 7 0 0 0 14 0c0-4-4-8-7-13Z',
  genealogy: 'M12 3v6m-7 5V9h14v5M3 14h4v6H3v-6Zm14 0h4v6h-4v-6ZM10 3h4',
  photo: 'M3 5h18v14H3V5Zm0 11 6-6 4 4 3-3 5 5M16 8h.01',
  edit: 'm15 4 5 5M4 20l5-1L21 7a2 2 0 0 0-4-4L5 15l-1 5Z',
  location: 'M19 10c0 6-7 12-7 12S5 16 5 10a7 7 0 0 1 14 0Zm-5 0a2 2 0 1 1-4 0 2 2 0 0 1 4 0Z',
  growth: 'M3 3v18h18M6 15l5-5 4 3 6-8',
  logout: 'M9 3H3v18h6m4-15 6 6-6 6m-6-6h12',
} as const;
export type IconName = keyof typeof paths;
export function Icon({ name, size = 20, ...props }: SVGProps<SVGSVGElement> & { name: IconName; size?: number }) {
  return <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.7" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true" focusable="false" {...props}><path d={paths[name]} /></svg>;
}
