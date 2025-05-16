import * as React from 'react'
import { EntityLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { useForceUpdate } from '@framework/Hooks';
import { BigValuePartEntity } from '../../Signum.UserQueries';
import { DashboardEntity } from '../../../Signum.Dashboard/Signum.Dashboard';
import QueryTokenEmbeddedBuilder from '../../../Signum.UserAssets/Templates/QueryTokenEmbeddedBuilder';
import { SubTokensOptions } from '../../../../Signum/React/FindOptions';

export default function BigValuePart(p: { ctx: TypeContext<BigValuePartEntity> }): React.JSX.Element {
  const ctx = p.ctx.subCtx({ formGroupStyle: "Basic" });
  const forceUpdate = useForceUpdate();
  const entityType = ctx.findParent(DashboardEntity)?.entityType;
  return (
    <div >
      <EntityLine ctx={ctx.subCtx(p => p.userQuery)} create={false} onChange={() => {
        ctx.value.valueToken = null;
        ctx.findParentCtx(DashboardEntity).frame!.entityComponent!.forceUpdate();
      }} />
      {
        ctx.value.userQuery ? <QueryTokenEmbeddedBuilder ctx={ctx.subCtx(a => a.valueToken)} queryKey={ctx.value.userQuery.query.key} subTokenOptions={SubTokensOptions.CanElement | SubTokensOptions.CanAggregate} /> :
          entityType ? <QueryTokenEmbeddedBuilder ctx={ctx.subCtx(a => a.valueToken)} queryKey={entityType.model as string} subTokenOptions={0 as SubTokensOptions} /> : 
          null
      }
    </div>
  );
}
