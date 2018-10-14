import * as React from 'react'
import { UserQueryEntity, UserQueryMessage, QueryFilterEmbedded, QueryOrderEmbedded, QueryColumnEmbedded } from '../../UserQueries/Signum.Entities.UserQueries'
import ChartBuilder from '../Templates/ChartBuilder'
import { UserChartEntity, ChartColumnEmbedded } from '../Signum.Entities.Chart'
import { FormGroup, FormControlReadonly, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater, EntityTable} from '@framework/Lines'
import * as Finder from '@framework/Finder'
import { QueryDescription, SubTokensOptions } from '@framework/FindOptions'
import { getQueryNiceName } from '@framework/Reflection'
import { TypeContext, FormGroupStyle } from '@framework/TypeContext'
import QueryTokenEntityBuilder from '../../UserAssets/Templates/QueryTokenEntityBuilder'
import FilterBuilderEmbedded from '../../UserAssets/Templates/FilterBuilderEmbedded';

import "../Chart.css"

const CurrentEntityKey = "[CurrentEntity]";
export default class UserChart extends React.Component<{ ctx: TypeContext<UserChartEntity> }> {

    render() {
        const ctx = this.props.ctx;
        const entity = ctx.value;
        const queryKey = entity.query!.key;

        return (
            <div>
                <EntityLine ctx={ctx.subCtx(e => e.owner) } />
                <ValueLine ctx={ctx.subCtx(e => e.displayName) } />
                <FormGroup ctx={ctx.subCtx(e => e.query) }>
                    {
                        Finder.isFindable(queryKey, true) ?
                            <a className="form-control-static" href={Finder.findOptionsPath({ queryName: queryKey }) }>{getQueryNiceName(queryKey) }</a> :
                            <span>{getQueryNiceName(queryKey) }</span>
                    }
                </FormGroup>
                <EntityLine ctx={ctx.subCtx(e => e.entityType) } onChange={() => this.forceUpdate() }/>
                {
                    entity.entityType &&
                    <div>
                        <ValueLine ctx={ctx.subCtx(e => e.hideQuickLink) } />
                        <p className="messageEntity col-sm-offset-2">
                            {UserQueryMessage.Use0ToFilterCurrentEntity.niceToString(CurrentEntityKey) }
                        </p>
                    </div>
                }
                <FilterBuilderEmbedded ctx={ctx.subCtx(e => e.filters)} queryKey={this.props.ctx.value.query.key}
                    subTokenOptions={SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement | SubTokensOptions.CanAggregate} />
                <ChartBuilder queryKey={queryKey} onInvalidate={this.handleInvalidate} onTokenChange={this.handleTokenChange} onRedraw={this.handleInvalidate} ctx={this.props.ctx} />
                <EntityTable ctx={ctx.subCtx(e => e.orders)} columns={EntityTable.typedColumns<QueryOrderEmbedded>([
                    {
                        property: a => a.token,
                        template: ctx => <QueryTokenEntityBuilder
                            ctx={ctx.subCtx(a => a.token, { formGroupStyle: "SrOnly" })}
                            queryKey={this.props.ctx.value.query!.key}
                            subTokenOptions={SubTokensOptions.CanElement | SubTokensOptions.CanAggregate} />
                    },
                    { property: a => a.orderType }
                ])} />
            </div>
        );
    }

    handleInvalidate = () => {
        this.fixOrders();
    };


    handleTokenChange = () => {
        this.fixOrders();
    };

    fixOrders() {
        var uc = this.props.ctx.value;

        if (uc.groupResults) {
            var oldOrders = uc.orders.filter(mle =>
                mle.element.token && mle.element.token.token && mle.element.token.token.queryTokenType != "Aggregate" &&
                !uc.columns.some(mle2 => !!mle2.element.token && !!mle2.element.token.token && mle2.element.token.token.fullKey == mle.element.token!.token!.fullKey));

            oldOrders.forEach(o => this.props.ctx.value.orders.remove(o));
        }

        this.forceUpdate();
    }
}

