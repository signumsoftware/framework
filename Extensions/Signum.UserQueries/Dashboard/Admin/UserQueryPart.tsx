import * as React from 'react'
import { AutoLine, CheckboxLine, EntityLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { useForceUpdate } from '@framework/Hooks';
import { UserQueryPartEntity, UserQueryEntity } from '../../Signum.UserQueries';
import { DashboardEntity } from '../../../Signum.Dashboard/Signum.Dashboard';
import { IsQueryCachedLine } from '../../../Signum.Dashboard/Admin/Dashboard';
import { getEntityTypeHelpText } from '../../../Signum.Dashboard/Admin/EntityTypeRelatedHelpText';

export default function UserQueryPart(p: { ctx: TypeContext<UserQueryPartEntity> }): React.JSX.Element {
  const ctx = p.ctx.subCtx({ formGroupStyle: "Basic" });
  const forceUpdate = useForceUpdate();
  const dashboardEntityType = ctx.findParentCtx(DashboardEntity).value.entityType;

  return (
    <div >
      <EntityLine ctx={ctx.subCtx(p => p.userQuery)} create={false}
        findOptions={dashboardEntityType ? {
          queryName: UserQueryEntity,
          filterOptions: [{
            token: UserQueryEntity.token(a => a.entity.entityType),
            value: dashboardEntityType,
            pinned: { active: "Checkbox_Checked" }
          }]
        } : undefined}
        helpText={getEntityTypeHelpText(dashboardEntityType, ctx.value.userQuery?.entityType)}
        onChange={() => ctx.findParentCtx(DashboardEntity).frame!.entityComponent!.forceUpdate()} />
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
      {ctx.findParentCtx(DashboardEntity).value.cacheQueryConfiguration && <IsQueryCachedLine ctx={ctx.subCtx(p => p.isQueryCached)} />}
    </div>
  );
}
