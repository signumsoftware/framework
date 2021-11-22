
import * as React from 'react'
import { FormGroup } from '@framework/Lines'
import { FindOptions } from '@framework/FindOptions'
import { getQueryNiceName } from '@framework/Reflection'
import { Entity, Lite, is, JavascriptMessage } from '@framework/Signum.Entities'
import { ValueSearchControlLine } from '@framework/Search'
import { TypeContext, mlistItemContext } from '@framework/TypeContext'
import * as UserQueryClient from '../../UserQueries/UserQueryClient'
import { ValueUserQueryListPartEntity, ValueUserQueryElementEmbedded, PanelPartEmbedded } from '../Signum.Entities.Dashboard'
import { useAPI } from '@framework/Hooks'
import { PanelPartContentProps } from '../DashboardClient'
import { DashboardFilterController } from './DashboardFilterController'

export default function ValueUserQueryListPart(p: PanelPartContentProps<ValueUserQueryListPartEntity>) {
  const entity = p.part;
  const ctx = TypeContext.root(entity, { formGroupStyle: "None" });
  return (
    <div>
      {
        mlistItemContext(ctx.subCtx(a => a.userQueries))
          .map((ctx, i) =>
            <div key={i} >
              <ValueUserQueryElement ctx={ctx} entity={p.entity} filterController={p.filterController} partEmbedded={p.partEmbedded} />
            </div>)
      }
    </div>
  );
}

export interface ValueUserQueryElementProps {
  ctx: TypeContext<ValueUserQueryElementEmbedded>
  entity?: Lite<Entity>;
  filterController: DashboardFilterController;
  partEmbedded: PanelPartEmbedded;
}

export function ValueUserQueryElement(p: ValueUserQueryElementProps) {

  let fo = useAPI(signal => UserQueryClient.Converter.toFindOptions(p.ctx.value.userQuery, p.entity),
    [p.ctx.value.userQuery, p.entity]);

  const ctx = p.ctx;
  const ctx2 = ctx.subCtx({ formGroupStyle: "SrOnly" });

  if (!fo)
    return <span>{JavascriptMessage.loading.niceToString()}</span>;

  fo = p.filterController.applyToFindOptions(p.partEmbedded, fo);

  return (
    <div>
      <FormGroup ctx={ctx} labelText={ctx.value.label ?? getQueryNiceName(fo.queryName)}>
        <div className="row align-items-center">
          <div className="col-auto">
            <span>{ctx.value.label ?? getQueryNiceName(fo.queryName)}</span>
          </div>
          <div className="col-auto">
            <ValueSearchControlLine ctx={ctx2} findOptions={fo} />
          </div>
        </div>
      </FormGroup>
    </div>
  );
}



