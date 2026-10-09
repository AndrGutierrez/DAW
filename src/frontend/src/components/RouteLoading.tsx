export function RouteLoading() {
  return <div className="route-loading" role="status" aria-label="Cargando pantalla"><div className="skeleton text-skeleton" /><div className="skeleton route-skeleton" /><span className="sr-only">Preparando la pantalla solicitada…</span></div>;
}
