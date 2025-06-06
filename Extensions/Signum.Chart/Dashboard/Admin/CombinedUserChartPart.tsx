import * as React from 'react'
import { AutoLine, CheckboxLine, EntityLine, EntityStrip, EntityTable } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { D3ChartScript } from '../../Signum.Chart';
import { CombinedUserChartElementEmbedded, CombinedUserChartPartEntity, UserChartEntity } from '../../UserChart/Signum.Chart.UserChart';
import { DashboardEntity } from '../../../Signum.Dashboard/Signum.Dashboard';
import { IsQueryCachedLine } from '../../../Signum.Dashboard/Admin/Dashboard';

export default function CombinedUserChartPart(p: { ctx: TypeContext<CombinedUserChartPartEntity> }): React.JSX.Element {
  const ctx = p.ctx;

  return (
    <div >
      <EntityTable ctx={ctx.subCtx(p => p.userCharts)} columns={[
        {
          property: p => p.userChart,
          template: (ectx) => <EntityLine ctx={ectx.subCtx(p => p.userChart)} findOptions={{
            queryName: UserChartEntity, filterOptions: [{
              token: UserChartEntity.token(a => a.entity.chartScript.key),
              operation: "IsIn",
              value: [D3ChartScript.Columns.key, D3ChartScript.Line.key]
            }]
          }}/>,
          headerHtmlAttributes: { style: { width: "70%" } },
        },
        ctx.findParentCtx(DashboardEntity).value.cacheQueryConfiguration && {
          property: p => p.isQueryCached,
          headerHtmlAttributes: { style: { width: "30%" } },
          template: ectx => <IsQueryCachedLine ctx={ectx.subCtx(p => p.isQueryCached)} />,
        },
      ]}
      />

      <div className="row">
        <div className="col-sm-6">
          <CheckboxLine ctx={ctx.subCtx(p => p.showData)} inlineCheckbox="block" />
          <CheckboxLine ctx={ctx.subCtx(p => p.allowChangeShowData)} inlineCheckbox="block" />
          <CheckboxLine ctx={ctx.subCtx(p => p.combinePinnedFiltersWithSameLabel)} inlineCheckbox="block" />
          <CheckboxLine ctx={ctx.subCtx(p => p.useSameScale)} inlineCheckbox="block" />
        </div>
        <div className="col-sm-6">
            <AutoLine ctx={ctx.subCtx(p => p.minHeight)} formGroupStyle="Basic" />
        </div>
      </div>
    </div>
  );
}
