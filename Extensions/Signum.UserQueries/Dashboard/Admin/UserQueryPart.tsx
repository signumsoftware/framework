import * as React from 'react'
import { AutoLine, CheckboxLine, EntityLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { useForceUpdate } from '@framework/Hooks';
import { UserQueryPartEntity } from '../../Signum.UserQueries';
import { DashboardEntity } from '../../../Signum.Dashboard/Signum.Dashboard';
import { IsQueryCachedLine } from '../../../Signum.Dashboard/Admin/Dashboard';

export default function UserQueryPart(p: { ctx: TypeContext<UserQueryPartEntity> }): React.JSX.Element {
  const ctx = p.ctx.subCtx({ formGroupStyle: "Basic" });
  const forceUpdate = useForceUpdate();
  return (
    <div >
      <EntityLine ctx={ctx.subCtx(p => p.userQuery)} create={false} onChange={() => ctx.findParentCtx(DashboardEntity).frame!.entityComponent!.forceUpdate()} />
      <AutoLine ctx={ctx.subCtx(p => p.renderMode)} onChange={() => forceUpdate()} />
      {
        ctx.value.renderMode == "SearchControl" &&
        <div className="row">
          <div className="col-sm-5">
            <CheckboxLine ctx={ctx.subCtx(p => p.allowSelection)} inlineCheckbox="block" />
            <CheckboxLine ctx={ctx.subCtx(p => p.showFooter)} inlineCheckbox="block" />
            <CheckboxLine ctx={ctx.subCtx(p => p.createNew)} inlineCheckbox="block" />
            <CheckboxLine ctx={ctx.subCtx(p => p.allowMaxHeight)} inlineCheckbox="block" />
          </div>
          <div className="col-sm-7">
            <AutoLine ctx={ctx.subCtx(p => p.autoUpdate)} />
          </div>
        </div>
      }
      {
        ctx.value.renderMode == "BigValue" &&
        <div>
          <CheckboxLine ctx={ctx.subCtx(p => p.aggregateFromSummaryHeader)} inlineCheckbox="block" />
        </div>
      }
      {ctx.findParentCtx(DashboardEntity).value.cacheQueryConfiguration && <IsQueryCachedLine ctx={ctx.subCtx(p => p.isQueryCached)} />}
    </div>
  );
}
