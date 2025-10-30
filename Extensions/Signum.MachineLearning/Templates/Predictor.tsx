import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { Tab, Tabs } from 'react-bootstrap'
import { AutoLine, EntityLine, EntityDetail, EntityCombo, EntityRepeater, EntityTable, IRenderButtons, EntityTabRepeater } from '@framework/Lines'
import { SearchControl, ColumnOption, FindOptions } from '@framework/Search'
import { TypeContext, ButtonsContext, ButtonBarElement } from '@framework/TypeContext'
import { FileLine } from '../../Signum.Files/Components/FileLine';
import { PredictorEntity, PredictorColumnEmbedded, PredictorMessage, PredictorSubQueryEntity, PredictorFileType, PredictorCodificationEntity, PredictorSubQueryColumnEmbedded, PredictorEpochProgressEntity, NeuralNetworkSettingsEntity, DefaultColumnEncodings } from '../Signum.MachineLearning'
import { Finder } from '@framework/Finder'
import { Navigator } from '@framework/Navigator'
import QueryTokenEmbeddedBuilder from '../../Signum.UserAssets/Templates/QueryTokenEmbeddedBuilder'
import { SubTokensOptions } from '@framework/FindOptions'
import { PredictorClient } from '../PredictorClient';
import { toLite } from "@framework/Signum.Entities";
import { newMListElement } from '@framework/Signum.Entities';
import FilterBuilderEmbedded from '../../Signum.UserAssets/Templates/FilterBuilderEmbedded';
import PredictorSubQuery from './PredictorSubQuery';
import ProgressBar from '@framework/Components/ProgressBar'
import LineChart, { LineChartSerie } from './LineChart'
import { QueryToken } from '@framework/FindOptions';
import PredictorMetrics from './PredictorMetrics';
import PredictorClassificationMetrics from './PredictorClassificationMetrics';
import PredictorRegressionMetrics from './PredictorRegressionMetrics';
import { useAPI, useForceUpdate, useInterval } from '@framework/Hooks'
import { QueryTokenEmbedded } from '../../Signum.UserAssets/Signum.UserAssets.Queries'
import { LinkButton } from '@framework/Basics/LinkButton'

export const Predictor: React.ForwardRefExoticComponent<{ ctx: TypeContext<PredictorEntity> } & React.RefAttributes<IRenderButtons>> =
  React.forwardRef(function Predictor({ ctx }: { ctx: TypeContext<PredictorEntity> }, ref: React.Ref<IRenderButtons>): React.ReactElement {

    const p = ctx.value;
    const queryDescription = useAPI(() => !p.mainQuery.query ? Promise.resolve(null) :
      Finder.getQueryDescription(p.mainQuery.query.key), [p.mainQuery.query?.key]);

    const forceUpdate = useForceUpdate();

    function handleClick() {

      if (!p.mainQuery.groupResults) {

        Finder.find({
          queryName: queryDescription!.queryKey,
          columnOptionsMode: "Add",
          columnOptions: p.mainQuery.columns.map(mle => ({ token: mle.element.token && mle.element.token.token!.fullKey }) as ColumnOption)
        })
          .then(lite => PredictorClient.predict(p, lite && { "Entity": lite }));

      } else {

        var fullKeys = p.mainQuery.columns.map(mle => mle.element.token!.tokenString!);

        Finder.findRow({
          queryName: queryDescription!.queryKey,
          groupResults: p.mainQuery.groupResults,
          columnOptionsMode: "ReplaceAll",
          columnOptions: fullKeys.map(fk => ({ token: fk }) as ColumnOption)
        }, { searchControlProps: { allowChangeColumns: false, showGroupButton: false } })
          .then(a => PredictorClient.predict(p, a && fullKeys.map((fk, i) => ({ tokenString: fk, value: a.row.columns[i] })).toObject(a => a.tokenString, a => a.value)));
      }
    }

    React.useImperativeHandle(ref, () => ({
      renderButtons(ctx: ButtonsContext): ButtonBarElement[] {
        if ((ctx.pack.entity as PredictorEntity).state == "Trained") {
          return [{
            order: 10000,
            button: <button className="btn btn-info" onClick={handleClick}><FontAwesomeIcon icon={["far", "lightbulb"]} />&nbsp;{PredictorMessage.Predict.niceToString()}</button >
          }];
        } else {
          return [];
        }
      }
    }), [p?.state]);




    function handleQueryChange() {

      p.mainQuery.filters.clear();
      p.mainQuery.columns.clear();
    }

    function handleGroupChange() {

      p.mainQuery.filters.forEach(a => a.element.token = fixTokenEmbedded(a.element.token ?? null, p.mainQuery.groupResults)!);
      p.mainQuery.columns.forEach(a => a.element.token = fixTokenEmbedded(a.element.token ?? null, p.mainQuery.groupResults)!);
      forceUpdate();
    }


    function handleCreate() {

      var mq = p.mainQuery;

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

    function handleAlgorithmChange() {
      var al = p.algorithm;
      if (al == null)
        p.algorithmSettings = null!;
      else {
        var init = PredictorClient.initializers[al.key];

        if (init != null)
          init(p);
      }

      forceUpdate();
    }

    function handleOnFinished() {
      Navigator.API.fetchEntityPack(toLite(ctx.value))
        .then(pack => ctx.frame!.onReload(pack));
    }

    function handlePreviewMainQuery(e: React.MouseEvent<any>) {
      var mq = p.mainQuery;

      var canAggregate = mq.groupResults ? SubTokensOptions.CanAggregate : 0;

      FilterBuilderEmbedded.toFilterOptionParsed(queryDescription!, mq.filters, SubTokensOptions.CanElement | SubTokensOptions.CanAnyAll | canAggregate)
        .then(filters => {
          var fo: FindOptions = {
            queryName: mq.query!.key,
            groupResults: mq.groupResults,
            filterOptions: Finder.toFilterOptions(filters),
            columnOptions: mq.columns.orderBy(mle => mle.element.usage == "Input" ? 0 : 1).map(mle => ({
              token: mle.element.token && mle.element.token.tokenString,
            } as ColumnOption)),
            columnOptionsMode: "ReplaceAll",
          };

          Finder.exploreWindowsOpen(fo, e);
        });
    }

    if (ctx.value.state != "Draft")
      ctx = ctx.subCtx({ readOnly: true });

    const ctxxs = ctx.subCtx({ formSize: "xs" });
    const ctxxs4 = ctx.subCtx({ labelColumns: 4 });
    const ctxmq = ctxxs.subCtx(a => a.mainQuery);
    const entity = ctx.value;
    const queryKey = entity.mainQuery.query && entity.mainQuery.query.key;

    var canAggregate = entity.mainQuery.groupResults ? SubTokensOptions.CanAggregate : 0;

    return (
      <div>
        <div className="row">
          <div className="col-sm-6">
            <AutoLine ctx={ctxxs4.subCtx(e => e.name)} readOnly={ctx.readOnly} />
            <AutoLine ctx={ctxxs4.subCtx(e => e.state, { readOnly: true })} />
            <EntityLine ctx={ctxxs4.subCtx(e => e.trainingException, { readOnly: true })} hideIfNull={true} />
          </div>
          <div className="col-sm-6">
            <EntityCombo ctx={ctxxs4.subCtx(f => f.algorithm)} onChange={handleAlgorithmChange} />
            <EntityCombo ctx={ctxxs4.subCtx(f => f.resultSaver)} />
            <EntityCombo ctx={ctxxs4.subCtx(f => f.publication)} readOnly={true} />
          </div>
        </div>
        {ctx.value.state == "Training" && <TrainingProgressComponent ctx={ctx} onStateChanged={handleOnFinished} />}
        <Tabs id="predictorTabs" mountOnEnter={true} unmountOnExit={true} >
          <Tab eventKey="query" title={ctxmq.niceName(a => a.query)}>
            <div>
              <fieldset>
                <legend>{ctxmq.niceName()}</legend>
                <EntityLine ctx={ctxmq.subCtx(f => f.query)} remove={ctx.value.isNew} onChange={handleQueryChange} />
                {queryKey && <div>
                  <AutoLine ctx={ctxmq.subCtx(f => f.groupResults)} onChange={handleGroupChange} />

                  <FilterBuilderEmbedded ctx={ctxmq.subCtx(a => a.filters)}
                    queryKey={queryKey}
                    subTokenOptions={SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement | canAggregate} />
                  <EntityTable ctx={ctxmq.subCtx(e => e.columns)} columns={[
                    { property: a => a.usage },
                    {
                      property: a => a.token,
                      template: (cctx, row) => <QueryTokenEmbeddedBuilder
                        ctx={cctx.subCtx(a => a.token)}
                        queryKey={p.mainQuery.query!.key}
                        subTokenOptions={SubTokensOptions.CanElement | canAggregate}
                        onTokenChanged={() => { initializeColumn(ctx.value, cctx.value); row.forceUpdate() }} />,
                      headerHtmlAttributes: { style: { width: "40%" } },
                    },
                    { property: a => a.encoding },
                    { property: a => a.nullHandling },
                  ]} />
                  {ctxmq.value.query && <LinkButton title={undefined} onClick={handlePreviewMainQuery}>{PredictorMessage.Preview.niceToString()}</LinkButton>}
                </div>}

              </fieldset>
              {queryKey && <EntityTabRepeater ctx={ctxxs.subCtx(e => e.subQueries)} onCreate={handleCreate}
                getTitle={(mctx: TypeContext<PredictorSubQueryEntity>) => mctx.value.name || PredictorSubQueryEntity.niceName()}
                getComponent={mctx =>
                  <div>
                    {!queryDescription ? undefined : <PredictorSubQuery ctx={mctx} mainQuery={ctxmq.value} mainQueryDescription={queryDescription} />}
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
              <SearchControl findOptions={{ queryName: PredictorCodificationEntity, filterOptions: [{ token: PredictorCodificationEntity.token(e => e.predictor), value: ctx.value }] }} />
            </Tab>
          }
          {
            ctx.value.state != "Draft" && <Tab eventKey="progress" title={PredictorMessage.Progress.niceToString()}>
              {ctx.value.state == "Trained" && <EpochProgressComponent ctx={ctx} />}
              <SearchControl findOptions={{ queryName: PredictorEpochProgressEntity, filterOptions: [{ token: PredictorEpochProgressEntity.token(e => e.predictor), value: ctx.value }] }} />
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
        </Tabs>
      </div>
    );
  });

export default Predictor;

export function initializeColumn(p: PredictorEntity, pc: PredictorColumnEmbedded | PredictorSubQueryColumnEmbedded): void {
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

export function TrainingProgressComponent(p: TrainingProgressComponentProps): React.JSX.Element {

  const tick = useInterval(500, 0, n => n + 1);

  const tp = useAPI<PredictorClient.TrainingProgress>((abort, prevState) => PredictorClient.API.getTrainingState(toLite(p.ctx.value))
    .then(newState => {
      if (prevState != null && prevState.state != newState.state)
        p.onStateChanged();
      return newState;
    }), [tick, p.ctx.value], { avoidReset: true });


  return (
    <div>
      {tp?.epochProgressesParsed && <LineChart height={200} series={getSeries(tp.epochProgressesParsed, p.ctx.value)} />}
      <ProgressBar color={tp == null || tp.running == false ? "warning" : null}
        value={tp?.progress}
        message={tp == null ? PredictorMessage.StartingTraining.niceToString() : tp.message}
      />
    </div>
  );
}


interface EpochProgressComponentProps {
  ctx: TypeContext<PredictorEntity>;
}

export function EpochProgressComponent(p: EpochProgressComponentProps): React.JSX.Element {

  const eps = useAPI(() => PredictorClient.API.getEpochLosses(toLite(p.ctx.value)), [p.ctx.value]);

  return (
    <div>
      {eps && <LineChart height={200} series={getSeries(eps, p.ctx.value)} />}
    </div>
  );
}

function getSeries(eps: Array<PredictorClient.EpochProgress>, predictor: PredictorEntity): LineChartSerie[] {

  const algSet = predictor.algorithmSettings;

  const nns = NeuralNetworkSettingsEntity.isInstance(algSet) ? algSet : undefined;

  var maxLoss = eps.flatMap(a => [a.lossTraining, a.lossValidation]).max()!;
  var maxEvaluation = eps.flatMap(a => [a.accuracyTraining, a.accuracyValidation]).max()!;

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
      name: PredictorEpochProgressEntity.nicePropertyName(a => a.accuracyTraining),
      title: nns && nns!.evalErrorFunction,
      color: "#731c7b",
      values: eps.filter(a => a.accuracyTraining != null).map(ep => ({ x: ep.trainingExamples, y: ep.accuracyTraining })),
      minValue: 0,
      maxValue: maxEvaluation,
      strokeWidth: "1px",
    },
    {
      name: PredictorEpochProgressEntity.nicePropertyName(a => a.accuracyValidation),
      title: nns && nns!.evalErrorFunction,
      color: "#d980d9",
      values: eps.filter(a => a.accuracyValidation != null).map(ep => ({ x: ep.trainingExamples, y: ep.accuracyValidation! })),
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
