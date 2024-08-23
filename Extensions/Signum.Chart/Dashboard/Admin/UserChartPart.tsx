import * as React from 'react'
import { AutoLine, CheckboxLine, EntityLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { UserChartPartEntity } from '../../UserChart/Signum.Chart.UserChart';
import { DashboardEntity } from '../../../Signum.Dashboard/Signum.Dashboard';
import { IsQueryCachedLine } from '../../../Signum.Dashboard/Admin/Dashboard';

export default function UserChartPart(p: { ctx: TypeContext<UserChartPartEntity> }): React.JSX.Element {
  const ctx = p.ctx;

  return (
    <div >
      <EntityLine ctx={ctx.subCtx(p => p.userChart)} create={false} onChange={() => ctx.findParentCtx(DashboardEntity).frame!.entityComponent!.forceUpdate()} />

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


