import * as React from 'react'
import { UserQueryMessage, QueryOrderEmbedded, UserQueryEntity } from '../../UserQueries/Signum.Entities.UserQueries'
import ChartBuilder from '../Templates/ChartBuilder'
import { UserChartEntity } from '../Signum.Entities.Chart'
import { FormGroup, ValueLine, EntityLine, EntityTable, EntityStrip } from '@framework/Lines'
import * as Finder from '@framework/Finder'
import { FilterConditionOption, FindOptions, SubTokensOptions } from '@framework/FindOptions'
import { getQueryNiceName } from '@framework/Reflection'
import { TypeContext } from '@framework/TypeContext'
import FilterBuilderEmbedded from '../../UserAssets/Templates/FilterBuilderEmbedded';
import "../Chart.css"
import { useAPI, useForceUpdate } from '@framework/Hooks'
import { getCustomDrilldownsFindOptions, hasAggregates } from '../ChartClient'
import { getToString } from '@framework/Signum.Entities'

const CurrentEntityKey = "[CurrentEntity]";
export default function UserChart(p : { ctx: TypeContext<UserChartEntity> }){
  const forceUpdate = useForceUpdate();
  const ctx = p.ctx;
  const entity = ctx.value;
  const queryKey = entity.query!.key;

  const hasAggregatesRef = React.useRef<boolean>(hasAggregates(ctx.value));

  React.useEffect(() => {
    const ha = hasAggregates(ctx.value);
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
      <ValueLine ctx={ctx.subCtx(e => e.displayName)} />
      <FormGroup ctx={ctx.subCtx(e => e.query)}>
        {
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
            {p.ctx.value.entityType && <ValueLine ctx={ctx.subCtx(e => e.hideQuickLink)} inlineCheckbox />}
          </div>
        }/>
      <ValueLine ctx={ctx.subCtx(e => e.includeDefaultFilters)} />
      <FilterBuilderEmbedded ctx={ctx.subCtx(e => e.filters)} queryKey={p.ctx.value.query.key}
        subTokenOptions={SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement | SubTokensOptions.CanAggregate}
        showPinnedFilterOptions={true}
      />
      <ChartBuilder queryKey={queryKey} ctx={p.ctx}
        onInvalidate={() => forceUpdate()}
        onTokenChange={() => forceUpdate()}
        onRedraw={() => forceUpdate()}
        onOrderChanged={() => forceUpdate()} />
      <EntityStrip ctx={ctx.subCtx(e => e.customDrilldowns)}
        findOptions={getCustomDrilldownsFindOptions(queryKey, qd, hasAggregatesRef.current)}
        avoidDuplicates={true}
        vertical={true}
        iconStart={true} />
    </div>
  );
}

