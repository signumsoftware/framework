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
import { UserChartEntity } from '../UserChart/Signum.Chart.UserChart'
import { UserQueryMessage } from '../../Signum.UserQueries/Signum.UserQueries'
import FilterBuilderEmbedded from '../../Signum.UserAssets/Templates/FilterBuilderEmbedded'

const CurrentEntityKey = "[CurrentEntity]";
export default function UserChart(p : { ctx: TypeContext<UserChartEntity> }): React.JSX.Element | null {
  const forceUpdate = useForceUpdate();
  const ctx = p.ctx;
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
            <a className="form-control-static" href={Finder.findOptionsPath({ queryName: queryKey })}>{getQueryNiceName(queryKey)}</a> :
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
        }/>
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

