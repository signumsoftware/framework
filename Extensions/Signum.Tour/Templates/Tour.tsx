import * as React from 'react'
import { TourEntity } from '../Signum.Tour'
import { useForceUpdate } from '@framework/Hooks';
import { AutoLine, CheckboxLine, EntityAccordion, TypeContext } from '@framework/Lines';
import TourStep from './TourStep';
import { Navigator } from '@framework/Navigator';
import { TypeEntity } from '@framework/Signum.Basics';
import { DashboardEntity } from '../../Signum.Dashboard/Signum.Dashboard';
import { UserQueryEntity } from '../../Signum.UserQueries/Signum.UserQueries';

export default function Tour(p: { ctx: TypeContext<TourEntity> }): React.ReactElement {
  const forceUpdate = useForceUpdate();
  const ctx = p.ctx.subCtx({ labelColumns: { sm: 2 } });
  const type = Navigator.useFetchInState(TypeEntity.isLite(p.ctx.value.trigger) ? p.ctx.value.trigger : null);
  const dashboard = Navigator.useFetchInState(DashboardEntity.isLite(p.ctx.value.trigger) ? p.ctx.value.trigger : null);
  const userQuery = Navigator.useFetchInState(UserQueryEntity.isLite(p.ctx.value.trigger) ? p.ctx.value.trigger : null);
  return (
    <div>
      <AutoLine ctx={ctx.subCtx(a => a.trigger)} onChange={forceUpdate} />

      <EntityAccordion ctx={ctx.subCtx(a => a.steps)} avoidFieldSet="h4"
        getComponent={ctx => <TourStep ctx={ctx} invalidate={forceUpdate} type={type} dashboard={dashboard} userQuery={userQuery} />}
        getTitle={ctx => ctx.value.title || ""} />

      <div className="row">
        <div className="col-sm-4">
          <CheckboxLine ctx={ctx.subCtx(a => a.showProgress)} inlineCheckbox={true} />
        </div>
        <div className="col-sm-4">
          <CheckboxLine ctx={ctx.subCtx(a => a.animate)} inlineCheckbox={true} />
        </div>
        <div className="col-sm-4">
          <CheckboxLine ctx={ctx.subCtx(a => a.showCloseButton)} inlineCheckbox={true} />
        </div>
      </div>
    </div>
  );
}
