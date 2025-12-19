import * as React from 'react'
import { BigValuePartEntity, HealthCheckConditionEmbedded, SystemTimeEmbedded, UserQueryEntity, UserQueryMessage, UserQueryPartEntity } from '../Signum.UserQueries'
import { FormGroup, AutoLine, EntityLine, EntityTable, EntityStrip, CheckboxLine, TextBoxLine, EntityDetail, EnumLine, NumberLine } from '@framework/Lines'
import * as AppContext from '@framework/AppContext'
import { Finder } from '@framework/Finder'
import { FilterConditionOption, filterOperations, FindOptions, getFilterOperations, SubTokensOptions } from '@framework/FindOptions'
import { getQueryNiceName, getTypeInfos } from '@framework/Reflection'
import { TypeContext } from '@framework/TypeContext'
import QueryTokenEmbeddedBuilder from '../../Signum.UserAssets/Templates/QueryTokenEmbeddedBuilder'
import FilterBuilderEmbedded from '../../Signum.UserAssets/Templates/FilterBuilderEmbedded';
import { useAPI, useForceUpdate } from '@framework/Hooks'
import { SearchMessage, getToString, toLite } from '@framework/Signum.Entities'
import { QueryTokenEmbedded } from '../../Signum.UserAssets/Signum.UserAssets.Queries'
import { CopyHealthCheckButton } from '@framework/Components/CopyHealthCheckButton'
import { UserQueryClient } from '../UserQueryClient'
import { ToolbarEntity, ToolbarMenuEntity } from '../../Signum.Toolbar/Signum.Toolbar'
import { DashboardEntity } from '../../Signum.Dashboard/Signum.Dashboard'
import { SearchValueLine } from '@framework/Search'
import { UserAssetMessage } from '../../Signum.UserAssets/Signum.UserAssets'
import CollapsableCard from '@framework/Components/CollapsableCard'

const CurrentEntityKey = "[CurrentEntity]";

export default function UserQuery(p: { ctx: TypeContext<UserQueryEntity> }): React.JSX.Element | null {

  const forceUpdate = useForceUpdate();

  const query = p.ctx.value.query;
  const ctx = p.ctx;
  const ctx4 = ctx.subCtx({ labelColumns: 4 });
  const ctxxs = ctx.subCtx({ formSize: "xs" });

  const canAggregate = ctx.value.groupResults ? SubTokensOptions.CanAggregate : 0;
  const canTimeSeries = ctx.value.systemTime?.mode == "TimeSeries" ? SubTokensOptions.CanTimeSeries : 0;

  const qd = useAPI(() => Finder.getQueryDescription(query.key), [query.key]);
  if (!qd)
    return null;

  var qs = Finder.querySettings[query.key];

  var hasSystemTime = qs?.allowSystemTime ?? getTypeInfos(qd.columns["Entity"].type);
  const url = window.location;

  return (
    <div>
      <AutoLine ctx={ctx.subCtx(e => e.displayName)} />
      <EntityLine ctx={ctx.subCtx(e => e.owner)} />
      <FormGroup ctx={ctx.subCtx(e => e.query)}>
        {() => query && (
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
                {p.ctx.value.entityType && <CheckboxLine ctx={ctx.subCtx(e => e.hideQuickLink)} inlineCheckbox />}
              </div>
            } />
        <div className="row"> 
          <div className="offset-sm-2 col-sm-10">

          <CollapsableCard header={UserAssetMessage.Advanced.niceToString()} size="xs">
              <div className="row mt-2 mb-2">
                <div className="col-sm-6">
                  <AutoLine ctx={ctx4.subCtx(e => e.appendFilters)} readOnly={ctx.value.entityType != null} onChange={() => forceUpdate()}
                    helpText={UserQueryMessage.MakesThe0AvailableForCustomDrilldownsAndInContextualMenuWhenGrouping0.niceToString(UserQueryEntity.niceName(), query?.key)} />
                  <AutoLine ctx={ctx4.subCtx(e => e.refreshMode)} />
                  <EntityStrip ctx={ctx4.subCtx(e => e.customDrilldowns)}
                    findOptions={getCustomDrilldownsFindOptions()}
                    avoidDuplicates={true}
                    vertical={true}
                    iconStart={true} />
                </div>

                <div className="col-sm-6">
                  {!ctx.value.isNew &&
                    <div>
                      <h3 className="mt-0 h5">{UserAssetMessage.UsedBy.niceToString()}</h3>
                      <SearchValueLine ctx={ctx4} findOptions={{ queryName: ToolbarMenuEntity, filterOptions: [{ token: ToolbarMenuEntity.token(a => a.entity.elements).any().append(a => a.content), value: ctx.value }] }} />
                      <SearchValueLine ctx={ctx4} findOptions={{ queryName: ToolbarEntity, filterOptions: [{ token: ToolbarEntity.token(a => a.entity.elements).any().append(a => a.content), value: ctx.value }] }} />
                      <SearchValueLine ctx={ctx4} findOptions={{
                        queryName: DashboardEntity,
                        filterOptions: [
                          {
                            token: DashboardEntity.token(a => a.entity.parts).any(), groupOperation: "Or",
                            filters: [
                              { token: DashboardEntity.token(a => a.entity.parts).any().append(a => a.content).cast(BigValuePartEntity).append(a => a.userQuery), value: ctx.value },
                              { token: DashboardEntity.token(a => a.entity.parts).any().append(a => a.content).cast(UserQueryPartEntity).append(a => a.userQuery), value: ctx.value },
                            ]
                          }
                        ]
                      }} />
                    </div>
                  }
                </div>
              </div>
            </CollapsableCard>
          </div>
        </div>
          <div>
            <h2 className="d-inline-block h4">
              <CheckboxLine ctx={ctx4.subCtx(e => e.groupResults)} onChange={handleOnGroupResultsChange} inlineCheckbox="block" formSize="lg" />
            </h2>

            <div className="my-2">
              <h2 className="h4">{ctx.niceName(a => a.filters)}</h2>
              <div className="ms-3">
                <AutoLine ctx={ctxxs.subCtx(e => e.includeDefaultFilters)} valueColumns={4} />
                <FilterBuilderEmbedded ctx={ctxxs.subCtx(e => e.filters)}
                  avoidFieldSet
                  subTokenOptions={SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement | canAggregate | canTimeSeries}
                  queryKey={ctxxs.value.query!.key}
                  showPinnedFilterOptions={true} />
              </div>
            </div>

            <div className="my-2">
              <h2 className="h4">{ctx.niceName(a => a.columns)}</h2>
              <div className="ms-3">
                <AutoLine ctx={ctxxs.subCtx(e => e.columnsMode)} valueColumns={4} />

                <EntityTable ctx={ctxxs.subCtx(e => e.columns)} avoidFieldSet columns={[
                  {
                    property: a => a.token,
                    template: (ctx, row) =>
                      <div>
                        <QueryTokenEmbeddedBuilder
                          ctx={ctx.subCtx(a => a.token, { formGroupStyle: "SrOnly" })}
                          queryKey={p.ctx.value.query!.key}
                          onTokenChanged={() => { ctx.value.summaryToken = null; ctx.value.modified = true; row.forceUpdate(); }}
                          subTokenOptions={SubTokensOptions.CanElement | SubTokensOptions.CanToArray | SubTokensOptions.CanSnippet | (canAggregate ? canAggregate : SubTokensOptions.CanOperation | SubTokensOptions.CanManual) | canTimeSeries} />

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
                    template: (ctx, row) => <TextBoxLine ctx={ctx.subCtx(a => a.displayName)} readOnly={ctx.value.hiddenColumn} valueHtmlAttributes={{ placeholder: ctx.value.token?.token?.niceName }}
                      helpText={
                        <div>
                          <AutoLine ctx={ctx.subCtx(a => a.combineRows)} readOnly={ctx.value.hiddenColumn} />
                          <CheckboxLine ctx={ctx.subCtx(a => a.hiddenColumn)} inlineCheckbox="block" onChange={() => { ctx.value.summaryToken = null; ctx.value.displayName = null; ctx.value.combineRows = null; row.forceUpdate(); }} />
                        </div>
                      }
                    />
                  },
                ]} />

              </div>
            </div>

            <div className="my-4">
              <h2 className="h4">{ctx.niceName(a => a.orders)}</h2>
              <div className="ms-3">
                <EntityTable ctx={ctxxs.subCtx(e => e.orders)} avoidFieldSet columns={[
                  {
                    property: a => a.token,
                    template: ctx => <QueryTokenEmbeddedBuilder
                      ctx={ctx.subCtx(a => a.token, { formGroupStyle: "SrOnly" })}
                      queryKey={p.ctx.value.query!.key}
                      subTokenOptions={SubTokensOptions.CanElement | SubTokensOptions.CanSnippet | canAggregate | canTimeSeries} />
                  },
                  { property: a => a.orderType }
                ]} />
              </div>
            </div>
          </div>

          <div className="my-4">
            <h3 className="h5">{UserQueryMessage.Pagination.niceToString()}</h3>

            <div className=" ms-3 row">
              <div className="col-sm-6">
                <AutoLine ctx={ctxxs.subCtx(e => e.paginationMode, { labelColumns: { sm: 4 } })} formGroupStyle="Basic" />
              </div>
              <div className="col-sm-6">
                <AutoLine ctx={ctxxs.subCtx(e => e.elementsPerPage, { labelColumns: { sm: 4 } })} formGroupStyle="Basic" />
              </div>
            </div>
          </div>

          {(hasSystemTime || ctx.value.systemTime) && <EntityDetail ctx={ctx.subCtx(a => a.systemTime)} avoidFieldSet="h5"
            getComponent={st => <SystemTime ctx={st} />} />}

          <EntityDetail ctx={ctx.subCtx(a => a.healthCheck)} avoidFieldSet="h5"
            showAsCheckBox
            extraButtons={() => !ctx.value.healthCheck || ctx.value.isNew ? undefined :
              <CopyHealthCheckButton name={ctx.value.displayName}
                healthCheckUrl={url.origin + AppContext.toAbsoluteUrl('/api/userQueries/healthCheck/' + ctx.value.id)}
                clickUrl={url.origin + AppContext.toAbsoluteUrl(UserQueryClient.userQueryUrl(toLite(ctx.value)))} />
            }
            onChange={() => forceUpdate()}
            getComponent={hcctx =>
              <div>
                <HealthCondition ctx={hcctx.subCtx(a => a.failWhen)} color="var(--bs-danger-bg-subtle)" queryNiceName={getQueryNiceName(qd.queryKey)} />
                <HealthCondition ctx={hcctx.subCtx(a => a.degradedWhen)} color="var(--bs-warning-bg-subtle)" queryNiceName={getQueryNiceName(qd.queryKey)} />
              </div>} />
        </div>)}


    </div>
  );

  function handleOnGroupResultsChange() {
    ctx.value.customDrilldowns = [];
    ctx.value.modified = true;
    forceUpdate();
  }

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

function SystemTime(p: { ctx: TypeContext<SystemTimeEmbedded> }) {
  const forceUpdate = useForceUpdate();
  const ctx = p.ctx.subCtx({ formSize: "xs", formGroupStyle: "Basic" });
  return (
    <div>
      <div className="row">
        <div className="col-sm-3">
          <AutoLine ctx={ctx.subCtx(e => e.mode)} onChange={() => {
            ctx.value.startDate = ctx.value.mode == "All" ? null : ctx.value.startDate;
            ctx.value.endDate = ctx.value.mode == "All" || ctx.value.mode == "AsOf" ? null : ctx.value.endDate;
            ctx.value.joinMode = ctx.value.mode == "AsOf" ? null : (ctx.value.joinMode ?? "FirstCompatible");
            ctx.value.timeSeriesStep = ctx.value.mode == "TimeSeries" ? 1 : null;
            ctx.value.timeSeriesUnit = ctx.value.mode == "TimeSeries" ? "Day" : null;
            ctx.value.timeSeriesMaxRowsPerStep = ctx.value.mode == "TimeSeries" ? 10 : null;
            ctx.value.splitQueries = ctx.value.mode == "TimeSeries" ? false : false;
            forceUpdate();
          }} />
        </div>
        <div className="col-sm-3">
          {ctx.value.mode == "All" ? null : <AutoLine ctx={ctx.subCtx(e => e.startDate)} label={ctx.value.mode == "AsOf" ? UserQueryMessage.Date.niceToString() : undefined} mandatory />}
        </div>
        <div className="col-sm-3">
          {ctx.value.mode == "All" || ctx.value.mode == "AsOf" ? null : <AutoLine ctx={ctx.subCtx(e => e.endDate)} mandatory />}
        </div>
        <div className="col-sm-3">
          {ctx.value.mode == "AsOf" || ctx.value.mode == "TimeSeries" ? null : <AutoLine ctx={ctx.subCtx(e => e.joinMode)} mandatory />}
        </div>
      </div>
      {
        ctx.value.mode == "TimeSeries" &&

        <div className="row">
          <div className="col-sm-3">
            <AutoLine ctx={ctx.subCtx(e => e.timeSeriesStep)} mandatory />
          </div>
          <div className="col-sm-3">
            <AutoLine ctx={ctx.subCtx(e => e.timeSeriesUnit)} mandatory />
          </div>
          <div className="col-sm-3">
            <AutoLine ctx={ctx.subCtx(e => e.timeSeriesMaxRowsPerStep)} mandatory />
          </div>
        </div>

      }
    </div>
  );
}

function HealthCondition(p: { ctx: TypeContext<HealthCheckConditionEmbedded | null>, color: string, queryNiceName: string }) {
  const ctx = p.ctx.subCtx({ formGroupStyle: "None", formSize: "xs" }) as TypeContext<HealthCheckConditionEmbedded>;
  const forceUpdate = useForceUpdate();
  return (<>
    <label className="d-flex flex-row align-items-center">
      <input type="checkbox" checked={p.ctx.value != null}
        className="form-check-input me-2"
        onChange={() => { p.ctx.value = p.ctx.value == null ? HealthCheckConditionEmbedded.New() : null; forceUpdate() }} />
      {p.ctx.value == null ? p.ctx.niceName() :
        <span style={{ backgroundColor: p.color }} className="p-2">
          {
            UserQueryMessage._0CountOf1Is2Than3.niceToString().formatHtml(
              <strong>{ctx.niceName()}</strong>,
              p.queryNiceName,
              <EnumLine ctx={ctx.subCtx(a => a.operation)} optionItems={filterOperations["Integer"]} formGroupHtmlAttributes={{ className: "d-inline-block" }} />,
              <NumberLine ctx={ctx.subCtx(a => a.value)} formGroupHtmlAttributes={{ className: "d-inline-block" }} />,
            )
          }
        </span>
      }
    </label>
  </>);
}
