import * as React from 'react'
import { ValueLine, EntityLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { UserChartPartEntity } from '../../Signum.Chart.UserChart';
import { DashboardEntity } from '../../../Signum.Dashboard/Signum.Dashboard';
import { IsQueryCachedLine } from '../../../Signum.Dashboard/Admin/Dashboard';

export default function UserChartPart(p: { ctx: TypeContext<UserChartPartEntity> }) {
  const ctx = p.ctx;

  return (
    <div >
      <EntityLine ctx={ctx.subCtx(p => p.userChart)} create={false} onChange={() => ctx.findParentCtx(DashboardEntity).frame!.entityComponent!.forceUpdate()} />

      <div className="row">
        <div className="col-sm-6">
          <ValueLine ctx={ctx.subCtx(p => p.showData)} inlineCheckbox="block" />
          <ValueLine ctx={ctx.subCtx(p => p.allowChangeShowData)} inlineCheckbox="block" />
          <ValueLine ctx={ctx.subCtx(p => p.createNew)} inlineCheckbox="block" />
          <ValueLine ctx={ctx.subCtx(p => p.autoRefresh)} inlineCheckbox="block" />
          {ctx.findParentCtx(DashboardEntity).value.cacheQueryConfiguration && <IsQueryCachedLine ctx={ctx.subCtx(p => p.isQueryCached)} />}
        </div>
        <div className="col-sm-6">
          <ValueLine ctx={ctx.subCtx(p => p.minHeight)} formGroupStyle="Basic" />
        </div>
      </div>


     
    </div>
  );
}


