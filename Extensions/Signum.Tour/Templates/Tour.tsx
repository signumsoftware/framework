import * as React from 'react'
import { TourEntity } from '../Signum.Tour'
import { useAPI, useForceUpdate } from '@framework/Hooks';
import { AutoLine, CheckboxLine, EntityAccordion, TypeContext } from '@framework/Lines';
import TourStep from './TourStep';
import { Navigator } from '@framework/Navigator';
import { getToString } from '@framework/Signum.Entities';
import { TypeEntity, TourTriggerSymbol } from '@framework/Signum.Basics';
import { DashboardEntity } from '../../Signum.Dashboard/Signum.Dashboard';
import { UserQueryEntity } from '../../Signum.UserQueries/Signum.UserQueries';
import { TourClient } from '../TourClient';

export default function Tour(p: { ctx: TypeContext<TourEntity> }): React.ReactElement {
  const forceUpdate = useForceUpdate();
  const ctx = p.ctx.subCtx({ labelColumns: { sm: 2 } });
  // For a TourTriggerSymbol trigger associated with an entity type, resolve that type so
  // its properties become available as "Property" CSS steps (like a Lite<TypeEntity> trigger).
  const symbolTypeLite = useAPI(() =>
    TourTriggerSymbol.isLite(p.ctx.value.trigger) ? TourClient.API.getTriggerType(getToString(p.ctx.value.trigger)) : Promise.resolve(null),
    [p.ctx.value.trigger]);
  const type = Navigator.useFetchInState(
    TypeEntity.isLite(p.ctx.value.trigger) ? p.ctx.value.trigger : (symbolTypeLite ?? null));
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
