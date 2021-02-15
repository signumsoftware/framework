import * as React from 'react'
import { UserQueryEntity, UserQueryMessage, QueryOrderEmbedded, QueryColumnEmbedded } from '../Signum.Entities.UserQueries'
import { FormGroup, ValueLine, EntityLine, EntityTable } from '@framework/Lines'
import * as Finder from '@framework/Finder'
import { SubTokensOptions } from '@framework/FindOptions'
import { getQueryNiceName } from '@framework/Reflection'
import { TypeContext } from '@framework/TypeContext'
import QueryTokenEmbeddedBuilder from '../../UserAssets/Templates/QueryTokenEmbeddedBuilder'
import FilterBuilderEmbedded from '../../UserAssets/Templates/FilterBuilderEmbedded';
import { useAPI, useForceUpdate } from '@framework/Hooks'
import { QueryTokenEmbedded } from '../../UserAssets/Signum.Entities.UserAssets'
import { SearchMessage } from '@framework/Signum.Entities'

const CurrentEntityKey = "[CurrentEntity]";

export default function UserQuery(p: { ctx: TypeContext<UserQueryEntity> }) {

  const forceUpdate = useForceUpdate();

  const query = p.ctx.value.query;
  const ctx = p.ctx;
  const ctxxs = ctx.subCtx({ formSize: "ExtraSmall" });

  const canAggregate = ctx.value.groupResults ? SubTokensOptions.CanAggregate : 0;

  return (
    <div>
      <EntityLine ctx={ctx.subCtx(e => e.owner)} />
      <ValueLine ctx={ctx.subCtx(e => e.displayName)} />
      <FormGroup ctx={ctx.subCtx(e => e.query)}>
        {
          query && (
            Finder.isFindable(query.key, true) ?
              <a className="form-control-static" href={Finder.findOptionsPath({ queryName: query.key })}>{getQueryNiceName(query.key)}</a> :
              <span>{getQueryNiceName(query.key)}</span>)
        }
      </FormGroup>

      {query &&
        (<div>
          <EntityLine ctx={ctx.subCtx(e => e.entityType)} readOnly={ctx.value.appendFilters} onChange={() => forceUpdate()}
            helpText={UserQueryMessage.MakesTheUserQueryAvailableAsAQuickLinkOf0.niceToString(ctx.value.entityType?.toStr ?? UserQueryMessage.TheSelected0.niceToString(ctx.niceName(a => a.entityType)))} />
          {
            p.ctx.value.entityType &&
            <div className="row">
              <div className="col-sm-4 offset-sm-2">
                <ValueLine ctx={ctx.subCtx(e => e.hideQuickLink)} inlineCheckbox />
              </div>
              <div className="col-sm-4">
                {UserQueryMessage.Use0ToFilterCurrentEntity.niceToString().formatHtml(<pre style={{ display: "inline" }}><strong>{CurrentEntityKey}</strong></pre>)}
              </div>
            </div>
        }
        <ValueLine ctx={ctx.subCtx(e => e.appendFilters)} readOnly={ctx.value.entityType != null} onChange={() => forceUpdate()}
            helpText={UserQueryMessage.MakesTheUserQueryAvailableInContextualMenuWhenGrouping0.niceToString(query?.key)} />
          <ValueLine ctx={ctx.subCtx(e => e.includeDefaultFilters)} valueColumns={2} />
          <ValueLine ctx={ctx.subCtx(e => e.groupResults)} />
          <div>
            <FilterBuilderEmbedded ctx={ctxxs.subCtx(e => e.filters)}
              subTokenOptions={SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement | canAggregate}
              queryKey={ctxxs.value.query!.key}
              showPinnedFilterOptions={true} />
            <ValueLine ctx={ctxxs.subCtx(e => e.columnsMode)} />
            <EntityTable ctx={ctxxs.subCtx(e => e.columns)} columns={EntityTable.typedColumns<QueryColumnEmbedded>([
              {
                property: a => a.token,
                template: ctx => <QueryTokenEmbeddedBuilder
                  ctx={ctx.subCtx(a => a.token, { formGroupStyle: "SrOnly" })}
                  queryKey={p.ctx.value.query!.key}
                  subTokenOptions={SubTokensOptions.CanElement | canAggregate} />
              },
              { property: a => a.displayName }
            ])} />
            <EntityTable ctx={ctxxs.subCtx(e => e.orders)} columns={EntityTable.typedColumns<QueryOrderEmbedded>([
              {
                property: a => a.token,
                template: ctx => <QueryTokenEmbeddedBuilder
                  ctx={ctx.subCtx(a => a.token, { formGroupStyle: "SrOnly" })}
                  queryKey={p.ctx.value.query!.key}
                  subTokenOptions={SubTokensOptions.CanElement | canAggregate} />
              },
              { property: a => a.orderType }
            ])} />
          </div>
          <div className="row">
            <div className="col-sm-6">
              <ValueLine ctx={ctxxs.subCtx(e => e.paginationMode, { labelColumns: { sm: 4 } })} />
            </div>
            <div className="col-sm-6">
              <ValueLine ctx={ctxxs.subCtx(e => e.elementsPerPage, { labelColumns: { sm: 4 } })} />
            </div>
          </div>
        </div>)
      }
    </div>
  );
}


