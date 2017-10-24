import * as React from 'react'
import { classes } from '../../../../Framework/Signum.React/Scripts/Globals'
import { FormGroup, FormControlStatic, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater, EntityTable } from '../../../../Framework/Signum.React/Scripts/Lines'
import { SearchControl } from '../../../../Framework/Signum.React/Scripts/Search'
import { TypeContext, FormGroupStyle } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import FileLine from '../../Files/FileLine'
import { PredictorMultiColumnEntity, PredictorColumnEmbedded } from '../Signum.Entities.MachineLearning'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import { getQueryNiceName } from '../../../../Framework/Signum.React/Scripts/Reflection'
import QueryTokenEntityBuilder from '../../UserAssets/Templates/QueryTokenEntityBuilder'
import { QueryTokenEmbedded } from '../../UserAssets/Signum.Entities.UserAssets'
import { QueryFilterEmbedded } from '../../UserQueries/Signum.Entities.UserQueries'
import { QueryDescription, SubTokensOptions } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { API } from '../PredictorClient';

export default class PredictorMultiColumn extends React.Component<{ ctx: TypeContext<PredictorMultiColumnEntity> }> {

    handleOnChange = () => {
        const e = this.props.ctx.value;
        e.additionalFilters = [];
        e.groupKeys = [];
        e.aggregates = [];
        this.forceUpdate();
    }

    render() {
        const ctx = this.props.ctx;
        const ctxxs = ctx.subCtx({ formGroupSize: "ExtraSmall" });
        const entity = ctx.value;
        const queryKey = entity.query && entity.query.key;

        return (
            <div>
                <EntityLine ctx={ctx.subCtx(f => f.query)} remove={ctx.value.isNew} onChange={this.handleOnChange} />
                {queryKey &&
                    <div>
                        <EntityTable ctx={ctxxs.subCtx(e => e.additionalFilters)} columns={EntityTable.typedColumns<QueryFilterEmbedded>([
                            {
                                property: a => a.token,
                                template: ctx => <QueryTokenEntityBuilder
                                    ctx={ctx.subCtx(a => a.token, { formGroupStyle: "SrOnly" })}
                                    queryKey={this.props.ctx.value.query!.key}
                                    subTokenOptions={SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement | SubTokensOptions.CanAggregate} />,
                                headerHtmlAttributes: { style: { width: "40%" } },
                            },
                            { property: a => a.operation },
                            { property: a => a.valueString, headerHtmlAttributes: { style: { width: "40%" } } }
                        ])} />

                        <EntityTable ctx={ctxxs.subCtx(e => e.groupKeys)} columns={EntityTable.typedColumns<QueryTokenEmbedded>([
                        {
                                header: "Token",
                                template: ctx => <QueryTokenEntityBuilder
                                        ctx={ctx.subCtx({ formGroupStyle: "None" })}
                                        queryKey={this.props.ctx.value.query!.key}
                                        subTokenOptions={SubTokensOptions.CanElement} />,
                                headerHtmlAttributes: { style: { width: "100%" } },
                            },
                        ])} />

                        <EntityTable ctx={ctxxs.subCtx(e => e.aggregates)} columns={EntityTable.typedColumns<QueryTokenEmbedded>([
                            {
                                header: "Token",
                                template: ctx => <QueryTokenEntityBuilder
                                    ctx={ctx.subCtx({ formGroupStyle: "None" })}
                                    queryKey={this.props.ctx.value.query!.key}
                                    subTokenOptions={SubTokensOptions.CanElement | SubTokensOptions.CanAggregate} />,
                                headerHtmlAttributes: { style: { width: "100%" } },
                            },
                        ])} />

                </div>}
            </div>
        );
    }
}
