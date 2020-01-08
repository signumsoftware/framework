import * as React from 'react'
import { ValueLine, EntityLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { UserQueryPartEntity, DashboardEntity } from '../Signum.Entities.Dashboard'
import { useForceUpdate } from '@framework/Hooks';

export default function UserQueryPart(p: { ctx: TypeContext<UserQueryPartEntity> }) {
  const ctx = p.ctx.subCtx({ formGroupStyle: p.ctx.value.renderMode == "BigValue" ? "Basic" : undefined });
  const forceUpdate = useForceUpdate();
  return (
    <div >
      <EntityLine ctx={ctx.subCtx(p => p.userQuery)} create={false} onChange={() => ctx.findParentCtx(DashboardEntity).frame!.entityComponent!.forceUpdate()} />
      <ValueLine ctx={ctx.subCtx(p => p.renderMode)} onChange={() => forceUpdate()} />
      {
        ctx.value.renderMode == "SearchControl" && <div>
          <ValueLine ctx={ctx.subCtx(p => p.allowSelection)} />
          <ValueLine ctx={ctx.subCtx(p => p.showFooter)} />
        </div>
      }
    </div>
  );
}
