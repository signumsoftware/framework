import * as React from 'react'
import { ValueLine, EntityLine, EntityStrip } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { UserChartPartEntity, DashboardEntity, CombinedUserChartPartEntity } from '../Signum.Entities.Dashboard'
import { D3ChartScript, UserChartEntity } from '../../Chart/Signum.Entities.Chart';

export default function CombinedUserChartPart(p: { ctx: TypeContext<CombinedUserChartPartEntity> }) {
  const ctx = p.ctx;

  return (
    <div >
      <EntityStrip ctx={ctx.subCtx(p => p.userCharts)} findOptions={{
        queryName: UserChartEntity, filterOptions: [{
          token: UserChartEntity.token().entity(a => a.chartScript.key),
          operation: "IsIn",
          value: [D3ChartScript.Columns.key, D3ChartScript.Line.key]
        }]
      }} />

      <ValueLine ctx={ctx.subCtx(p => p.showData)} inlineCheckbox="block" />
      <ValueLine ctx={ctx.subCtx(p => p.allowChangeShowData)} inlineCheckbox="block" />
      <ValueLine ctx={ctx.subCtx(p => p.combinePinnedFiltersWithSameLabel)} inlineCheckbox="block" />
      <ValueLine ctx={ctx.subCtx(p => p.useSameScale)} inlineCheckbox="block" />
    </div>
  );
}
