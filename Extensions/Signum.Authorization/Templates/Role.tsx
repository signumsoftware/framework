import * as React from 'react'
import { RoleEntity, UserEntity, MergeStrategy } from '../Signum.Authorization'
import { AutoLine, EntityStrip, TypeContext } from '@framework/Lines'
import { useForceUpdate, useAPI } from '@framework/Hooks'
import { SearchValue, SearchValueLine } from '@framework/Search';
import { getToString, toLite } from '@framework/Signum.Entities';
import { Finder } from '@framework/Finder';
import { AuthMessage } from '../Signum.Authorization';
import { AuthAdminMessage } from '../Rules/Signum.Authorization.Rules';

export default function Role(p: { ctx: TypeContext<RoleEntity> }): React.JSX.Element {
  const forceUpdate = useForceUpdate();

  function rolesMessage(r: RoleEntity) {
    return AuthMessage.DefaultAuthorization.niceToString() +
      (r.inheritsFrom.length == 0 ? (r.mergeStrategy == "Union" ? AuthMessage.Nothing : AuthMessage.Everything).niceToString() :
        r.inheritsFrom.length == 1 ? AuthMessage.SameAs0.niceToString(getToString(r.inheritsFrom.single().element)) :
          (r.mergeStrategy == "Union" ? AuthMessage.MaximumOfThe0 : AuthMessage.MinumumOfThe0).niceToString(RoleEntity.niceCount(r.inheritsFrom.length)));
  }

  const ctx = p.ctx.subCtx({ readOnly: p.ctx.value.isTrivialMerge ? true : undefined });

  const trivialMergeRoles = Finder.useFetchLites(ctx.value.isNew ? null : RoleEntity.fetchOptions(token => ({
      filterOptions: [
        token(a => a.entity.isTrivialMerge).filter("EqualTo", true),
        token(a => a.entity).append(u => u.inheritsFrom).any().filter("EqualTo", ctx.value)
      ]
    })), [ctx.value.id]);

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


      {!ctx.value.isNew && <SearchValueLine ctx={ctx} findOptions={UserEntity.findOptions(token => ({
        filterOptions: [token(u => u.entity.role).filter("EqualTo", ctx.value)]
      }))} />
      }


      {!ctx.value.isNew && trivialMergeRoles && trivialMergeRoles.length > 0 && <SearchValueLine ctx={ctx}
        label={AuthAdminMessage.UsersIncludingInheritedAndMergedRoles.niceToString()}
        findOptions={{
          queryName: UserEntity,
          filterOptions: [{ token: UserEntity.token(u => u.entity.role), operation: "IsIn", value: [toLite(ctx.value), ...trivialMergeRoles] }]
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

