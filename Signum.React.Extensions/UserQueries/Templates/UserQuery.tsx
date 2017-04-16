import * as React from 'react'
import { UserQueryEntity, UserQueryMessage, QueryFilterEmbedded, QueryOrderEmbedded, QueryColumnEmbedded } from '../Signum.Entities.UserQueries'
import { FormGroup, FormControlStatic, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater} from '../../../../Framework/Signum.React/Scripts/Lines'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import { QueryDescription, SubTokensOptions } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { getQueryNiceName } from '../../../../Framework/Signum.React/Scripts/Reflection'
import { TypeContext, FormGroupStyle } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import QueryTokenEntityBuilder from '../../UserAssets/Templates/QueryTokenEntityBuilder'

const CurrentEntityKey = "[CurrentEntity]";
export default class UserQuery extends React.Component<{ ctx: TypeContext<UserQueryEntity> }, void> {

    render() {

        const query = this.props.ctx.value.query;
        const ctx = this.props.ctx;

        return (
            <div>
                <EntityLine ctx={ctx.subCtx(e => e.owner)} />
                <ValueLine ctx={ctx.subCtx(e => e.displayName)} />
                <FormGroup ctx={ctx.subCtx(e => e.query)}>
                    {
                        query && (
                            Finder.isFindable(query.key) ?
                                <a className="form-control-static" href={Finder.findOptionsPath({ queryName: query.key })}>{getQueryNiceName(query.key)}</a> :
                                <span>{getQueryNiceName(query.key)}</span>)
                    }
                </FormGroup>

                {query &&
                    <div>
                        <EntityLine ctx={ctx.subCtx(e => e.entityType)} onChange={() => this.forceUpdate()}/>
                        {
                            this.props.ctx.value.entityType &&
                            <p className="messageEntity col-sm-offset-2">
                                {UserQueryMessage.Use0ToFilterCurrentEntity.niceToString(CurrentEntityKey)}
                            </p>
                        }
                        <ValueLine ctx={ctx.subCtx(e => e.withoutFilters)} />
                        <div className="form-xs">
                            <div className="repeater-inline form-inline sf-filters-list ">
                                <EntityRepeater ctx={ctx.subCtx(e => e.filters)} getComponent={this.renderFilter}/>
                            </div>
                            <ValueLine ctx={ctx.subCtx(e => e.columnsMode)} />
                            <div className="repeater-inline form-inline sf-filters-list ">
                                <EntityRepeater ctx={ctx.subCtx(e => e.columns)} getComponent={this.renderColumn}/>
                            </div>
                            <div className="repeater-inline form-inline sf-filters-list ">
                                <EntityRepeater ctx={ctx.subCtx(e => e.orders)} getComponent={this.renderOrder}/>
                            </div>
                        </div>
                        <div className="row">
                            <div className="col-sm-6">
                                <ValueLine ctx={ctx.subCtx(e => e.paginationMode, { labelColumns: { sm: 4 } })} />
                            </div>
                            <div className="col-sm-6">
                                <ValueLine ctx={ctx.subCtx(e => e.elementsPerPage, { labelColumns: { sm: 4 } })} />
                            </div>
                        </div>
                    </div>
                }
            </div>
        );
    }

    renderFilter = (ctx: TypeContext<QueryFilterEmbedded>) => {
        const ctx2 = ctx.subCtx({ formGroupStyle: "None" });
        return (
            <div>
                <QueryTokenEntityBuilder
                    ctx={ctx2.subCtx(a => a.token, { formGroupStyle: "None" }) }
                    queryKey={this.props.ctx.value.query!.key}
                    subTokenOptions={SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement} />
                <span style={{ margin: "0px 10px" }}>
                    <ValueLine ctx={ctx2.subCtx(e => e.operation) } />
                </span>
                <ValueLine ctx={ctx2.subCtx(e => e.valueString) } valueHtmlAttributes={{ size: 50 }} />
            </div>
        );
    }

    renderColumn = (ctx: TypeContext<QueryColumnEmbedded>) => {
        const ctx2 = ctx.subCtx({ formGroupStyle: "None" });
        return (
            <div>
                <QueryTokenEntityBuilder
                    ctx={ctx2.subCtx(a => a.token, { formGroupStyle: "None" }) }
                    queryKey={this.props.ctx.value.query!.key}
                    subTokenOptions={SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement} />
                <span style={{ margin: "0px 10px" }}>
                    <ValueLine ctx={ctx2.subCtx(e => e.displayName) } />
                </span>
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
                    subTokenOptions={SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement} />
                <span style={{ margin: "0px 10px" }}>
                    <ValueLine ctx={ctx2.subCtx(e => e.orderType) } />
                </span>
            </div>
        );
    }
}

