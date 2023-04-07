
import * as React from 'react'
import { ValueLine, EntityLine, EntityRepeater, EntityTable } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { ValueUserQueryListPartEntity, ValueUserQueryElementEmbedded, DashboardEntity } from '../Signum.Dashboard'
import { IsQueryCachedLine } from './Dashboard';

export default function ValueUserQueryListPart(p : { ctx: TypeContext<ValueUserQueryListPartEntity> }){
  
  const ctx = p.ctx;

  const db = ctx.findParentCtx(DashboardEntity).value.cacheQueryConfiguration;
  return (
    <div>
      <EntityTable ctx={ctx.subCtx(p => p.userQueries)} columns={EntityTable.typedColumns<ValueUserQueryElementEmbedded>([
        {
          property: p => p.userQuery,
          headerHtmlAttributes: { style: { width: "35%" } },
        },
        {
          property: p => p.label,
        },
        {
          property: p => p.href,
        },
        db && {
          property: p => p.isQueryCached,
          template: rctx => db && <IsQueryCachedLine ctx={rctx.subCtx(p => p.isQueryCached)} />
        }
      ])}
      />
    </div>
  );
}
