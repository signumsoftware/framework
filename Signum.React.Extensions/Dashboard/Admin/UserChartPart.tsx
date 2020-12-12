import * as React from 'react'
import { ValueLine, EntityLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { UserChartPartEntity, DashboardEntity } from '../Signum.Entities.Dashboard'

export default function UserChartPart(p: { ctx: TypeContext<UserChartPartEntity> }) {
  const ctx = p.ctx;

  return (
    <div >
      <EntityLine ctx={ctx.subCtx(p => p.userChart)} create={false} onChange={() => ctx.findParentCtx(DashboardEntity).frame!.entityComponent!.forceUpdate()} />
      <ValueLine ctx={ctx.subCtx(p => p.showData)} inlineCheckbox="block" />
      <ValueLine ctx={ctx.subCtx(p => p.allowChangeShowData)} inlineCheckbox="block" />
      <ValueLine ctx={ctx.subCtx(p => p.createNew)} inlineCheckbox="block" />
      <ValueLine ctx={ctx.subCtx(p => p.autoRefresh)} inlineCheckbox="block" />
    </div>
  );
}
