import * as React from 'react'
import { AutoLine, CheckboxLine, EntityLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { UserChartPartEntity, UserChartEntity } from '../../UserChart/Signum.Chart.UserChart';
import { DashboardEntity } from '../../../Signum.Dashboard/Signum.Dashboard';
import { IsQueryCachedLine } from '../../../Signum.Dashboard/Admin/Dashboard';
import { getEntityTypeHelpText } from '../../../Signum.Dashboard/Admin/EntityTypeRelatedHelpText';

export default function UserChartPart(p: { ctx: TypeContext<UserChartPartEntity> }): React.JSX.Element {
  const ctx = p.ctx;
  const dashboardEntityType = ctx.findParentCtx(DashboardEntity).value.entityType;

  return (
    <div >
      <EntityLine ctx={ctx.subCtx(p => p.userChart)} create={false}
        findOptions={dashboardEntityType ? {
          queryName: UserChartEntity,
          filterOptions: [{
            token: UserChartEntity.token(a => a.entity.entityType),
            value: dashboardEntityType,
            pinned: { active: "Checkbox_Checked" }
          }]
        } : undefined}
        helpText={getEntityTypeHelpText(dashboardEntityType, ctx.value.userChart?.entityType)}
        onChange={() => ctx.findParentCtx(DashboardEntity).frame!.entityComponent!.forceUpdate()} />

      <div className="row">
        <div className="col-sm-6">
          <CheckboxLine ctx={ctx.subCtx(p => p.showData)} inlineCheckbox="block" />
          <CheckboxLine ctx={ctx.subCtx(p => p.allowChangeShowData)} inlineCheckbox="block" />
          <CheckboxLine ctx={ctx.subCtx(p => p.createNew)} inlineCheckbox="block" />
          <CheckboxLine ctx={ctx.subCtx(p => p.autoRefresh)} inlineCheckbox="block" />
          {ctx.findParentCtx(DashboardEntity).value.cacheQueryConfiguration && <IsQueryCachedLine ctx={ctx.subCtx(p => p.isQueryCached)} />}
        </div>
        <div className="col-sm-6">
          <AutoLine ctx={ctx.subCtx(p => p.minHeight)} formGroupStyle="Basic" />
        </div>
      </div>

    </div>
  );
}
