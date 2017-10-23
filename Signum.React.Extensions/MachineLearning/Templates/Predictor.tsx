import * as React from 'react'
import { Tabs, Tab } from 'react-bootstrap';
import { classes } from '../../../../Framework/Signum.React/Scripts/Globals'
import { FormGroup, FormControlStatic, ValueLine, ValueLineType, EntityLine, EntityDetail, EntityCombo, EntityList, EntityRepeater, EntityTable, IRenderButtons, EntityTabRepeater } from '../../../../Framework/Signum.React/Scripts/Lines'
import { SearchControl } from '../../../../Framework/Signum.React/Scripts/Search'
import { TypeContext, FormGroupStyle, ButtonsContext } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import FileLine from '../../../../Extensions/Signum.React.Extensions/Files/FileLine'
import { PredictorEntity, PredictorColumnEmbedded, PredictorMessage, PredictorMultiColumnEntity, PredictorGroupKeyEmbedded, PredictorFileType } from '../Signum.Entities.MachineLearning'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import { getQueryNiceName } from '../../../../Framework/Signum.React/Scripts/Reflection'
import QueryTokenEntityBuilder from '../../UserAssets/Templates/QueryTokenEntityBuilder'
import { QueryFilterEmbedded } from '../../UserQueries/Signum.Entities.UserQueries'
import { QueryDescription, SubTokensOptions } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { API, initializers } from '../PredictorClient';
import { toLite } from "../../../../Framework/Signum.React/Scripts/Signum.Entities";
import FilterBuilder from '../../../../Framework/Signum.React/Scripts/SearchControl/FilterBuilder';
import { MList, newMListElement } from '../../../../Framework/Signum.React/Scripts/Signum.Entities';
import FilterBuilderEmbedded from './FilterBuilderEmbedded';
import PredictorMultiColumn from './PredictorMultiColumn';
import { QueryTokenEmbedded } from '../../UserAssets/Signum.Entities.UserAssets';
import { QueryEntity } from '../../../../Framework/Signum.React/Scripts/Signum.Entities.Basics';
import { FilePathEmbedded } from '../../Files/Signum.Entities.Files';

export default class Predictor extends React.Component<{ ctx: TypeContext<PredictorEntity> }, { queryDescription?: QueryDescription }> {

    constructor(props: any) {
        super(props);
        this.state = { queryDescription: undefined };
    }

    componentWillMount() {

        let v = this.props.ctx.value;
        if (v.query)
            this.loadData(v.query);
    }

    loadData(query: QueryEntity) {
        Finder.getQueryDescription(query.key)
            .then(qd => this.setState({ queryDescription: qd }))
            .done();
    }


    handleQueryChange = () => {

        const e = this.props.ctx.value;
        e.filters = [];
        e.simpleColumns = [];
        e.multiColumns = [];
        this.forceUpdate();

        this.setState({
            queryDescription: undefined
        }, () => {
            if (e.query)
                this.loadData(e.query);
        });
    }

    handleCreate = () => {

        var query = this.props.ctx.value.query;
        return Finder.parseSingleToken(query!.key, "Entity", SubTokensOptions.CanElement)
            .then(qt => PredictorMultiColumnEntity.New({
                query: query,
                groupKeys: [
                    newMListElement(PredictorGroupKeyEmbedded.New({
                        token: QueryTokenEmbedded.New({
                            token: qt,
                            tokenString: "Entity",
                        })
                    }))
                ]
            }));
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
                <Tabs>
                    <Tab eventKey="query" title={ctxxs.niceName(a => a.query)}>
                        <EntityLine ctx={ctxxs.subCtx(f => f.query)} remove={ctx.value.isNew} onChange={this.handleQueryChange} />
                        {queryKey && <div>

                            <FilterBuilderEmbedded ctx={ctxxs.subCtx(a => a.filters)}
                                queryKey={queryKey}
                                subTokenOptions={SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement} />
                            <EntityTable ctx={ctxxs.subCtx(e => e.simpleColumns)} columns={EntityTable.typedColumns<PredictorColumnEmbedded>([
                                { property: a => a.usage },
                                {
                                    property: a => a.token,
                                    template: ctx => <QueryTokenEntityBuilder
                                        ctx={ctx.subCtx(a => a.token)}
                                        queryKey={this.props.ctx.value.query!.key}
                                        subTokenOptions={SubTokensOptions.CanElement} />,
                                    headerHtmlAttributes: { style: { width: "40%" } },
                                },
                                { property: a => a.encoding },
                            ])} />
                            <EntityTabRepeater ctx={ctxxs.subCtx(e => e.multiColumns)} onCreate={this.handleCreate}
                                getTitle={(mctx: TypeContext<PredictorMultiColumnEntity>) => mctx.value.name || PredictorMultiColumnEntity.niceName()}
                                getComponent={(mctx: TypeContext<PredictorMultiColumnEntity>) =>
                                    <div>
                                        {!this.state.queryDescription ? undefined : <PredictorMultiColumn ctx={mctx} targetType={this.state.queryDescription.columns["Entity"].type} />}
                                    </div>
                                } />
                        </div>}
                    </Tab>
                    <Tab eventKey="algorithm" title={ctxxs.niceName(a => a.algorithmSettings)}>
                        <EntityCombo ctx={ctxxs.subCtx(f => f.algorithm)} onChange={this.handleAlgorithmChange} />
                        {ctxxs.value.algorithm && <EntityDetail ctx={ctxxs.subCtx(f => f.algorithmSettings)} remove={false} />}
                        <EntityDetail ctx={ctxxs.subCtx(f => f.settings)} remove={false} />
                    </Tab>
                    <Tab eventKey="result" title={ctxxs.niceName(a => a.files)}>
                        
                    </Tab>
                </Tabs>
            </div>
        );
    }

    renderFile = (ec: TypeContext<FilePathEmbedded>) => {
        const sc = ec.subCtx({ formGroupStyle: "SrOnly" });
        return (
            <div>
                <FileLine ctx={ec.subCtx(a => a.file)} remove={false}
                    fileType={Predictor.Attachment} />
            </div>
        );
    };
}