import { TableScroll } from '../components/TableScroll';
import { useState } from 'react';
import type { FormEvent } from 'react';
import { useAuth } from '../auth/AuthContext';
import { useResource } from '../api/useResource';
import type { CarePage } from '../api/care';
import type { CatalogItem } from '../api/livestock';
import { errorMessage, fieldErrors } from '../api/errors';
import type { FieldErrors } from '../api/errors';
import { params } from '../api/operations';
import { roleLabels } from '../api/users';
import type { ManagedUser, AccessRole, AccessPermission } from '../api/users';
import { Button, Input, Select } from '../components/ui/Controls';
import { PasswordInput } from '../components/ui/PasswordInput';
import { Field } from '../components/Field';
import { SidePanel } from '../components/SidePanel';
import { CarePagination } from '../components/CareForm';
import { useFeedback } from '../components/Feedback';
import { useUnsavedChanges } from '../components/NavigationProtection';
import { Icon } from '../components/ui/Icon';

function UserEditor({ item, roles, farms, permissions, passwordOnly, onSaved, onClose }: { item?: ManagedUser; roles: AccessRole[]; farms: CatalogItem[]; permissions: AccessPermission[]; passwordOnly: boolean; onSaved: () => void; onClose: () => void }) {
  const { request, session } = useAuth(); const { notify, confirm } = useFeedback();
  const [role,setRole]=useState(item?.roles.find(r=>roleLabels[r])||'Employee');
  const [farmIds,setFarmIds]=useState(item?.farmIds||[]), [extra,setExtra]=useState(item?.directPermissions||[]), [permissionSearch,setPermissionSearch]=useState('');
  const [dirty,setDirty]=useState(false),[busy,setBusy]=useState(false),[error,setError]=useState(''),[errors,setErrors]=useState<FieldErrors>({});
  const clearUnsaved=useUnsavedChanges(dirty); const own=item?.id===session?.user.id;
  async function close() { if(busy)return;if(dirty&&!await confirm('¿Descartar los cambios de esta cuenta?',{destructive:true}))return;clearUnsaved();onClose(); }
  function toggle(values: string[], id: string) { return values.includes(id)?values.filter(v=>v!==id):[...values,id]; }
  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();if(busy)return;const data=new FormData(event.currentTarget);
    if(passwordOnly&&!await confirm('¿Restablecer la contraseña y cerrar todas las sesiones de esta cuenta?',{destructive:true,confirmLabel:'Restablecer contraseña'}))return;
    const body=passwordOnly?{newPassword:data.get('password'),expectedVersion:item!.version}:{username:String(data.get('username')||'').trim(),email:String(data.get('email')||'').trim(),fullName:String(data.get('fullName')||'').trim(),role,farmIds,directPermissions:extra,isActive:own?item!.isActive:data.get('active')==='on',...(item?{expectedVersion:item.version}:{initialPassword:data.get('password')})};
    setBusy(true);setError('');setErrors({});
    try {await request('/api/admin/users'+(item?'/'+item.id:'')+(passwordOnly?'/password':''),{method:passwordOnly||!item?'POST':'PUT',headers:{'Content-Type':'application/json'},body:JSON.stringify(body)});clearUnsaved();setDirty(false);notify(passwordOnly?'Contraseña restablecida; sesiones anteriores cerradas.':item?'Cuenta actualizada; sus sesiones anteriores se cerraron.':'Cuenta creada.');onSaved();}
    catch(failure){setError(errorMessage(failure));setErrors(fieldErrors(failure));notify(errorMessage(failure),'error');}
    finally{setBusy(false);}
  }
  return <SidePanel eyebrow="PERSONAS Y ACCESOS" closeLabel="Cerrar gestión de cuenta" title={passwordOnly?'Restablecer contraseña':item?'Editar cuenta':'Crear cuenta'} onRequestClose={()=>void close()}>
    <p className="muted">{passwordOnly?'Define una contraseña nueva y comunícala al usuario por el canal acordado.': 'Los roles definen las operaciones permitidas. Operador y Solo lectura acceden únicamente a sus fincas asignadas.'}</p>
    <form className="care-form" onSubmit={submit} onChange={()=>setDirty(true)} aria-busy={busy}><fieldset disabled={busy}>
      {!passwordOnly&&<><Field label="Nombre completo *" error={errors.fullname}><Input name="fullName" required maxLength={200} defaultValue={item?.fullName}/></Field>
        <Field label="Usuario *" error={errors.username} hint="Entre 3 y 100 caracteres: letras, números o . _ @ + -"><Input name="username" required minLength={3} maxLength={100} pattern="[a-zA-Z0-9._@+\-]+" defaultValue={item?.username}/></Field>
        <Field label="Correo electrónico *" error={errors.email}><Input name="email" type="email" required maxLength={256} defaultValue={item?.email}/></Field>
        <Field label="Rol *" error={errors.role}><Select value={role} onChange={e=>setRole(e.target.value)} disabled={own}>{roles.filter(r=>roleLabels[r.name]).map(r=><option value={r.name} key={r.id}>{roleLabels[r.name]}</option>)}</Select></Field>
        <details className="user-access-details"><summary>Permisos del rol ({roles.find(r=>r.name===role)?.permissions.length||0})</summary><div className="permission-tags">{roles.find(r=>r.name===role)?.permissions.map(p=><span className="tag" key={p}>{p}</span>)}</div></details>
        <fieldset className="user-farm-selection"><legend>Fincas asignadas</legend>{(role==='Admin'||role==='Administrador')&&<p className="muted">Este rol accede a todas las fincas. Estas asignaciones se conservan si cambia a un rol operativo.</p>}{farms.map(f=><label className="checkbox-label" key={f.id}><Input type="checkbox" checked={farmIds.includes(f.id)} onChange={()=>setFarmIds(ids=>toggle(ids,f.id))}/>{f.data.name}</label>)}{!farms.length&&<p className="muted">No hay fincas disponibles.</p>}{!farmIds.length&&role!=='Admin'&&role!=='Administrador'&&<p className="muted">Sin fincas asignadas, esta cuenta no tendrá datos operativos.</p>}</fieldset>
        <details className="user-access-details"><summary>Permisos adicionales ({extra.length})</summary><Field label="Filtrar permisos adicionales"><Input value={permissionSearch} onChange={e=>setPermissionSearch(e.target.value)}/></Field><div className="user-permission-options">{permissions.filter(p=>p.name.includes(permissionSearch.trim())).map(p=><label className="checkbox-label" key={p.id}><Input type="checkbox" checked={extra.includes(p.name)} disabled={own} onChange={()=>setExtra(ids=>toggle(ids,p.name))}/>{p.name}</label>)}</div></details>
        <label className="checkbox-label"><Input name="active" type="checkbox" defaultChecked={item?.isActive??true} disabled={own}/>Cuenta activa</label>
      </>}
      {(!item||passwordOnly)&&<Field label={passwordOnly?'Nueva contraseña *':'Contraseña inicial *'} error={errors.password||errors.newpassword||errors.initialpassword} hint="8–256 caracteres con mayúscula, minúscula, número y símbolo."><PasswordInput name="password" required minLength={8} maxLength={256} autoComplete="new-password"/></Field>}
    </fieldset>{item&&!passwordOnly&&<p className="muted">Guardar cambios invalida las sesiones previas de esta cuenta.</p>}{error&&<p className="error-banner" role="alert">{error}</p>}<div className="button-row"><Button type="submit" disabled={busy}>{busy?'Guardando…':passwordOnly?'Restablecer contraseña':item?'Guardar cuenta':'Crear cuenta'}</Button><Button type="button" variant="secondary" disabled={busy} onClick={()=>void close()}>Cancelar</Button></div></form>
  </SidePanel>;
}
export function UsersPage() {
  const {isAdmin,can,session}=useAuth(); const allowed=isAdmin&&can('users.list');
  const [filter,setFilter]=useState({search:'',isActive:'',page:1}),[selected,setSelected]=useState<ManagedUser|'new'|null>(null),[passwordOnly,setPasswordOnly]=useState(false);
  const users=useResource<CarePage<ManagedUser>>('/api/admin/users?'+params({...filter,pageSize:12}),allowed);
  const roles=useResource<AccessRole[]>('/api/admin/roles',allowed&&can('roles.list'));
  const permissions=useResource<AccessPermission[]>('/api/admin/permissions',allowed&&can('permissions.list'));
  const farms=useResource<CatalogItem[]>('/api/farms',allowed&&can('farms.list'));
  const catalogsReady=!!roles.data&&!!permissions.data&&!!farms.data;
  function edit(item: ManagedUser|'new', password=false){setPasswordOnly(password);setSelected(item);}
  if(!allowed)return <p role="alert">La gestión de usuarios requiere acceso de administrador y permiso de consulta.</p>;
  return <section className="operations-page"><div className="page-heading"><div><span className="eyebrow">PERSONAS Y ACCESOS</span><h1>Usuarios</h1><p className="muted">Cuentas, roles y fincas. La desactivación conserva el historial del usuario.</p></div>{can('users.create')&&can('roles.manage')&&<Button variant="primary" onClick={()=>edit('new')} disabled={!catalogsReady}><Icon name="plus"/>Crear cuenta</Button>}</div>
    <form className="filter-bar" onSubmit={e=>{e.preventDefault();const f=new FormData(e.currentTarget);setFilter({page:1,search:String(f.get('search')||'').trim(),isActive:String(f.get('state')||'')});}}><Field label="Buscar usuario"><Input name="search" type="search" maxLength={100} placeholder="Nombre, usuario o correo"/></Field><Field label="Estado de la cuenta"><Select name="state" defaultValue=""><option value="">Todas las cuentas</option><option value="true">Activas</option><option value="false">Inactivas</option></Select></Field><Button type="submit">Buscar</Button></form>
    {(roles.error||permissions.error||farms.error)&&<p className="error-banner" role="alert">{roles.error||permissions.error||farms.error}</p>}
    {users.loading?<div className="panel skeleton" role="status" aria-label="Cargando usuarios" style={{minHeight:180}}/>:users.error?<div className="error-banner"><p role="alert">{users.error}</p><Button onClick={users.reload}>Reintentar</Button></div>:users.data&&<article className="panel"><TableScroll className="table-scroll"><table aria-label="Cuentas de usuario"><thead><tr><th>Persona</th><th>Rol y acceso</th><th>Estado</th><th>Acciones</th></tr></thead><tbody>{users.data.items.map(u=><tr key={u.id}><td>{u.fullName}<small className="muted">{u.username} · {u.email}</small></td><td>{u.roles.map(r=>roleLabels[r]||r).join(' · ')||'Sin rol'}<small className="muted">{u.isSuperuser||u.roles.some(r=>r==='Admin'||r==='Administrador')?'Todas las fincas':u.farmIds.length+' fincas asignadas'}{u.directPermissions.length>0?' · '+u.directPermissions.length+' permisos adicionales':''}</small></td><td><span className={'status-pill '+(u.isActive?'positive':'neutral')}>{u.isActive?'Activa':'Inactiva'}</span>{u.isSuperuser&&<small className="muted">Cuenta del sistema protegida</small>}</td><td><div className="button-row">{!u.isSuperuser&&can('users.update')&&can('roles.manage')&&<Button variant="secondary" disabled={!catalogsReady} onClick={()=>edit(u)}>Editar cuenta de {u.username}</Button>}{!u.isSuperuser&&u.id!==session?.user.id&&can('users.update')&&<Button variant="secondary" onClick={()=>edit(u,true)}>Restablecer contraseña de {u.username}</Button>}</div></td></tr>)}</tbody></table></TableScroll>{!users.data.items.length&&<p className="muted">No hay cuentas con estos filtros.</p>}<CarePagination {...users.data} onPage={page=>setFilter(f=>({...f,page}))}/></article>}
    {selected&&<UserEditor key={(selected==='new'?'new':selected.id)+passwordOnly} item={selected==='new'?undefined:selected} roles={roles.data||[]} farms={farms.data||[]} permissions={permissions.data||[]} passwordOnly={passwordOnly} onSaved={()=>{setSelected(null);users.reload();}} onClose={()=>setSelected(null)}/>}
  </section>;
}
