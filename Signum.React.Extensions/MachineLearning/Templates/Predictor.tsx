import * as React from 'react'
import * as OrderUtils from '@framework/Frames/OrderUtils'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { Tab, UncontrolledTabs } from '@framework/Components/Tabs'
import { ValueLine, EntityLine, EntityDetail, EntityCombo, EntityRepeater, EntityTable, IRenderButtons, EntityTabRepeater } from '@framework/Lines'
import { SearchControl, ColumnOption, FindOptions } from '@framework/Search'
import { TypeContext, ButtonsContext } from '@framework/TypeContext'
import FileLine from '../../Files/FileLine'
import { PredictorEntity, PredictorColumnEmbedded, PredictorMessage, PredictorSubQueryEntity, PredictorFileType, PredictorCodificationEntity, PredictorSubQueryColumnEmbedded, PredictorEpochProgressEntity, NeuralNetworkSettingsEntity, DefaultColumnEncodings } from '../Signum.Entities.MachineLearning'
import * as Finder from '@framework/Finder'
import * as Navigator from '@framework/Navigator'
import QueryTokenEmbeddedBuilder from '../../UserAssets/Templates/QueryTokenEmbeddedBuilder'
import { QueryDescription, SubTokensOptions } from '@framework/FindOptions'
import * as PredictorClient from '../PredictorClient';
import { toLite } from "@framework/Signum.Entities";
import { newMListElement } from '@framework/Signum.Entities';
import FilterBuilderEmbedded from '../../UserAssets/Templates/FilterBuilderEmbedded';
import PredictorSubQuery from './PredictorSubQuery';
import { QueryTokenEmbedded } from '../../UserAssets/Signum.Entities.UserAssets';
import { QueryEntity } from '@framework/Signum.Entities.Basics';
import { is } from '@framework/Signum.Entities';
import ProgressBar from './ProgressBar'
import LineChart, { LineChartSerie } from './LineChart'
import { QueryToken } from '@framework/FindOptions';
import PredictorMetrics from './PredictorMetrics';
import PredictorClassificationMetrics from './PredictorClassificationMetrics';
import PredictorRegressionMetrics from './PredictorRegressionMetrics';
import { toFilterOptions } from '@framework/Finder';

export default class Predictor extends React.Component<{ ctx: TypeContext<PredictorEntity> }, { queryDescription?: QueryDescription }> implements IRenderButtons {
  handleClick = () => {
    var p = this.props.ctx.value;

    if (!p.mainQuery.groupResults) {

      Finder.find({
        queryName: this.state.queryDescription!.queryKey,
        columnOptionsMode: "Add",
        columnOptions: p.mainQuery.columns.map(mle => ({ token: mle.element.token && mle.element.token.token!.fullKey }) as ColumnOption)
      })
        .then(lite => PredictorClient.predict(p, lite && { "Entity": lite }))
        .done();

    } else {

      var fullKeys = p.mainQuery.columns.map(mle => mle.element.token!.tokenString!);

      Finder.findRow({
        queryName: this.state.queryDescription!.queryKey,
        groupResults: p.mainQuery.groupResults,
        columnOptionsMode: "Replace",
        columnOptions: fullKeys.map(fk => ({ token: fk }) as ColumnOption)
      }, { searchControlProps: { allowChangeColumns: false, showGroupButton: false } })
        .then(row => PredictorClient.predict(p, row && fullKeys.map((fk, i) => ({ tokenString: fk, value: row!.columns[i] })).toObject(a => a.tokenString, a => a.value)))
        .done();
    }
  }

  renderButtons(ctx: ButtonsContext): (React.ReactElement<any> | undefined)[] {
    if ((ctx.pack.entity as PredictorEntity).state == "Trained") {
      return [OrderUtils.setOrder(10000, <button className="btn btn-info" onClick={this.handleClick}><FontAwesomeIcon icon={["far", "lightbulb"]} />&nbsp;{PredictorMessage.Predict.niceToString()}</button >)];
    } else {
      return [];
    }
  }

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
    p.mainQuery.filters.clear();
    p.mainQuery.columns.clear();

    this.setState({
      queryDescription: undefined
    }, () => {
      if (p.mainQuery.query)
        this.loadData(p.mainQuery.query);
    });
  }

  handleGroupChange = () => {

    const p = this.props.ctx.value;
    p.mainQuery.filters.forEach(a => a.element.token = fixTokenEmbedded(a.element.token || null, p.mainQuery.groupResults || false));
    p.mainQuery.columns.forEach(a => a.element.token = fixTokenEmbedded(a.element.token || null, p.mainQuery.groupResults || false));
    this.forceUpdate();
  }


  handleCreate = () => {

    var mq = this.props.ctx.value.mainQuery;

    var promise = !mq.groupResults ? Finder.parseSingleToken(mq.query!.key, "Entity", SubTokensOptions.CanElement).then(t => [t]) :
      Promise.resolve(mq.columns.map(a => a.element.token).filter(t => t != null && t.token != null && t.token.queryTokenType != "Aggregate").map(t => t!.token!));


    return promise.then(keys => PredictorSubQueryEntity.New({
      query: mq.query,
      columns: keys.map(t =>
        newMListElement(PredictorSubQueryColumnEmbedded.New({
          usage: "ParentKey",
          token: QueryTokenEmbedded.New({
            token: t,
            tokenString: t.fullKey,
          })
        }))
      )
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

    var canAggregate = mq.groupResults ? SubTokensOptions.CanAggregate : 0;

    FilterBuilderEmbedded.toFilterOptionParsed(this.state.queryDescription!, mq.filters, SubTokensOptions.CanElement | SubTokensOptions.CanAnyAll | canAggregate)
      .then(filters => {
        var fo: FindOptions = {
          queryName: mq.query!.key,
          groupResults: mq.groupResults,
          filterOptions: toFilterOptions(filters),
          columnOptions: mq.columns.orderBy(mle => mle.element.usage == "Input" ? 0 : 1).map(mle => ({
            token: mle.element.token && mle.element.token.tokenString,
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

    const ctxxs = ctx.subCtx({ formSize: "ExtraSmall" });
    const ctxxs4 = ctx.subCtx({ labelColumns: 4 });
    const ctxmq = ctxxs.subCtx(a => a.mainQuery);
    const entity = ctx.value;
    const queryKey = entity.mainQuery.query && entity.mainQuery.query.key;

    var canAggregate = entity.mainQuery.groupResults ? SubTokensOptions.CanAggregate : 0;

    return (
      <div>
        <div className="row">
          <div className="col-sm-6">
            <ValueLine ctx={ctxxs4.subCtx(e => e.name)} readOnly={this.props.ctx.readOnly} />
            <ValueLine ctx={ctxxs4.subCtx(e => e.state, { readOnly: true })} />
            <EntityLine ctx={ctxxs4.subCtx(e => e.trainingException, { readOnly: true })} hideIfNull={true} />
          </div>
          <div className="col-sm-6">
            <EntityCombo ctx={ctxxs4.subCtx(f => f.algorithm)} onChange={this.handleAlgorithmChange} />
            <EntityCombo ctx={ctxxs4.subCtx(f => f.resultSaver)} />
            <EntityCombo ctx={ctxxs4.subCtx(f => f.publication)} readOnly={true} />
          </div>
        </div>
        {ctx.value.state == "Training" && <TrainingProgressComponent ctx={ctx} onStateChanged={this.handleOnFinished} />}
        <UncontrolledTabs>
          <Tab eventKey="query" title={ctxmq.niceName(a => a.query)}>
            <div>
              <fieldset>
                <legend>{ctxmq.niceName()}</legend>
                <EntityLine ctx={ctxmq.subCtx(f => f.query)} remove={ctx.value.isNew} onChange={this.handleQueryChange} />
                {queryKey && <div>
                  <ValueLine ctx={ctxmq.subCtx(f => f.groupResults)} onChange={this.handleGroupChange} />

                  <FilterBuilderEmbedded ctx={ctxmq.subCtx(a => a.filters)}
                    queryKey={queryKey}
                    subTokenOptions={SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement | canAggregate}
                    showUserFilters={false} />
                  <EntityTable ctx={ctxmq.subCtx(e => e.columns)} columns={EntityTable.typedColumns<PredictorColumnEmbedded>([
                    { property: a => a.usage },
                    {
                      property: a => a.token,
                      template: (cctx, row) => <QueryTokenEmbeddedBuilder
                        ctx={cctx.subCtx(a => a.token)}
                        queryKey={this.props.ctx.value.mainQuery.query!.key}
                        subTokenOptions={SubTokensOptions.CanElement | canAggregate}
                        onTokenChanged={() => { initializeColumn(ctx.value, cctx.value); row.forceUpdate() }} />,
                      headerHtmlAttributes: { style: { width: "40%" } },
                    },
                    { property: a => a.encoding },
                    { property: a => a.nullHandling },
                  ])} />
                  {ctxmq.value.query && <a href="#" onClick={this.handlePreviewMainQuery}>{PredictorMessage.Preview.niceToString()}</a>}
                </div>}

              </fieldset>
              {queryKey && <EntityTabRepeater ctx={ctxxs.subCtx(e => e.subQueries)} onCreate={this.handleCreate}
                getTitle={(mctx: TypeContext<PredictorSubQueryEntity>) => mctx.value.name || PredictorSubQueryEntity.niceName()}
                getComponent={(mctx: TypeContext<PredictorSubQueryEntity>) =>
                  <div>
                    {!this.state.queryDescription ? undefined : <PredictorSubQuery ctx={mctx} mainQuery={ctxmq.value} mainQueryDescription={this.state.queryDescription} />}
                  </div>
                } />}
            </div>
          </Tab>
          <Tab eventKey="settings" title={ctxxs.niceName(a => a.settings)}>
            {ctxxs.value.algorithm && <EntityDetail ctx={ctxxs.subCtx(f => f.algorithmSettings)} remove={false} />}
            <EntityDetail ctx={ctxxs.subCtx(f => f.settings)} remove={false} />
          </Tab>
          {
            ctx.value.state != "Draft" && <Tab eventKey="codifications" title={PredictorMessage.Codifications.niceToString()}>
              <SearchControl findOptions={{ queryName: PredictorCodificationEntity, parentToken: PredictorCodificationEntity.token(e => e.predictor), parentValue: ctx.value }} />
            </Tab>
          }
          {
            ctx.value.state != "Draft" && <Tab eventKey="progress" title={PredictorMessage.Progress.niceToString()}>
              {ctx.value.state == "Trained" && <EpochProgressComponent ctx={ctx} />}
              <SearchControl findOptions={{ queryName: PredictorEpochProgressEntity, parentToken: PredictorEpochProgressEntity.token(e => e.predictor), parentValue: ctx.value }} />
            </Tab>
          }
          {
            ctx.value.state == "Trained" && <Tab eventKey="files" title={PredictorMessage.Results.niceToString()}>
              {ctx.value.resultTraining && ctx.value.resultValidation && <PredictorMetrics ctx={ctx} />}
              {ctx.value.classificationTraining && ctx.value.classificationValidation && <PredictorClassificationMetrics ctx={ctx} />}
              {ctx.value.regressionTraining && ctx.value.regressionTraining && <PredictorRegressionMetrics ctx={ctx} />}
              {ctx.value.resultSaver && PredictorClient.getResultRendered(ctx)}
              <EntityRepeater ctx={ctxxs.subCtx(f => f.files)} getComponent={ec =>
                <FileLine ctx={ec.subCtx({ formGroupStyle: "SrOnly" })} remove={false} fileType={PredictorFileType.PredictorFile} />
              } />
            </Tab>
          }
        </UncontrolledTabs>
      </div>
    );
  }
}

export function initializeColumn(p: PredictorEntity, pc: PredictorColumnEmbedded | PredictorSubQueryColumnEmbedded) {
  var token = pc.token && pc.token.token;
  if (token) {
    pc.encoding =
      token.type.name == "number" || token.type.name == "decimal" ? DefaultColumnEncodings.NormalizeZScore :
        token.type.name == "boolean" ? DefaultColumnEncodings.None :
          DefaultColumnEncodings.OneHot;

    pc.nullHandling = "Zero";
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

  timeoutHandler!: number;

  loadData(props: TrainingProgressComponentProps) {
    PredictorClient.API.getTrainingState(toLite(props.ctx.value))
      .then(p => {
        var prev = this.state.trainingProgress;
        this.setState({ trainingProgress: p });
        if (prev != null && prev.state != p.state)
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
        {tp && tp.epochProgressesParsed && <LineChart height={200} series={getSeries(tp.epochProgressesParsed, this.props.ctx.value)} />}
        <ProgressBar color={tp == null || tp.running == false ? "warning" : null}
          value={tp && tp.progress}
          message={tp == null ? PredictorMessage.StartingTraining.niceToString() : tp.message}
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
        {eps && <LineChart height={200} series={getSeries(eps, this.props.ctx.value)} />}
      </div>
    );
  }

}

function getSeries(eps: Array<PredictorClient.EpochProgress>, predictor: PredictorEntity): LineChartSerie[] {

  const algSet = predictor.algorithmSettings;

  const nns = NeuralNetworkSettingsEntity.isInstance(algSet) ? algSet : undefined;

  var maxLoss = eps.flatMap(a => [a.lossTraining, a.lossValidation]).max()!;
  var maxEvaluation = eps.flatMap(a => [a.evaluationTraining, a.evaluationValidation]).max()!;

  return [
    {
      name: PredictorEpochProgressEntity.nicePropertyName(a => a.lossTraining),
      title: nns && nns!.lossFunction,
      color: "#1A5276",
      values: eps.filter(a => a.lossTraining != null).map(ep => ({ x: ep.trainingExamples, y: ep.lossTraining })),
      minValue: 0,
      maxValue: maxLoss,
      strokeWidth: "2px",
    },
    {
      name: PredictorEpochProgressEntity.nicePropertyName(a => a.lossValidation),
      title: nns && nns!.lossFunction,
      color: "#5DADE2",
      values: eps.filter(a => a.lossValidation != null).map(ep => ({ x: ep.trainingExamples, y: ep.lossValidation! })),
      minValue: 0,
      maxValue: maxLoss,
      strokeWidth: "2px",
    },
    {
      name: PredictorEpochProgressEntity.nicePropertyName(a => a.evaluationTraining),
      title: nns && nns!.evalErrorFunction,
      color: "#731c7b",
      values: eps.filter(a => a.evaluationTraining != null).map(ep => ({ x: ep.trainingExamples, y: ep.evaluationTraining })),
      minValue: 0,
      maxValue: maxEvaluation,
      strokeWidth: "1px",
    },
    {
      name: PredictorEpochProgressEntity.nicePropertyName(a => a.evaluationValidation),
      title: nns && nns!.evalErrorFunction,
      color: "#d980d9",
      values: eps.filter(a => a.evaluationValidation != null).map(ep => ({ x: ep.trainingExamples, y: ep.evaluationValidation! })),
      minValue: 0,
      maxValue: maxEvaluation,
      strokeWidth: "1px",
    }
  ];
}

function fixTokenEmbedded(token: QueryTokenEmbedded | null, groupResults: boolean): QueryTokenEmbedded | null {
  if (token == undefined)
    return null;

  const t = token.token!;

  const ft = fixToken(t, groupResults);

  if (t == ft)
    return token;

  if (ft == null)
    return null;

  return QueryTokenEmbedded.New({ token: ft, tokenString: ft.fullKey });

}

function fixToken(token: QueryToken | null, groupResults: boolean): QueryToken | null {
  if (token == undefined)
    return null;

  if (groupResults && token.queryTokenType == "Aggregate")
    return token.parent || null;

  return token;
}
