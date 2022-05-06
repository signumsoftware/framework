import * as React from 'react'
import { RoleEntity, AuthAdminMessage, UserEntity } from '../Signum.Entities.Authorization'
import { ValueLine, EntityStrip, TypeContext } from '@framework/Lines'
import { useForceUpdate } from '@framework/Hooks'
import { SearchValue, SearchValueLine } from '@framework/Search';

export default function Role(p: { ctx: TypeContext<RoleEntity> }) {
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
          filterOptions: [{ token: RoleEntity.token(a => a.entity), operation: "IsNotIn", value: ctx.value.roles.map(a => a.element) }]
        }}
        onChange={() => forceUpdate()} />


      <div className="row mt-4">
        <div className="offset-sm-2">
          <h4 className="lead">Referenced by</h4>
        </div>
      </div>


      {!ctx.value.isNew && <SearchValueLine ctx={ctx} findOptions={{
        queryName: UserEntity,
        filterOptions: [{ token: UserEntity.token(u => u.entity.role), value: ctx.value }]
      }} />
      }
      {!ctx.value.isNew && <SearchValueLine ctx={ctx} findOptions={{
        queryName: RoleEntity,
        filterOptions: [{ token: RoleEntity.token(a => a.entity).append(u => u.roles).any(), value: ctx.value }]
      }} />
      }

    </div>
  );
}

