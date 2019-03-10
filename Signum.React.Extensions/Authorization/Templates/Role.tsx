import * as React from 'react'
import { RoleEntity, AuthAdminMessage } from '../Signum.Entities.Authorization'
import { ValueLine, EntityList, TypeContext } from '@framework/Lines'
import { useForceUpdate } from '@framework/Hooks'

export default function Role(p : { ctx: TypeContext<RoleEntity> }){
  const forceUpdate = useForceUpdate();

  function rolesMessage() {
    return AuthAdminMessage.NoRoles.niceToString() + " ⇒ " +
      (p.ctx.value.mergeStrategy == "Union" ? AuthAdminMessage.Nothing : AuthAdminMessage.Everything).niceToString();
  }
  const ctx = p.ctx;
  return (
    <div>
      <ValueLine ctx={ctx.subCtx(e => e.name)} />
      <ValueLine ctx={ctx.subCtx(e => e.mergeStrategy)} unitText={rolesMessage()} onChange={() => forceUpdate()} />
      <EntityList ctx={ctx.subCtx(e => e.roles)} onChange={() => forceUpdate()} />
    </div>
  );
}

