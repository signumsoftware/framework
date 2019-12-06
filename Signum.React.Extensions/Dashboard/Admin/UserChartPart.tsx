import * as React from 'react'
import { ValueLine, EntityLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { UserChartPartEntity, DashboardEntity } from '../Signum.Entities.Dashboard'

export default function UserChartPart(p : { ctx: TypeContext<UserChartPartEntity> }){
  const ctx = p.ctx;

  return (
    <div >
      <EntityLine ctx={ctx.subCtx(p => p.userChart)} create={false} onChange={() => ctx.findParentCtx(DashboardEntity).frame!.entityComponent!.forceUpdate()} />
      <div className="row">
        <div className="col-sm-6">
          <ValueLine ctx={ctx.subCtx(p => p.showData)} inlineCheckbox={true} />
        </div>
        <div className="col-sm-6">
          <ValueLine ctx={ctx.subCtx(p => p.allowChangeShowData)} inlineCheckbox={true} />
        </div>
      </div>
    </div>
  );
}
