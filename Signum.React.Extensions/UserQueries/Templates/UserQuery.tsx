import * as React from 'react'
import { UserQueryEntity, UserQueryMessage, QueryOrderEmbedded, QueryColumnEmbedded } from '../Signum.Entities.UserQueries'
import { FormGroup, ValueLine, EntityLine, EntityTable, EntityStrip } from '@framework/Lines'
import * as Finder from '@framework/Finder'
import { FilterConditionOption, FindOptions, SubTokensOptions } from '@framework/FindOptions'
import { getQueryNiceName } from '@framework/Reflection'
import { TypeContext } from '@framework/TypeContext'
import QueryTokenEmbeddedBuilder from '../../UserAssets/Templates/QueryTokenEmbeddedBuilder'
import FilterBuilderEmbedded from '../../UserAssets/Templates/FilterBuilderEmbedded';
import { useAPI, useForceUpdate } from '@framework/Hooks'
import { QueryTokenEmbedded } from '../../UserAssets/Signum.Entities.UserAssets'
import { SearchMessage, getToString } from '@framework/Signum.Entities'
import { formGroupStyle } from '../../Dynamic/View/StyleOptionsExpression'

const CurrentEntityKey = "[CurrentEntity]";

export default function UserQuery(p: { ctx: TypeContext<UserQueryEntity> }) {

  const forceUpdate = useForceUpdate();

  const query = p.ctx.value.query;
  const ctx = p.ctx;
  const ctx4 = ctx.subCtx({ labelColumns: 4 });
  const ctxxs = ctx.subCtx({ formSize: "xs" });

  const canAggregate = ctx.value.groupResults ? SubTokensOptions.CanAggregate : 0;

  const groupsResultRef = React.useRef<boolean>(ctx.value.groupResults);

  React.useEffect(() => {
    if (ctx.value.groupResults == groupsResultRef.current)
      return;

    groupsResultRef.current = ctx.value.groupResults;
    ctx.value.customDrilldowns = [];
    ctx.value.modified = true;
    forceUpdate();
  }, [ctx.value.groupResults]);

  const qd = useAPI(() => Finder.getQueryDescription(query.key), [query.key]);
  if (!qd)
    return null;

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
          helpText={
            <div>
              {UserQueryMessage.MakesThe0AvailableAsAQuickLinkOf1.niceToString(UserQueryEntity.niceName(), ctx.value.entityType ? getToString(ctx.value.entityType) : UserQueryMessage.TheSelected0.niceToString(ctx.niceName(a => a.entityType)))}
              {p.ctx.value.entityType && <br />}
              {p.ctx.value.entityType && UserQueryMessage.Use0ToFilterCurrentEntity.niceToString().formatHtml(<code style={{ display: "inline" }}><strong>{CurrentEntityKey}</strong></code>)}
              {p.ctx.value.entityType && <br />}
              {p.ctx.value.entityType && <ValueLine ctx={ctx.subCtx(e => e.hideQuickLink)} inlineCheckbox />}
            </div>
          } />



          <div className="row">
          <div className="col-sm-6">
            <ValueLine ctx={ctx4.subCtx(e => e.groupResults)} />
            <ValueLine ctx={ctx4.subCtx(e => e.appendFilters)} readOnly={ctx.value.entityType != null} onChange={() => forceUpdate()}
              helpText={UserQueryMessage.MakesThe0AvailableInContextualMenuWhenGrouping0.niceToString(UserQueryEntity.niceName(), query?.key)} />

            </div>
            <div className="col-sm-6">
              <ValueLine ctx={ctx4.subCtx(e => e.refreshMode)} />
              <ValueLine ctx={ctx4.subCtx(e => e.includeDefaultFilters)} />
            </div>
          </div>

          <div>
            <FilterBuilderEmbedded ctx={ctxxs.subCtx(e => e.filters)}
              subTokenOptions={SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement | canAggregate}
              queryKey={ctxxs.value.query!.key}
            showPinnedFilterOptions={true} />
          <ValueLine ctx={ctxxs.subCtx(e => e.columnsMode)} valueColumns={4} />
            <EntityTable ctx={ctxxs.subCtx(e => e.columns)} columns={EntityTable.typedColumns<QueryColumnEmbedded>([
              {
                property: a => a.token,
                template: (ctx, row) =>
                  <div>
                    <QueryTokenEmbeddedBuilder
                      ctx={ctx.subCtx(a => a.token, { formGroupStyle: "SrOnly" })}
                      queryKey={p.ctx.value.query!.key}
                      onTokenChanged={() => { ctx.value.summaryToken = null; ctx.value.modified = true; row.forceUpdate(); }}
                      subTokenOptions={SubTokensOptions.CanElement | SubTokensOptions.CanToArray | (canAggregate ? canAggregate : SubTokensOptions.CanOperation)} />

                    <div className="d-flex">
                      <label className="col-form-label col-form-label-xs me-2" style={{ minWidth: "140px" }}>
                        <input type="checkbox" className="form-check-input" disabled={ctx.value.token == null} checked={ctx.value.summaryToken != null} onChange={() => {
                          ctx.value.summaryToken = ctx.value.summaryToken == null ? QueryTokenEmbedded.New(ctx.value.token) : null;
                          ctx.value.modified = true;
                          row.forceUpdate();
                        }} /> {SearchMessage.SummaryHeader.niceToString()}
                      </label>
                      <div className="flex-grow-1">
                        {ctx.value.summaryToken &&
                          <QueryTokenEmbeddedBuilder
                            ctx={ctx.subCtx(a => a.summaryToken, { formGroupStyle: "SrOnly" })}
                            queryKey={p.ctx.value.query!.key}
                            subTokenOptions={SubTokensOptions.CanElement | SubTokensOptions.CanAggregate} />
                        }
                      </div>
                    </div>
                  </div>
              },
              {
                property: a => a.displayName,
                template: (ctx, row) => <ValueLine ctx={ctx.subCtx(a => a.displayName)} readOnly={ctx.value.hiddenColumn} valueHtmlAttributes={{ placeholder: ctx.value.token?.token?.niceName }}
                  helpText={<ValueLine ctx={ctx.subCtx(a => a.hiddenColumn)} inlineCheckbox onChange={() => { ctx.value.summaryToken = null; ctx.value.displayName = null; row.forceUpdate(); }} />}
                />
              },
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
          <EntityStrip ctx={ctx.subCtx(e => e.customDrilldowns)}
            findOptions={getCustomDrilldownsFindOptions()}
            avoidDuplicates={true}
            vertical={true}
            iconStart={true} />
        </div>)
      }
    </div>
  );

  function getCustomDrilldownsFindOptions() {
    var fos: FilterConditionOption[] = [];

    if (ctx.value.groupResults)
      fos.push(...[
        { token: UserQueryEntity.token(e => e.query.key), value: query.key },
        { token: UserQueryEntity.token(e => e.entity.appendFilters), value: true }
      ]);
    else
      fos.push({ token: UserQueryEntity.token(e => e.entityType?.entity?.cleanName), value: qd!.columns["Entity"].type.name });

    if (!ctx.value.isNew)
      fos.push({ token: UserQueryEntity.token(e => e.entity), operation: "DistinctTo", value: ctx.value });

    const result = {
      queryName: UserQueryEntity,
      filterOptions: fos.map(fo => { fo.frozen = true; return fo; }),
    } as FindOptions;

    return result;
  }
}


