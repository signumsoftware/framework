
import * as React from 'react'
import { FormGroup } from '@framework/Lines'
import { FindOptions } from '@framework/FindOptions'
import { getQueryNiceName } from '@framework/Reflection'
import { Entity, Lite, is, JavascriptMessage, liteKey, toLite } from '@framework/Signum.Entities'
import { ValueSearchControlLine } from '@framework/Search'
import { TypeContext, mlistItemContext } from '@framework/TypeContext'
import * as UserQueryClient from '../../UserQueries/UserQueryClient'
import { ValueUserQueryListPartEntity, ValueUserQueryElementEmbedded, PanelPartEmbedded, CachedQueryEntity } from '../Signum.Entities.Dashboard'
import { useAPI } from '@framework/Hooks'
import { PanelPartContentProps } from '../DashboardClient'
import { DashboardFilterController } from './DashboardFilterController'
import { CachedQueryJS, executeQueryValueCached } from '../CachedQueryExecutor'

export default function ValueUserQueryListPart(p: PanelPartContentProps<ValueUserQueryListPartEntity>) {
  const entity = p.part;
  const ctx = TypeContext.root(entity, { formGroupStyle: "None" });
  return (
    <div>
      {
        mlistItemContext(ctx.subCtx(a => a.userQueries))
          .map((ctx, i) =>
            <div key={i} >
              <ValueUserQueryElement ctx={ctx} entity={p.entity} filterController={p.filterController}
                partEmbedded={p.partEmbedded}
                cachedQuery={p.cachedQueries[liteKey(toLite(ctx.value.userQuery))]} />
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
  cachedQuery?: Promise<CachedQueryJS>;
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
        <span className="form-inline">
          <span>{ctx.value.label ?? getQueryNiceName(fo.queryName)}</span>&nbsp;
          <ValueSearchControlLine ctx={ctx2} findOptions={fo}
            customRequest={p.cachedQuery && ((qr, fo, token) => p.cachedQuery!.then(cq => executeQueryValueCached(qr, fo, token, cq)))} />
        </span>
      </FormGroup>
    </div>
  );
}



