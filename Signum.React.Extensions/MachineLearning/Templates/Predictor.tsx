import * as React from 'react'
import { Tabs, Tab } from 'react-bootstrap';
import * as numbro from 'numbro';
import { classes } from '../../../../Framework/Signum.React/Scripts/Globals'
import { FormGroup, FormControlStatic, ValueLine, ValueLineType, EntityLine, EntityDetail, EntityCombo, EntityList, EntityRepeater, EntityTable, IRenderButtons, EntityTabRepeater } from '../../../../Framework/Signum.React/Scripts/Lines'
import { SearchControl, FilterOption, ColumnOption, FindOptions } from '../../../../Framework/Signum.React/Scripts/Search'
import { TypeContext, FormGroupStyle, ButtonsContext } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import FileLine from '../../Files/FileLine'
import { PredictorEntity, PredictorColumnEmbedded, PredictorMessage, PredictorSubQueryEntity, PredictorGroupKeyEmbedded, PredictorFileType, PredictorCodificationEntity, PredictorEpochProgressEntity } from '../Signum.Entities.MachineLearning'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import { getQueryNiceName } from '../../../../Framework/Signum.React/Scripts/Reflection'
import QueryTokenEntityBuilder from '../../UserAssets/Templates/QueryTokenEntityBuilder'
import { QueryFilterEmbedded } from '../../UserQueries/Signum.Entities.UserQueries'
import { QueryDescription, SubTokensOptions } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import * as PredictorClient from '../PredictorClient';
import { toLite } from "../../../../Framework/Signum.React/Scripts/Signum.Entities";
import FilterBuilder from '../../../../Framework/Signum.React/Scripts/SearchControl/FilterBuilder';
import { MList, newMListElement } from '../../../../Framework/Signum.React/Scripts/Signum.Entities';
import FilterBuilderEmbedded from './FilterBuilderEmbedded';
import PredictorSubQuery from './PredictorSubQuery';
import { QueryTokenEmbedded } from '../../UserAssets/Signum.Entities.UserAssets';
import { QueryEntity } from '../../../../Framework/Signum.React/Scripts/Signum.Entities.Basics';
import { FilePathEmbedded } from '../../Files/Signum.Entities.Files';
import { is } from '../../../../Framework/Signum.React/Scripts/Signum.Entities';
import ProgressBar from './ProgressBar'
import LineChart from './LineChart/LineChart'

export default class Predictor extends React.Component<{ ctx: TypeContext<PredictorEntity> }, { queryDescription?: QueryDescription }> {

    constructor(props: any) {
        super(props);
        this.state = { queryDescription: undefined };
    }

    componentWillMount() {

        let p = this.props.ctx.value;
        if (p.mainQuery.query)
            this.loadData(p.mainQuery.query);
    }

    loadData(query: QueryEntity) {
        Finder.getQueryDescription(query.key)
            .then(qd => this.setState({ queryDescription: qd }))
            .done();
    }


    handleQueryChange = () => {

        const p = this.props.ctx.value;
        p.mainQuery.filters = [];
        p.mainQuery.columns = [];
        p.subQueries = [];
        this.forceUpdate();

        this.setState({
            queryDescription: undefined
        }, () => {
            if (p.mainQuery.query)
                this.loadData(p.mainQuery.query);
        });
    }

    handleCreate = () => {

        var query = this.props.ctx.value.mainQuery.query;
        return Finder.parseSingleToken(query!.key, "Entity", SubTokensOptions.CanElement)
            .then(qt => PredictorSubQueryEntity.New({
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
        else {
            var init = PredictorClient.initializers[al.key];

            if (init != null)
                init(pred);
        }

        this.forceUpdate();
    }

    handleOnFinished = () => {
        const ctx = this.props.ctx;
        Navigator.API.fetchEntityPack(toLite(ctx.value))
            .then(pack => ctx.frame!.onReload(pack))
            .done();
    }

    handlePreviewMainQuery = (e: React.MouseEvent<any>) => {
        e.preventDefault();
        e.persist();
        var mq = this.props.ctx.value.mainQuery;

        FilterBuilderEmbedded.toFilterOptionParsed(this.state.queryDescription!, mq.filters, SubTokensOptions.CanElement | SubTokensOptions.CanAnyAll)
            .then(filters => {
                var fo: FindOptions = {
                    queryName: mq.query!.key,
                    filterOptions: filters.map(f => ({
                        columnName: f.token!.fullKey,
                        operation: f.operation,
                        value: f.value
                    }) as FilterOption),
                    columnOptions: mq.columns.orderBy(mle => mle.element.usage == "Input" ? 0 : 1).map(mle => ({
                        columnName: mle.element.token && mle.element.token.tokenString,
                    } as ColumnOption)),
                    columnOptionsMode: "Replace",
                };

                Finder.exploreWindowsOpen(fo, e);
            })
            .done();
    }

    render() {
        let ctx = this.props.ctx;

        if (ctx.value.state != "Draft")
            ctx = ctx.subCtx({ readOnly: true });

        const ctxxs = ctx.subCtx({ formGroupSize: "ExtraSmall" });
        const ctxmq = ctxxs.subCtx(a => a.mainQuery);
        const entity = ctx.value;
        const queryKey = entity.mainQuery.query && entity.mainQuery.query.key;

        return (
            <div>
                <ValueLine ctx={ctxxs.subCtx(e => e.name)} />
                <EntityCombo ctx={ctxxs.subCtx(f => f.algorithm)} onChange={this.handleAlgorithmChange} />
                <ValueLine ctx={ctxxs.subCtx(e => e.state, { readOnly: true })} />
                <EntityLine ctx={ctxxs.subCtx(e => e.trainingException, { readOnly: true })} hideIfNull={true} />
                {ctx.value.state == "Training" && <TrainingProgressComponent ctx={ctx} onStateChanged={this.handleOnFinished} />}
                <Tabs id={ctx.prefix + "tabs"} unmountOnExit={true}>
                    <Tab eventKey="query" title={ctxmq.niceName(a => a.query)}>
                        <EntityLine ctx={ctxmq.subCtx(f => f.query)} remove={ctx.value.isNew} onChange={this.handleQueryChange} />
                        {queryKey &&
                            <div>
                                <fieldset>
                                    <legend>{ctxmq.niceName()}</legend>
                                    <div>
                                        <FilterBuilderEmbedded ctx={ctxmq.subCtx(a => a.filters)}
                                            queryKey={queryKey}
                                            subTokenOptions={SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement} />
                                        <EntityTable ctx={ctxmq.subCtx(e => e.columns)} columns={EntityTable.typedColumns<PredictorColumnEmbedded>([
                                            { property: a => a.usage },
                                            {
                                                property: a => a.token,
                                                template: ctx => <QueryTokenEntityBuilder
                                                    ctx={ctx.subCtx(a => a.token)}
                                                    queryKey={this.props.ctx.value.mainQuery.query!.key}
                                                    subTokenOptions={SubTokensOptions.CanElement} />,
                                                headerHtmlAttributes: { style: { width: "40%" } },
                                            },
                                            { property: a => a.encoding },
                                        ])} />
                                        {ctxmq.value.query && <a href="#" onClick={this.handlePreviewMainQuery}>{PredictorMessage.Preview.niceToString()}</a>}
                                    </div>

                                </fieldset>
                                <EntityTabRepeater ctx={ctxxs.subCtx(e => e.subQueries)} onCreate={this.handleCreate}
                                    getTitle={(mctx: TypeContext<PredictorSubQueryEntity>) => mctx.value.name || PredictorSubQueryEntity.niceName()}
                                    getComponent={(mctx: TypeContext<PredictorSubQueryEntity>) =>
                                        <div>
                                            {!this.state.queryDescription ? undefined : <PredictorSubQuery ctx={mctx} mainQuery={ctxmq.value} targetType={this.state.queryDescription.columns["Entity"].type} />}
                                        </div>
                                    } />
                            </div>
                        }
                    </Tab>
                    <Tab eventKey="settings" title={ctxxs.niceName(a => a.settings)}>
                        {ctxxs.value.algorithm && <EntityDetail ctx={ctxxs.subCtx(f => f.algorithmSettings)} remove={false} />}
                        <EntityDetail ctx={ctxxs.subCtx(f => f.settings)} remove={false} />
                    </Tab>
                    {
                        ctx.value.state != "Draft" && <Tab eventKey="codifications" title={PredictorMessage.Codifications.niceToString()}>
                            <SearchControl findOptions={{ queryName: PredictorCodificationEntity, parentColumn: "Predictor", parentValue: ctx.value }} />
                        </Tab>
                    }
                    {
                        ctx.value.state != "Draft" && <Tab eventKey="progress" title={PredictorMessage.Progress.niceToString()}>
                            {ctx.value.state == "Trained" && <EpochProgressComponent ctx={ctx} />}
                            <SearchControl findOptions={{ queryName: PredictorEpochProgressEntity, parentColumn: "Predictor", parentValue: ctx.value }} />
                        </Tab>
                    }
                    {
                        ctx.value.state == "Trained" && <Tab eventKey="files" title={PredictorMessage.Results.niceToString()}>
                            <div className="form-vertical">
                                <EntityRepeater ctx={ctxxs.subCtx(f => f.files)} getComponent={ec =>
                                    <FileLine ctx={ec.subCtx({ formGroupStyle: "SrOnly" })} remove={false} fileType={PredictorFileType.PredictorFile} />
                                } />
                            </div>

                            <EntityDetail ctx={ctxxs.subCtx(f => f.classificationTraining)} />
                            <EntityDetail ctx={ctxxs.subCtx(f => f.classificationValidation)} />
                            <EntityDetail ctx={ctxxs.subCtx(f => f.regressionTraining)} />
                            <EntityDetail ctx={ctxxs.subCtx(f => f.regressionValidation)} />
                        </Tab>
                    }
                </Tabs>
            </div>
        );
    }
}


interface TrainingProgressComponentProps {
    ctx: TypeContext<PredictorEntity>;
    onStateChanged: () => void;
}

interface TrainingProgressComponentState {
    trainingProgress?: PredictorClient.TrainingProgress | null;
}

export class TrainingProgressComponent extends React.Component<TrainingProgressComponentProps, TrainingProgressComponentState> {

    constructor(props: TrainingProgressComponentProps) {
        super(props);
        this.state = {};
    }

    componentWillMount() {
        this.loadData(this.props);
    }

    componentWillReceiveProps(newProps: TrainingProgressComponentProps) {
        if (!is(newProps.ctx.value, this.props.ctx.value))
            this.loadData(newProps);
    }

    componentWillUnmount() {
        if (this.timeoutHandler)
            clearTimeout(this.timeoutHandler);
    }

    refreshInterval = 500;

    timeoutHandler: number;

    loadData(props: TrainingProgressComponentProps) {
        PredictorClient.API.getTrainingState(toLite(props.ctx.value))
            .then(p => {
                var prev = this.state.trainingProgress;
                this.setState({ trainingProgress: p });
                if (prev != null && prev.State != p.State)
                    this.props.onStateChanged();
                else
                    this.timeoutHandler = setTimeout(() => this.loadData(this.props), this.refreshInterval);
            })
            .done();
    }

    render() {

        const tp = this.state.trainingProgress;

        return (
            <div>
                {tp && tp.EpochProgressesParsed && <LineChart height={200} series={[{
                    color: "rgb(0,0,0)",
                    name: "data",
                    values: tp.EpochProgressesParsed.map(ep => ({ x: ep.TrainingExamples, y: ep.LossTraining }))
                },]} />}
                <ProgressBar color={tp == null ? "info" : "default"}
                    value={tp && tp.Progress}
                    message={tp == null ? PredictorMessage.StartingTraining.niceToString() : tp.Message}
                />
            </div>
        );
    }

}


interface EpochProgressComponentProps {
    ctx: TypeContext<PredictorEntity>;
}

interface EpochProgressComponentState {
    epochProgress?: PredictorClient.EpochProgress[] | null;
}

export class EpochProgressComponent extends React.Component<EpochProgressComponentProps, EpochProgressComponentState> {

    constructor(props: EpochProgressComponentProps) {
        super(props);
        this.state = {};
    }

    componentWillMount() {
        this.loadData(this.props);
    }

    componentWillReceiveProps(newProps: EpochProgressComponentProps) {
        if (!is(newProps.ctx.value, this.props.ctx.value))
            this.loadData(newProps);
    }

    loadData(props: EpochProgressComponentProps) {
        PredictorClient.API.getEpochLosses(toLite(props.ctx.value))
            .then(p => {
                var prev = this.state.epochProgress;
                this.setState({ epochProgress: p });
            })
            .done();
    }

    render() {

        const eps = this.state.epochProgress;

        return (
            <div>
                {eps && <LineChart height={200} series={[{
                    color: "rgb(0,0,0)",
                    name: "data",
                    values: eps.map(ep => ({ x: ep.TrainingExamples, y: ep.LossTraining }))
                },]} />}
            </div>
        );
    }

}

