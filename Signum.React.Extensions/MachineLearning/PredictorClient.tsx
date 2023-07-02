import * as React from 'react'
import * as Constructor from '@framework/Constructor';
import { ajaxPost, ajaxGet, saveFile, ajaxGetRaw } from '@framework/Services';
import { EntitySettings } from '@framework/Navigator'
import * as Navigator from '@framework/Navigator'
import * as Finder from '@framework/Finder'
import { Lite, toLite } from '@framework/Signum.Entities'
import { EntityOperationSettings } from '@framework/Operations'
import { symbolNiceName, toNumberFormat } from '@framework/Reflection'
import * as Operations from '@framework/Operations'
import {
  PredictorEntity, PredictorSubQueryEntity, PredictorMessage,
  PredictorAlgorithmSymbol,
  PredictorResultSaverSymbol, PredictorSimpleResultSaver,
  NeuralNetworkSettingsEntity, PredictorSettingsEmbedded, PredictorState,
  PredictorMainQueryEmbedded, PredictorColumnUsage, PredictorOperation, PredictSimpleResultEntity, PredictorPublicationSymbol, PredictorEpochProgressEntity, TensorFlowPredictorAlgorithm
} from './Signum.Entities.MachineLearning'
import * as QuickLinks from '@framework/QuickLinks'
import { QueryToken } from '@framework/FindOptions';
import { ImportComponent } from '@framework/AsyncImport';
import { TypeContext } from '@framework/Lines';
import SelectorModal from '@framework/SelectorModal';
import { CellFormatter } from '@framework/Finder';

export function start(options: { routes: JSX.Element[] }) {
  Navigator.addSettings(new EntitySettings(PredictorEntity, e => import('./Templates/Predictor')));
  Navigator.addSettings(new EntitySettings(PredictorSubQueryEntity, e => import('./Templates/PredictorSubQuery')));
  Navigator.addSettings(new EntitySettings(NeuralNetworkSettingsEntity, e => import('./Templates/NeuralNetworkSettings')));
  Navigator.addSettings(new EntitySettings(PredictSimpleResultEntity, e => import('./Templates/PredictSimpleResult')));

  function numbericCellFormatter(color: string) {
    var numberFormat = toNumberFormat("0.000");
    return new CellFormatter((cell: number) => cell == undefined ? "" : <span style={{ color: color }}>{numberFormat.format(cell)}</span>, false, "numeric-cell");
  }

  Finder.registerPropertyFormatter(PredictorEpochProgressEntity.tryPropertyRoute(a => a.lossTraining), numbericCellFormatter("#1A5276"));
  Finder.registerPropertyFormatter(PredictorEpochProgressEntity.tryPropertyRoute(a => a.accuracyTraining), numbericCellFormatter("#5DADE2"));
  Finder.registerPropertyFormatter(PredictorEpochProgressEntity.tryPropertyRoute(a => a.lossValidation), numbericCellFormatter("#7B241C"));
  Finder.registerPropertyFormatter(PredictorEpochProgressEntity.tryPropertyRoute(a => a.accuracyValidation), numbericCellFormatter("#D98880"));

  QuickLinks.registerQuickLink(PredictorEntity, PredictorMessage.DownloadCsv.name, ctx => new QuickLinks.QuickLinkAction(
    PredictorMessage.DownloadCsv.name,
    () => PredictorMessage.DownloadCsv.niceToString(),
    e => API.downloadCsvById(ctx.lite)), { text: () => PredictorMessage.DownloadCsv.niceToString() });

  QuickLinks.registerQuickLink(PredictorEntity, PredictorMessage.DownloadTsv.name, ctx => new QuickLinks.QuickLinkAction(
    PredictorMessage.DownloadTsv.name,
    () => PredictorMessage.DownloadTsv.niceToString(),
    e => API.downloadTsvById(ctx.lite)), { text: () => PredictorMessage.DownloadTsv.niceToString() });

  QuickLinks.registerQuickLink(PredictorEntity, PredictorMessage.DownloadTsvMetadata.name, ctx => new QuickLinks.QuickLinkAction(
    PredictorMessage.DownloadTsvMetadata.name,
    () => PredictorMessage.DownloadTsvMetadata.niceToString(),
    e => API.downloadTsvMetadataById(ctx.lite)));

/*  QuickLinks.registerQuickLink(PredictorEntity, ctx => new QuickLinks.QuickLinkAction(
    PredictorMessage.OpenTensorflowProjector.name,
    () => PredictorMessage.OpenTensorflowProjector.niceToString(),
    e => window.open("http://projector.tensorflow.org/", "_blank")));*/

  Operations.addSettings(new EntityOperationSettings(PredictorOperation.StopTraining, { hideOnCanExecute: true }));
  Operations.addSettings(new EntityOperationSettings(PredictorOperation.CancelTraining, { hideOnCanExecute: true }));
  Operations.addSettings(new EntityOperationSettings(PredictorOperation.Train, { hideOnCanExecute: true }));
  Operations.addSettings(new EntityOperationSettings(PredictorOperation.Untrain, {
    hideOnCanExecute: true,
    confirmMessage: ctx => ctx.entity.publication && PredictorMessage.PredictorIsPublishedUntrainAnyway.niceToString(),
  }));

  Operations.addSettings(new EntityOperationSettings(PredictorOperation.Publish, {
    hideOnCanExecute: true,
    commonOnClick: oc => oc.getEntity()
      .then(p => API.publications(p.mainQuery.query!.key))
      .then(pubs => SelectorModal.chooseElement(pubs, { buttonDisplay: a => symbolNiceName(a), buttonName: a => a.key }))
      .then(pps => pps && oc.defaultClick(pps)),
  }));

  Operations.addSettings(new EntityOperationSettings(PredictorOperation.AfterPublishProcess, {
    hideOnCanExecute: true,
    group: null,
  }));

  Constructor.registerConstructor(PredictorEntity, () => PredictorEntity.New({
    mainQuery: PredictorMainQueryEmbedded.New(),
    settings: PredictorSettingsEmbedded.New(),
  }));

  registerInitializer(TensorFlowPredictorAlgorithm.NeuralNetworkGraph, a => a.algorithmSettings = NeuralNetworkSettingsEntity.New({
    predictionType: "Regression",
    lossFunction: "MeanSquaredError",
    evalErrorFunction: "MeanAbsoluteError",

    optimizer: "Adam",

    learningRate: 0.01,

    minibatchSize: 1000,
    numMinibatches: 100,

    saveProgressEvery: 5,
    saveValidationProgressEvery: 10,
  }));

  registerResultRenderer(PredictorSimpleResultSaver.Full, ctx =>
    <ImportComponent onImportModule={() => import("./Templates/SimpleResultButton")} componentProps={{ ctx: ctx }} />
  );
}

export async function predict(predictor: PredictorEntity, mainKeys: { [queryToken: string]: any } | undefined): Promise<void> {
  var predictRequest = await API.getPredict(toLite(predictor), mainKeys);

  var modal = await import("./Templates/PredictModal");

  var isClassification = NeuralNetworkSettingsEntity.isInstance(predictor.algorithmSettings) && predictor.algorithmSettings.predictionType == "Classification";

  return modal.PredictModal.show(predictRequest, mainKeys && mainKeys["Entity"], isClassification);
}

export const initializers: { [key: string]: (pred: PredictorEntity) => void } = {};
export function registerInitializer(symbol: PredictorAlgorithmSymbol, initialize: (predictor: PredictorEntity) => void) {
  initializers[symbol.key] = initialize;
}

export const resultRenderers: { [key: string]: (ctx: TypeContext<PredictorEntity>) => React.ReactNode } = {};
export function registerResultRenderer(symbol: PredictorResultSaverSymbol, renderer: (ctx: TypeContext<PredictorEntity>) => React.ReactNode) {
  resultRenderers[symbol.key] = renderer;
}

export function getResultRendered(ctx: TypeContext<PredictorEntity>): React.ReactNode {
  if (!ctx.value.resultSaver)
    return null;

  var rr = resultRenderers[ctx.value.resultSaver.key];
  if (rr == null)
    return null;

  return rr(ctx);
}

export namespace API {

  export function downloadCsvById(lite: Lite<PredictorEntity>): void {
    ajaxGetRaw({ url: `~/api/predictor/csv/${lite.id}` })
      .then(response => saveFile(response));
  }

  export function downloadTsvById(lite: Lite<PredictorEntity>): void {
    ajaxGetRaw({ url: `~/api/predictor/tsv/${lite.id}` })
      .then(response => saveFile(response));
  }

  export function downloadTsvMetadataById(lite: Lite<PredictorEntity>): void {
    ajaxGetRaw({ url: `~/api/predictor/tsv/${lite.id}/metadata` })
      .then(response => saveFile(response));
  }

  export function getTrainingState(lite: Lite<PredictorEntity>): Promise<TrainingProgress> {
    return ajaxGet<TrainingProgress>({ url: `~/api/predictor/trainingProgress/${lite.id}` }).then(tp => {
      if (tp.epochProgresses)
        tp.epochProgressesParsed = tp.epochProgresses.map(p => fromObjectArray(p));
      return tp;
    });
  }

  export function getEpochLosses(lite: Lite<PredictorEntity>): Promise<Array<EpochProgress>> {
    return ajaxGet<Array<(number | undefined)[]>>({ url: `~/api/predictor/epochProgress/${lite.id}` }).then(ps => ps.map(p => fromObjectArray(p)));
  }

  export function getPredict(predictor: Lite<PredictorEntity>, mainKeys: { [queryToken: string]: any } | undefined): Promise<PredictRequest> {
    return ajaxPost({ url: `~/api/predict/get/${predictor.id}` }, mainKeys);
  }

  export function updatePredict(predict: PredictRequest): Promise<PredictRequest> {
    return ajaxPost({ url: `~/api/predict/update/` }, predict);
  }

  export function publications(queryKey: string): Promise<PredictorPublicationSymbol[]> {
    return ajaxGet({ url: `~/api/predict/publications/${queryKey}` });
  }
}

export interface TrainingProgress {
  message?: string;
  progress?: number;
  state: PredictorState;
  running: boolean;

  epochProgresses?: Array<(number | undefined)[]>;
  epochProgressesParsed?: Array<EpochProgress>;
}

export interface EpochProgress {
  ellapsed: number;
  trainingExamples: number;
  epoch: number;
  lossTraining: number;
  accuracyTraining: number;
  lossValidation?: number;
  accuracyValidation?: number;
}

function fromObjectArray(array: (number | undefined)[]): EpochProgress {
  return {
    ellapsed: array[0]!,
    trainingExamples: array[1]!,
    epoch: array[2]!,
    lossTraining: array[3]!,
    accuracyTraining: array[4]!,
    lossValidation: array[5],
    accuracyValidation: array[6],
  }
}

export interface PredictRequest {
  hasOriginal: boolean;
  alternativesCount: number | null;
  predictor: Lite<PredictorEntity>;
  columns: PredictColumn[]
  subQueries: PredictSubQueryTable[]
}

export interface PredictColumn {
  token: QueryToken;
  usage: PredictorColumnUsage;
  value: object;
}

export interface AlternativePrediction {
  probability: number;
  value: any;
}

export interface PredictSubQueryTable {
  subQuery: Lite<PredictorSubQueryEntity>;
  columnHeaders: PredictSubQueryHeader[];
  rows: Array<object[]>;
}

export interface PredictOutputTuple {
  predicted: any;
  original: any;
}

export interface PredictSubQueryHeader {
  token: QueryToken;
  headerType: PredictorHeaderType
}

export type PredictorHeaderType = "Key" | "Input" | "Output";
