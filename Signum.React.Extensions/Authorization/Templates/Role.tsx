import * as React from 'react'
import { RoleEntity, AuthAdminMessage } from '../Signum.Entities.Authorization'
import { ValueLine, EntityStrip, TypeContext } from '@framework/Lines'
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
      <EntityStrip ctx={ctx.subCtx(e => e.roles)}
        iconStart={true}
        vertical={true}
        findOptions={{
          queryName: RoleEntity,
          filterOptions: [{ token: "Entity", operation: "IsNotIn", value: ctx.value.roles.map(a => a.element) }]
        }}
        onChange={() => forceUpdate()} />
    </div>
  );
}

