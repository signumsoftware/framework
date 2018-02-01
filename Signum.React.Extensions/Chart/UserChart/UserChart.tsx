import * as React from 'react'
import { UserQueryEntity, UserQueryMessage, QueryFilterEmbedded, QueryOrderEmbedded, QueryColumnEmbedded } from '../../UserQueries/Signum.Entities.UserQueries'
import ChartBuilder from '../Templates/ChartBuilder'
import { UserChartEntity, ChartColumnEmbedded } from '../Signum.Entities.Chart'
import { FormGroup, FormControlReadonly, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater} from '../../../../Framework/Signum.React/Scripts/Lines'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import { QueryDescription, SubTokensOptions } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { getQueryNiceName } from '../../../../Framework/Signum.React/Scripts/Reflection'
import { TypeContext, FormGroupStyle } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import QueryTokenEntityBuilder from '../../UserAssets/Templates/QueryTokenEntityBuilder'

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
                <div className="form-xs">
                    <div className="repeater-inline form-inline sf-filters-list ">
                        <EntityRepeater ctx={ctx.subCtx(e => e.filters) } getComponent={this.renderFilter}/>
                    </div>
                </div>
                <ChartBuilder queryKey={queryKey} onInvalidate={this.handleInvalidate} onTokenChange={this.handleTokenChange} onRedraw={this.handleInvalidate} ctx={this.props.ctx} />
                <div className="form-xs">
                    <div className="repeater-inline form-inline sf-filters-list ">
                        <EntityRepeater ctx={ctx.subCtx(e => e.orders) } getComponent={this.renderOrder}/>
                    </div>
                </div>
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

    renderFilter = (ctx: TypeContext<QueryFilterEmbedded>) => {
        const ctx2 = ctx.subCtx({ formGroupStyle: "None" });
        return (
            <div>
                <QueryTokenEntityBuilder
                    ctx={ctx2.subCtx(a => a.token, { formGroupStyle: "None" }) }
                    queryKey={this.props.ctx.value.query!.key}
                    subTokenOptions={SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement | SubTokensOptions.CanAggregate} />
                <span style={{ margin: "0px 10px" }}>
                    <ValueLine ctx={ctx2.subCtx(e => e.operation) } />
                </span>
                <ValueLine ctx={ctx2.subCtx(e => e.valueString) } valueHtmlAttributes={{ size: 50 }} />
            </div>
        );
    }

    renderOrder = (ctx: TypeContext<QueryOrderEmbedded>) => {
        const ctx2 = ctx.subCtx({ formGroupStyle: "None" });
        return (
            <div>
                <QueryTokenEntityBuilder
                    ctx={ctx2.subCtx(a => a.token, { formGroupStyle: "None" }) }
                    queryKey={this.props.ctx.value.query!.key}
                    subTokenOptions={SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement | SubTokensOptions.CanAggregate} />
                <span style={{ margin: "0px 10px" }}>
                    <ValueLine ctx={ctx2.subCtx(e => e.orderType) } />
                </span>
            </div>
        );
    }
}

