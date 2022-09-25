import * as React from 'react'
import { RoleEntity, AuthAdminMessage, UserEntity, MergeStrategy } from '../Signum.Entities.Authorization'
import { ValueLine, EntityStrip, TypeContext } from '@framework/Lines'
import { useForceUpdate } from '@framework/Hooks'
import { SearchValue, SearchValueLine } from '@framework/Search';
import { External, getToString } from '@framework/Signum.Entities';

export default function Role(p: { ctx: TypeContext<RoleEntity> }) {
  const forceUpdate = useForceUpdate();

  function rolesMessage(r: RoleEntity) {
    return AuthAdminMessage.DefaultAuthorization.niceToString() +
      (r.inheritsFrom.length == 0 ? (r.mergeStrategy == "Union" ? AuthAdminMessage.Everything : AuthAdminMessage.Nothing).niceToString() :
        r.inheritsFrom.length == 1 ? AuthAdminMessage.SameAs0.niceToString(getToString(r.inheritsFrom.single().element)) :
          (r.mergeStrategy == "Union" ? AuthAdminMessage.MaximumOfThe0 : AuthAdminMessage.MinumumOfThe0).niceToString(RoleEntity.niceCount(r.inheritsFrom.length)));
  }
  const ctx = p.ctx.subCtx({ readOnly: p.ctx.value.isTrivialMerge });
  return (
    <div>
      <ValueLine ctx={ctx.subCtx(e => e.name)} />
      {ctx.value.isTrivialMerge ?
        <ValueLine ctx={ctx.subCtx(e => e.isTrivialMerge)} /> :
        <ValueLine ctx={ctx.subCtx(e => e.description)} />
      }
      <br/>
      <EntityStrip ctx={ctx.subCtx(e => e.inheritsFrom)}
        iconStart={true}
        vertical={true}
        onChange={() => forceUpdate()} />
      <ValueLine ctx={ctx.subCtx(e => e.mergeStrategy)} helpText={rolesMessage(ctx.value)} onChange={() => forceUpdate()} />

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
        filterOptions: [{ token: RoleEntity.token(a => a.entity).append(u => u.inheritsFrom).any(), value: ctx.value }]
      }} />
      }

    </div>
  );
}

