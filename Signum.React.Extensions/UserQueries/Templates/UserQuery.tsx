import * as React from 'react'
import { UserQueryEntity, UserQueryMessage, QueryFilterEntity, QueryOrderEntity, QueryColumnEntity } from '../Signum.Entities.UserQueries'
import { FormGroup, FormControlStatic, EntityComponent, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater, EntityFrame} from '../../../../Framework/Signum.React/Scripts/Lines'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import { QueryDescription, SubTokensOptions } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { getQueryNiceName } from '../../../../Framework/Signum.React/Scripts/Reflection'
import { TypeContext, FormGroupStyle } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import QueryTokenEntityBuilder from '../../UserAssets/Templates/QueryTokenEntityBuilder'

const CurrentEntityKey = "[CurrentEntity]";
export default class UserQuery extends EntityComponent<UserQueryEntity> {

    renderEntity() {

        var queryKey = this.entity.query.key;
        var ctx = this.props.ctx;

        return (
            <div>
                <EntityLine ctx={ctx.subCtx(e => e.owner) } />
                <ValueLine ctx={ctx.subCtx(e => e.displayName) } />
                <FormGroup ctx={ctx.subCtx(e => e.query) }>
                    {
                        Finder.isFindable(queryKey) ?
                            <a className="form-control-static" href={Finder.findOptionsPath({ queryName: queryKey }) }>{getQueryNiceName(queryKey) }</a> :
                            <span>{getQueryNiceName(queryKey) }</span>
                    }
                </FormGroup>
                <EntityLine ctx={ctx.subCtx(e => e.entityType) } onChange={() => this.forceUpdate() }/>
                {
                    this.entity.entityType &&
                    <p className="messageEntity col-sm-offset-2">
                        {UserQueryMessage.Use0ToFilterCurrentEntity.niceToString(CurrentEntityKey) }
                    </p>
                }
                <ValueLine ctx={ctx.subCtx(e => e.withoutFilters) } />
                <div className="form-xs">
                    <div className="repeater-inline form-inline sf-filters-list ">
                        <EntityRepeater ctx={ctx.subCtx(e => e.filters) } getComponent={this.renderFilter}/>
                    </div>
                    <ValueLine ctx={ctx.subCtx(e => e.columnsMode) } />
                    <div className="repeater-inline form-inline sf-filters-list ">
                        <EntityRepeater ctx={ctx.subCtx(e => e.columns) } getComponent={this.renderColumn}/>
                    </div>
                    <div className="repeater-inline form-inline sf-filters-list ">
                        <EntityRepeater ctx={ctx.subCtx(e => e.orders) } getComponent={this.renderOrder}/>
                    </div>
                </div>
                <div className="row">
                    <div className="col-sm-6">
                        <ValueLine ctx={ctx.subCtx(e => e.paginationMode, { labelColumns: { sm: 4 } }) } />
                    </div>
                    <div className="col-sm-6">
                        <ValueLine ctx={ctx.subCtx(e => e.elementsPerPage, { labelColumns: { sm: 4 } }) } />
                    </div>
                </div>
            </div>
        );
    }

    renderFilter = (ctx: TypeContext<QueryFilterEntity>) => {
        var ctx2 = ctx.subCtx({ formGroupStyle: FormGroupStyle.None });
        return (
            <div>
                <QueryTokenEntityBuilder
                    ctx={ctx2.subCtx(a => a.token, { formGroupStyle: FormGroupStyle.None }) }
                    queryKey={this.entity.query.key}
                    subTokenOptions={SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement} />
                <span style={{ margin: "0px 10px" }}>
                    <ValueLine ctx={ctx2.subCtx(e => e.operation) } />
                </span>
                <ValueLine ctx={ctx2.subCtx(e => e.valueString) } />
            </div>
        );
    }

    renderColumn = (ctx: TypeContext<QueryColumnEntity>) => {
        var ctx2 = ctx.subCtx({ formGroupStyle: FormGroupStyle.None });
        return (
            <div>
                <QueryTokenEntityBuilder
                    ctx={ctx2.subCtx(a => a.token, { formGroupStyle: FormGroupStyle.None }) }
                    queryKey={this.entity.query.key}
                    subTokenOptions={SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement} />
                <span style={{ margin: "0px 10px" }}>
                    <ValueLine ctx={ctx2.subCtx(e => e.displayName) } />
                </span>
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
                    subTokenOptions={SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement} />
                <span style={{ margin: "0px 10px" }}>
                    <ValueLine ctx={ctx2.subCtx(e => e.orderType) } />
                </span>
            </div>
        );
    }
}

