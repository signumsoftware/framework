import * as React from 'react'
import { classes } from '../../../../Framework/Signum.React/Scripts/Globals'
import { FormGroup, FormControlStatic, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater, EntityTable } from '../../../../Framework/Signum.React/Scripts/Lines'
import { SearchControl } from '../../../../Framework/Signum.React/Scripts/Search'
import { TypeContext, FormGroupStyle } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import FileLine from '../../../../Extensions/Signum.React.Extensions/Files/FileLine'
import { PredictorEntity, PredictorColumnEmbedded } from '../Signum.Entities.MachineLearning'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import { getQueryNiceName } from '../../../../Framework/Signum.React/Scripts/Reflection'
import QueryTokenEntityBuilder from '../../UserAssets/Templates/QueryTokenEntityBuilder'
import { QueryFilterEmbedded } from '../../UserQueries/Signum.Entities.UserQueries'
import { QueryDescription, SubTokensOptions } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { API } from '../PredictorClient';

export default class Predictor extends React.Component<{ ctx: TypeContext<PredictorEntity> }> {

    handleOnChange = () => {
        const e = this.props.ctx.value;
        e.filters = [];
        e.columns = [];
        this.forceUpdate();
    }

    handleChange = (column: PredictorColumnEmbedded) => {
        if (column.type == "SimpleColumn")
            column.multiColumn = null;
        else
            column.token = null;

        this.forceUpdate();
    }

    render() {
        const ctx = this.props.ctx;
        const ctxxs = ctx.subCtx({ formGroupSize: "ExtraSmall" });
        const entity = ctx.value;
        const queryKey = entity.query && entity.query.key;

        return (
            <div>
                <ValueLine ctx={ctx.subCtx(e => e.name)} />
                <EntityLine ctx={ctx.subCtx(f => f.query)} remove={ctx.value.isNew} onChange={this.handleOnChange} />
                {queryKey && <div>
                    <EntityTable ctx={ctxxs.subCtx(e => e.filters)} columns={EntityTable.typedColumns<QueryFilterEmbedded>([
                        {
                            property: a => a.token,
                            template: ctx => <QueryTokenEntityBuilder
                                ctx={ctx.subCtx(a => a.token, { formGroupStyle: "SrOnly" })}
                                queryKey={this.props.ctx.value.query!.key}
                                subTokenOptions={SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement} />,
                            headerHtmlAttributes: { style: { width: "40%" } },
                        },
                        { property: a => a.operation },
                        { property: a => a.valueString, headerHtmlAttributes: { style: { width: "40%" } } }
                    ])} />

                    <EntityTable ctx={ctxxs.subCtx(e => e.columns)} columns={EntityTable.typedColumns<PredictorColumnEmbedded>([
                        { property: a => a.usage },
                        { property: a => a.type, template: ctx => <ValueLine ctx={ctx.subCtx(a => a.type)} onChange={() => this.handleChange(ctx.value)} /> },
                        {
                            property: a => a.token,
                            template: ctx => ctx.value.type == "SimpleColumn" ?
                                <QueryTokenEntityBuilder
                                    ctx={ctx.subCtx(a => a.token, { formGroupStyle: "SrOnly" })}
                                    queryKey={this.props.ctx.value.query!.key}
                                    subTokenOptions={SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement} /> :
                                <EntityLine ctx={ctx.subCtx(a => a.multiColumn)} />,
                            headerHtmlAttributes: { style: { width: "40%" } },
                        },

                    ])} />

                </div>}
            </div>
        );
    }
}
