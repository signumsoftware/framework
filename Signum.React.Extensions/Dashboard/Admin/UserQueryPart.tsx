import * as React from 'react'
import { ValueLine, EntityLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { UserQueryPartEntity, DashboardEntity } from '../Signum.Entities.Dashboard'

export default function UserQueryPart(p : { ctx: TypeContext<UserQueryPartEntity> }){
  const ctx = p.ctx;

  return (
    <div >
      <EntityLine ctx={ctx.subCtx(p => p.userQuery)} create={false} onChange={() => ctx.findParentCtx(DashboardEntity).frame!.entityComponent!.forceUpdate()} />
      <ValueLine ctx={ctx.subCtx(p => p.renderMode)} inlineCheckbox={true} />
    </div>
  );
}
