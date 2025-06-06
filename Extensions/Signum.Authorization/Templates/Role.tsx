import * as React from 'react'
import { RoleEntity, UserEntity, MergeStrategy } from '../Signum.Authorization'
import { AutoLine, EntityStrip, TypeContext } from '@framework/Lines'
import { useForceUpdate } from '@framework/Hooks'
import { SearchValue, SearchValueLine } from '@framework/Search';
import { getToString } from '@framework/Signum.Entities';
import { AuthMessage } from '../Signum.Authorization';

export default function Role(p: { ctx: TypeContext<RoleEntity> }): React.JSX.Element {
  const forceUpdate = useForceUpdate();

  function rolesMessage(r: RoleEntity) {
    return AuthMessage.DefaultAuthorization.niceToString() +
      (r.inheritsFrom.length == 0 ? (r.mergeStrategy == "Union" ? AuthMessage.Nothing : AuthMessage.Everything).niceToString() :
        r.inheritsFrom.length == 1 ? AuthMessage.SameAs0.niceToString(getToString(r.inheritsFrom.single().element)) :
          (r.mergeStrategy == "Union" ? AuthMessage.MaximumOfThe0 : AuthMessage.MinumumOfThe0).niceToString(RoleEntity.niceCount(r.inheritsFrom.length)));
  }
  const ctx = p.ctx.subCtx({ readOnly: p.ctx.value.isTrivialMerge ? true : undefined });
  return (
    <div>
      <AutoLine ctx={ctx.subCtx(e => e.name)} />
      {ctx.value.isTrivialMerge ?
        <AutoLine ctx={ctx.subCtx(e => e.isTrivialMerge)} /> :
        <AutoLine ctx={ctx.subCtx(e => e.description)} />
      }
      <br/>
      <EntityStrip ctx={ctx.subCtx(e => e.inheritsFrom)}
        iconStart={true}
        vertical={true}
        onChange={() => forceUpdate()} />
      <AutoLine ctx={ctx.subCtx(e => e.mergeStrategy)} helpText={rolesMessage(ctx.value)} onChange={() => forceUpdate()} />

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

