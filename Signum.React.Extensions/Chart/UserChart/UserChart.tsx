import * as React from 'react'
import { UserQueryMessage, QueryOrderEmbedded } from '../../UserQueries/Signum.Entities.UserQueries'
import ChartBuilder from '../Templates/ChartBuilder'
import { UserChartEntity } from '../Signum.Entities.Chart'
import { FormGroup, ValueLine, EntityLine, EntityTable } from '@framework/Lines'
import * as Finder from '@framework/Finder'
import { SubTokensOptions } from '@framework/FindOptions'
import { getQueryNiceName } from '@framework/Reflection'
import { TypeContext } from '@framework/TypeContext'
import QueryTokenEmbeddedBuilder from '../../UserAssets/Templates/QueryTokenEmbeddedBuilder'
import FilterBuilderEmbedded from '../../UserAssets/Templates/FilterBuilderEmbedded';
import "../Chart.css"
import { useForceUpdate } from '@framework/Hooks'

const CurrentEntityKey = "[CurrentEntity]";
export default function UserChart(p : { ctx: TypeContext<UserChartEntity> }){
  const forceUpdate = useForceUpdate();
  const ctx = p.ctx;
  const entity = ctx.value;
  const queryKey = entity.query!.key;

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
      <EntityLine ctx={ctx.subCtx(e => e.entityType)} onChange={() => forceUpdate()} />
      {
        entity.entityType &&
        <div>
          <ValueLine ctx={ctx.subCtx(e => e.hideQuickLink)} />
          <p className="messageEntity col-sm-offset-2">
            {UserQueryMessage.Use0ToFilterCurrentEntity.niceToString(CurrentEntityKey)}
          </p>
        </div>
      }
      <FilterBuilderEmbedded ctx={ctx.subCtx(e => e.filters)} queryKey={p.ctx.value.query.key}
        subTokenOptions={SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement | SubTokensOptions.CanAggregate}
        showUserFilters={true}
      />
      <ChartBuilder queryKey={queryKey} ctx={p.ctx}
          onInvalidate={() => forceUpdate()} 
          onTokenChange={() =>  forceUpdate()} 
          onRedraw={() => forceUpdate()} 
          onOrderChanged={() => forceUpdate()} />
    </div>
  );
}

