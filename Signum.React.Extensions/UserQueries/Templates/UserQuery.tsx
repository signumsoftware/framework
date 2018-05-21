import * as React from 'react'
import { UserQueryEntity, UserQueryMessage, QueryFilterEmbedded, QueryOrderEmbedded, QueryColumnEmbedded } from '../Signum.Entities.UserQueries'
import { FormGroup, FormControlReadonly, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater, EntityTable } from '../../../../Framework/Signum.React/Scripts/Lines'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import { QueryDescription, SubTokensOptions } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { getQueryNiceName } from '../../../../Framework/Signum.React/Scripts/Reflection'
import { TypeContext, FormGroupStyle } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import QueryTokenEntityBuilder from '../../UserAssets/Templates/QueryTokenEntityBuilder'

const CurrentEntityKey = "[CurrentEntity]";
export default class UserQuery extends React.Component<{ ctx: TypeContext<UserQueryEntity> }> {

    handleGroupResultsChanged = () => {

        var uq = this.props.ctx.value;
        if (!uq.groupResults) {
            uq.filters = [];
            uq.columns = [];
            uq.orders = [];
        }

        this.forceUpdate();
    }

    render() {

        const query = this.props.ctx.value.query;
        const ctx = this.props.ctx;
        const ctxxs = ctx.subCtx({ formSize: "ExtraSmall" });

        const canAggregate = ctx.value.groupResults ? SubTokensOptions.CanAggregate : 0;

        return (
            <div>
                <EntityLine ctx={ctx.subCtx(e => e.owner)} />
                <ValueLine ctx={ctx.subCtx(e => e.displayName)} />
                <FormGroup ctx={ctx.subCtx(e => e.query)}>
                    {
                        query && (
                            Finder.isFindable(query.key, true) ?
                                <a className="form-control-static" href={Finder.findOptionsPath({ queryName: query.key })}>{getQueryNiceName(query.key)}</a> :
                                <span>{getQueryNiceName(query.key)}</span>)
                    }
                </FormGroup>

                {query &&
                    (<div>
                        <EntityLine ctx={ctx.subCtx(e => e.entityType)} onChange={() => this.forceUpdate()} />
                        {
                            this.props.ctx.value.entityType &&
                            <div>
                                <ValueLine ctx={ctx.subCtx(e => e.hideQuickLink)} />
                                <p className="messageEntity col-sm-offset-2">
                                    {UserQueryMessage.Use0ToFilterCurrentEntity.niceToString(CurrentEntityKey)}
                                </p>
                            </div>
                        }
                        <ValueLine ctx={ctx.subCtx(e => e.appendFilters)} />
                        <ValueLine ctx={ctx.subCtx(e => e.groupResults)} />
                        <div>
                            <EntityTable ctx={ctxxs.subCtx(e => e.filters)} columns={EntityTable.typedColumns<QueryFilterEmbedded>([
                                {
                                    property: a => a.token,
                                    template: ctx => <QueryTokenEntityBuilder
                                        ctx={ctx.subCtx(a => a.token, { formGroupStyle: "SrOnly" })}
                                        queryKey={this.props.ctx.value.query!.key}
                                        subTokenOptions={SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement | canAggregate} />,
                                    headerHtmlAttributes: { style: { width: "40%" } },
                                },
                                { property: a => a.operation },
                                { property: a => a.valueString, headerHtmlAttributes: { style: { width: "40%" } } }
                            ])} />
                            <ValueLine ctx={ctxxs.subCtx(e => e.columnsMode)} />
                            <EntityTable ctx={ctxxs.subCtx(e => e.columns)} columns={EntityTable.typedColumns<QueryColumnEmbedded>([
                                {
                                    property: a => a.token,
                                    template: ctx => <QueryTokenEntityBuilder
                                        ctx={ctx.subCtx(a => a.token, { formGroupStyle: "SrOnly" })}
                                        queryKey={this.props.ctx.value.query!.key}
                                        subTokenOptions={SubTokensOptions.CanElement | canAggregate} />
                                },
                                { property: a => a.displayName }
                            ])} />
                            <EntityTable ctx={ctxxs.subCtx(e => e.orders)} columns={EntityTable.typedColumns<QueryOrderEmbedded>([
                                {
                                    property: a => a.token,
                                    template: ctx => <QueryTokenEntityBuilder
                                        ctx={ctx.subCtx(a => a.token, { formGroupStyle: "SrOnly" })}
                                        queryKey={this.props.ctx.value.query!.key}
                                        subTokenOptions={SubTokensOptions.CanElement | canAggregate} />
                                },
                                { property: a => a.orderType }
                            ])} />
                        </div>
                        <div className="row">
                            <div className="col-sm-6">
                                <ValueLine ctx={ctxxs.subCtx(e => e.paginationMode, { labelColumns: { sm: 4 } })} />
                            </div>
                            <div className="col-sm-6">
                                <ValueLine ctx={ctxxs.subCtx(e => e.elementsPerPage, { labelColumns: { sm: 4 } })} />
                            </div>
                        </div>
                    </div>)
                }
            </div>
        );
    }
}

