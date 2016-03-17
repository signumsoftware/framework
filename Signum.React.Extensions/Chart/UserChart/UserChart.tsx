import * as React from 'react'
import { UserQueryEntity, UserQueryEntity_Type, UserQueryMessage, QueryFilterEntity, QueryOrderEntity, QueryColumnEntity } from '../../UserQueries/Signum.Entities.UserQueries'
import ChartBuilder from '../Templates/ChartBuilder'
import { UserChartEntity, ChartColumnEntity} from '../Signum.Entities.Chart'
import { FormGroup, FormControlStatic, EntityComponent, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater, EntityFrame} from '../../../../Framework/Signum.React/Scripts/Lines'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import { QueryDescription, SubTokensOptions } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { getQueryNiceName } from '../../../../Framework/Signum.React/Scripts/Reflection'
import { TypeContext, FormGroupStyle } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import QueryTokenEntityBuilder from '../../UserAssets/Templates/QueryTokenEntityBuilder'

require("!style!css!../Chart.css");

const CurrentEntityKey = "[CurrentEntity]";
export default class UserChart extends EntityComponent<UserChartEntity> {

    renderEntity() {

        var queryKey = this.entity.query.key;

        return (
            <div>
                <EntityLine ctx={this.subCtx(e => e.owner) } />
                <ValueLine ctx={this.subCtx(e => e.displayName) } />
                <FormGroup ctx={this.subCtx(e => e.query) }>
                    {
                        Finder.isFindable(queryKey) ?
                            <a className="form-control-static" href={Finder.findOptionsPath(queryKey) }>{getQueryNiceName(queryKey) }</a> :
                            <span>{getQueryNiceName(queryKey) }</span>
                    }
                </FormGroup>
                <EntityLine ctx={this.subCtx(e => e.entityType) } onChange={() => this.forceUpdate() }/>
                {
                    this.entity.entityType &&
                    <p className="messageEntity col-sm-offset-2">
                        {UserQueryMessage.Use0ToFilterCurrentEntity.niceToString(CurrentEntityKey) }
                    </p>
                }
                <div className="form-xs">
                    <div className="repeater-inline form-inline sf-filters-list ">
                        <EntityRepeater ctx={this.subCtx(e => e.filters) } getComponent={this.renderFilter}/>
                    </div>
                </div>
                <ChartBuilder queryKey={this.entity.query.key} onInvalidate={this.handleNull} onRedraw={this.handleNull} ctx={this.props.ctx} />
                <div className="form-xs">
                    <div className="repeater-inline form-inline sf-filters-list ">
                        <EntityRepeater ctx={this.subCtx(e => e.orders) } getComponent={this.renderOrder}/>
                    </div>
                </div>
            </div>
        );
    }

    handleNull = () => {

    };

    renderFilter = (ctx: TypeContext<QueryFilterEntity>) => {
        var ctx2 = ctx.subCtx({ formGroupStyle: FormGroupStyle.None });
        return (
            <div>
                <QueryTokenEntityBuilder
                    ctx={ctx2.subCtx(a => a.token, { formGroupStyle: FormGroupStyle.None }) }
                    queryKey={this.entity.query.key}
                    subTokenOptions={SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement | SubTokensOptions.CanAggregate} />
                <span style={{ margin: "0px 10px" }}>
                    <ValueLine ctx={ctx2.subCtx(e => e.operation) } />
                </span>
                <ValueLine ctx={ctx2.subCtx(e => e.valueString) } />
            </div>
        );
    }

    renderOrder = (ctx: TypeContext<QueryOrderEntity>) => {
        var ctx2 = ctx.subCtx({ formGroupStyle: FormGroupStyle.None });
        return (
            <div>
                <QueryTokenEntityBuilder
                    ctx={ctx2.subCtx(a => a.token, { formGroupStyle: FormGroupStyle.None }) }
                    queryKey={this.entity.query.key}
                    subTokenOptions={SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement | SubTokensOptions.CanAggregate} />
                <span style={{ margin: "0px 10px" }}>
                    <ValueLine ctx={ctx2.subCtx(e => e.orderType) } />
                </span>
            </div>
        );
    }
}

