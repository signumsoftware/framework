import * as React from 'react'
import { classes } from '../../../../Framework/Signum.React/Scripts/Globals'
import { FormGroup, FormControlStatic, ValueLine, ValueLineType, EntityLine, EntityDetail, EntityCombo, EntityList, EntityRepeater, EntityTable, IRenderButtons } from '../../../../Framework/Signum.React/Scripts/Lines'
import { SearchControl } from '../../../../Framework/Signum.React/Scripts/Search'
import { TypeContext, FormGroupStyle, ButtonsContext } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import FileLine from '../../../../Extensions/Signum.React.Extensions/Files/FileLine'
import { PredictorEntity, PredictorColumnEmbedded, PredictorMessage, PredictorMultiColumnEntity } from '../Signum.Entities.MachineLearning'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import { getQueryNiceName } from '../../../../Framework/Signum.React/Scripts/Reflection'
import QueryTokenEntityBuilder from '../../UserAssets/Templates/QueryTokenEntityBuilder'
import { QueryFilterEmbedded } from '../../UserQueries/Signum.Entities.UserQueries'
import { QueryDescription, SubTokensOptions } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { API, initializers } from '../PredictorClient';
import { toLite } from "../../../../Framework/Signum.React/Scripts/Signum.Entities";

export default class Predictor extends React.Component<{ ctx: TypeContext<PredictorEntity> }> implements IRenderButtons {

    renderButtons(ctx: ButtonsContext): (React.ReactElement<any> | undefined)[] {
        return ([
            <div className="btn-group pull-right">
                <button type="button" className="btn btn-default" onClick={this.handleCsv}>{PredictorMessage.Csv.niceToString()}</button>
                <button type="button" className="btn btn-default" onClick={this.handleTsv}>{PredictorMessage.Tsv.niceToString()}</button>
                <button type="button" className="btn btn-default" onClick={this.handleTsvMetadata}>{PredictorMessage.TsvMetadata.niceToString()}</button>
                <button type="button" className="btn btn-default" onClick={() => window.open("http://projector.tensorflow.org/", "_blank")}>{PredictorMessage.TensorflowProjector.niceToString()}</button>
            </div>
        ]);
    }

    handleCsv = () => {
        API.downloadCsvById(toLite(this.props.ctx.value));
    }

    handleTsv = () => {
        API.downloadTsvById(toLite(this.props.ctx.value));
    }

    handleTsvMetadata = () => {
        API.downloadTsvMetadataById(toLite(this.props.ctx.value));
    }

    handleQueryChange = () => {
        const e = this.props.ctx.value;
        e.filters = [];
        e.simpleColumns = [];
        e.multiColumns = [];
        this.forceUpdate();
    }

   

    handleCreate = () => {
        return Promise.resolve(PredictorMultiColumnEntity.New(
            { query: this.props.ctx.value.query }
        ))
    }

    handleAlgorithmChange = () => {
        var pred = this.props.ctx.value;
        var al = pred.algorithm;
        if (al == null)
            pred.algorithmSettings = null;
        else
            initializers[al.key](pred);

        this.forceUpdate();
    }

    render() {
        const ctx = this.props.ctx;
        const ctxxs = ctx.subCtx({ formGroupSize: "ExtraSmall" });
        const entity = ctx.value;
        const queryKey = entity.query && entity.query.key;

        return (
            <div>
                <ValueLine ctx={ctxxs.subCtx(e => e.name)} />
                <EntityLine ctx={ctxxs.subCtx(f => f.query)} remove={ctx.value.isNew} onChange={this.handleQueryChange} />
                {queryKey && <div>
                    <EntityCombo ctx={ctxxs.subCtx(f => f.algorithm)} onChange={this.handleAlgorithmChange} />
                    {ctxxs.value.algorithm && < EntityDetail ctx={ctxxs.subCtx(f => f.algorithmSettings)} remove={false} />}
                    <EntityDetail ctx={ctxxs.subCtx(f => f.settings)} remove={false} />
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

                    <EntityTable ctx={ctxxs.subCtx(e => e.simpleColumns)} columns={EntityTable.typedColumns<PredictorColumnEmbedded>([
                        { property: a => a.usage },
                        {
                            property: a => a.token,
                            template: ctx => <QueryTokenEntityBuilder
                                ctx={ctx.subCtx(a => a.token)}
                                queryKey={this.props.ctx.value.query!.key}
                                subTokenOptions={0} />, 
                            headerHtmlAttributes: { style: { width: "40%" } },
                        },
                        { property: a => a.encoding },
                    ])} />

                    <EntityRepeater ctx={ctxxs.subCtx(e => e.multiColumns)} onCreate={this.handleCreate} />

                </div>}
            </div>
        );
    }
}
