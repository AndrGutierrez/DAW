export function PaddockVisual() {
  return <svg className="paddock-landscape" viewBox="0 0 360 130" fill="none" aria-hidden="true" focusable="false">
    <path className="land-ground" d="M0 0h360v130H0z" />
    <path className="land-path" d="M-10 106c83-24 148 26 216 0 72-28 91-14 165-36" strokeWidth="18" />
    <path className="land-grass" d="M66 27h222a12 12 0 0 1 12 12v57H54V39a12 12 0 0 1 12-12Z" />
    <path className="land-fence" d="M54 40h246M54 83h246M54 27v69m41-69v69m42-69v69m42-69v69m42-69v69m42-69v69m37-69v69" strokeWidth="2" />
    <path className="land-row" d="m111 47 12 15m22-15 12 15m22-15 12 15m22-15 12 15m22-15 12 15" strokeWidth="3" />
    <path className="land-tree-trunk" d="M31 74v19m299-43v20" strokeWidth="4" />
    <path className="land-tree" d="M48 62a17 17 0 1 1-34 0 17 17 0 0 1 34 0Zm295-23a14 14 0 1 1-28 0 14 14 0 0 1 28 0Z" />
  </svg>;
}
