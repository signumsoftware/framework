import * as React from 'react'
import ChartBuilder from '../Templates/ChartBuilder'
import { FormGroup, AutoLine, EntityLine, EntityStrip, CheckboxLine } from '@framework/Lines'
import { Finder } from '@framework/Finder'
import { SubTokensOptions } from '@framework/FindOptions'
import { getQueryNiceName, getTypeInfos } from '@framework/Reflection'
import { TypeContext } from '@framework/TypeContext'
import "../Chart.css"
import { useAPI, useForceUpdate } from '@framework/Hooks'
import { ChartClient } from '../ChartClient'
import { getToString } from '@framework/Signum.Entities'
import { CombinedUserChartPartEntity, UserChartEntity, UserChartPartEntity } from '../UserChart/Signum.Chart.UserChart'
import { BigValuePartEntity, UserQueryMessage, UserQueryPartEntity } from '../../Signum.UserQueries/Signum.UserQueries'
import FilterBuilderEmbedded from '../../Signum.UserAssets/Templates/FilterBuilderEmbedded'
import { toAbsoluteUrl } from '@framework/AppContext'
import CollapsableCard from '@framework/Components/CollapsableCard'
import { SearchValueLine } from '@framework/Search'
import { ToolbarEntity, ToolbarMenuEntity } from '../../Signum.Toolbar/Signum.Toolbar'
import { DashboardEntity } from '../../Signum.Dashboard/Signum.Dashboard'
import { UserAssetMessage } from '../../Signum.UserAssets/Signum.UserAssets'
import CombinedUserChartPart from '../Dashboard/Admin/CombinedUserChartPart'

const CurrentEntityKey = "[CurrentEntity]";
export default function UserChart(p : { ctx: TypeContext<UserChartEntity> }): React.JSX.Element | null {
  const forceUpdate = useForceUpdate();
  const ctx = p.ctx;
  const ctx4 = ctx.subCtx({ labelColumns: 4 });
  const entity = ctx.value;
  const queryKey = entity.query!.key;

  const hasAggregatesRef = React.useRef<boolean>(ChartClient.hasAggregates(ctx.value));

  React.useEffect(() => {
    const ha = ChartClient.hasAggregates(ctx.value);
    if (ha == hasAggregatesRef.current)
      return;

    hasAggregatesRef.current = ha;
    ctx.value.customDrilldowns = [];
    ctx.value.modified = true;
    forceUpdate();
  });

  const qd = useAPI(() => Finder.getQueryDescription(queryKey), [queryKey]);
  if (!qd)
    return null;

  return (
    <div>
      <EntityLine ctx={ctx.subCtx(e => e.owner)} />
      <AutoLine ctx={ctx.subCtx(e => e.displayName)} />
      <FormGroup ctx={ctx.subCtx(e => e.query)}>
        {() =>
          Finder.isFindable(queryKey, true) ?
            <a className="form-control-static" target="_blank" href={toAbsoluteUrl(Finder.findOptionsPath({ queryName: queryKey }))}>{getQueryNiceName(queryKey)}</a> :
            <span>{getQueryNiceName(queryKey)}</span>
        }
      </FormGroup>
      <EntityLine ctx={ctx.subCtx(e => e.entityType)} onChange={() => forceUpdate()}
        helpText={
          <div>
            {UserQueryMessage.MakesThe0AvailableAsAQuickLinkOf1.niceToString(UserChartEntity.niceName(), ctx.value.entityType ? getToString(ctx.value.entityType) : UserQueryMessage.TheSelected0.niceToString(ctx.niceName(a => a.entityType)))}
            {p.ctx.value.entityType && <br />}
            {p.ctx.value.entityType && UserQueryMessage.Use0ToFilterCurrentEntity.niceToString().formatHtml(<code style={{ display: "inline" }}><strong>{CurrentEntityKey}</strong></code>)}
            {p.ctx.value.entityType && <br/>}
            {p.ctx.value.entityType && <CheckboxLine ctx={ctx.subCtx(e => e.hideQuickLink)} inlineCheckbox />}
          </div>
        } />

      <div className="offset-sm-2 mb-3">

        <CollapsableCard header={UserAssetMessage.Advanced.niceToString()} size="xs">
          <div className="row mt-2 mb-2">
            <div className="col-sm-6">
              {!ctx.value.isNew &&
                <div>
                  <h5 className="mt-0">{UserAssetMessage.UsedBy.niceToString()}</h5>
                  <SearchValueLine ctx={ctx4} findOptions={{ queryName: ToolbarMenuEntity, filterOptions: [{ token: ToolbarMenuEntity.token(a => a.entity.elements).any().append(a => a.content), value: ctx.value }] }} />
                  <SearchValueLine ctx={ctx4} findOptions={{ queryName: ToolbarEntity, filterOptions: [{ token: ToolbarEntity.token(a => a.entity.elements).any().append(a => a.content), value: ctx.value }] }} />
                  <SearchValueLine ctx={ctx4} findOptions={{
                    queryName: DashboardEntity,
                    filterOptions: [
                      {
                        token: DashboardEntity.token(a => a.entity.parts).any(), groupOperation: "Or",
                        filters: [
                          { token: DashboardEntity.token(a => a.entity.parts).any().append(a => a.content).cast(UserChartPartEntity).append(a => a.userChart), value: ctx.value },
                          { token: DashboardEntity.token(a => a.entity.parts).any().append(a => a.content).cast(CombinedUserChartPartEntity).append(a => a.userCharts).any().append(a => a.userChart), value: ctx.value },
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

      <AutoLine ctx={ctx.subCtx(e => e.includeDefaultFilters)} />
      <FilterBuilderEmbedded ctx={ctx.subCtx(e => e.filters)} queryKey={p.ctx.value.query.key}
        subTokenOptions={SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement | SubTokensOptions.CanAggregate | (ctx.value.chartTimeSeries != null ? SubTokensOptions.CanTimeSeries : 0)}
        showPinnedFilterOptions={true}
      />
      <ChartBuilder queryKey={queryKey} ctx={p.ctx}
        queryDescription={qd}
        onInvalidate={() => forceUpdate()}
        onTokenChange={() => forceUpdate()}
        onRedraw={() => forceUpdate()}
        onOrderChanged={() => forceUpdate()} />
      <EntityStrip ctx={ctx.subCtx(e => e.customDrilldowns)}
        findOptions={ChartClient.getCustomDrilldownsFindOptions(queryKey, qd, hasAggregatesRef.current)}
        avoidDuplicates={true}
        vertical={true}
        iconStart={true} />
    </div>
  );
}

