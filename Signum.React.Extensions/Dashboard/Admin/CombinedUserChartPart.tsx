import * as React from 'react'
import { ValueLine, EntityLine, EntityStrip } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { UserChartPartEntity, DashboardEntity, CombinedUserChartPartEntity } from '../Signum.Entities.Dashboard'

export default function CombinedUserChartPart(p: { ctx: TypeContext<CombinedUserChartPartEntity> }) {
  const ctx = p.ctx;

  return (
    <div >
      <EntityStrip ctx={ctx.subCtx(p => p.userCharts)} />
    </div>
  );
}
