import * as React from 'react'
import { Route } from 'react-router'
import { Dic, classes } from '../../../Framework/Signum.React/Scripts/Globals';
import * as Constructor from '../../../Framework/Signum.React/Scripts/Constructor';
import { ajaxPost, ajaxPostRaw, ajaxGet, saveFile, ajaxGetRaw } from '../../../Framework/Signum.React/Scripts/Services';
import { EntitySettings, ViewPromise } from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Navigator from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Finder from '../../../Framework/Signum.React/Scripts/Finder'
import { QueryRequest } from '../../../Framework/Signum.React/Scripts/FindOptions'
import { Lite, Entity, EntityPack, ExecuteSymbol, DeleteSymbol, ConstructSymbol_From, registerToString, JavascriptMessage, toLite } from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import { EntityOperationSettings } from '../../../Framework/Signum.React/Scripts/Operations'
import { PseudoType, QueryKey, GraphExplorer, OperationType, Type, getTypeName } from '../../../Framework/Signum.React/Scripts/Reflection'
import * as Operations from '../../../Framework/Signum.React/Scripts/Operations'
import * as ContextualOperations from '../../../Framework/Signum.React/Scripts/Operations/ContextualOperations'
import {
    PredictorEntity, PredictorSubQueryEntity, PredictorMessage,
    PredictorAlgorithmSymbol, AccordPredictorAlgorithm, CNTKPredictorAlgorithm,
    PredictorResultSaverSymbol, PredictorSimpleResultSaver,
    NaiveBayesSettingsEntity, NeuralNetworkSettingsEntity, PredictorSettingsEmbedded, PredictorState, PredictorRegressionMetricsEmbedded,
    PredictorClassificationMetricsEmbedded, PredictorMainQueryEmbedded, PredictorColumnUsage, PredictorOperation, PredictSimpleResultEntity, PredictorPublicationSymbol
} from './Signum.Entities.MachineLearning'
import * as OmniboxClient from '../Omnibox/OmniboxClient'

import * as QuickLinks from '../../../Framework/Signum.React/Scripts/QuickLinks'
import { QueryToken } from '../../../Framework/Signum.React/Scripts/FindOptions';
import { ImportComponent } from '../../../Framework/Signum.React/Scripts/AsyncImport';
import { TypeContext } from '../../../Framework/Signum.React/Scripts/Lines';
import SelectorModal from '../../../Framework/Signum.React/Scripts/SelectorModal';

export function start(options: { routes: JSX.Element[] }) {

    Navigator.addSettings(new EntitySettings(PredictorEntity, e => import('./Templates/Predictor')));
    Navigator.addSettings(new EntitySettings(PredictorSubQueryEntity, e => import('./Templates/PredictorSubQuery')));
    Navigator.addSettings(new EntitySettings(NeuralNetworkSettingsEntity, e => import('./Templates/NeuralNetworkSettings')));
    Navigator.addSettings(new EntitySettings(PredictSimpleResultEntity, e => import('./Templates/PredictSimpleResult')));

    QuickLinks.registerQuickLink(PredictorEntity, ctx => new QuickLinks.QuickLinkAction(
        PredictorMessage.DownloadCsv.niceToString(),
        PredictorMessage.DownloadCsv.niceToString(),
        e => API.downloadCsvById(ctx.lite)));

    QuickLinks.registerQuickLink(PredictorEntity, ctx => new QuickLinks.QuickLinkAction(
        PredictorMessage.DownloadTsv.niceToString(),
        PredictorMessage.DownloadTsv.niceToString(),
        e => API.downloadTsvById(ctx.lite)));

    QuickLinks.registerQuickLink(PredictorEntity, ctx => new QuickLinks.QuickLinkAction(
        PredictorMessage.DownloadTsvMetadata.niceToString(),
        PredictorMessage.DownloadTsvMetadata.niceToString(),
        e => API.downloadTsvMetadataById(ctx.lite)));

    QuickLinks.registerQuickLink(PredictorEntity, ctx => new QuickLinks.QuickLinkAction(
        PredictorMessage.OpenTensorflowProjector.niceToString(),
        PredictorMessage.OpenTensorflowProjector.niceToString(),
        e => window.open("http://projector.tensorflow.org/", "_blank")));

    Operations.addSettings(new EntityOperationSettings(PredictorOperation.StopTraining, { hideOnCanExecute: true }));
    Operations.addSettings(new EntityOperationSettings(PredictorOperation.CancelTraining, { hideOnCanExecute: true }));
    Operations.addSettings(new EntityOperationSettings(PredictorOperation.Train, { hideOnCanExecute: true }));
    Operations.addSettings(new EntityOperationSettings(PredictorOperation.Untrain, { hideOnCanExecute: true }));
    
    Operations.addSettings(new EntityOperationSettings(PredictorOperation.Publish, {
        hideOnCanExecute: true,
        onClick: eoc => {
            API.publications(eoc.entity.mainQuery.query!.key)
                .then(pubs => SelectorModal.chooseElement(pubs))
                .then(pps => pps && eoc.defaultClick(pps))
                .done();
        },
        contextual: {
            onClick: coc => {
                Navigator.API.fetchAndForget(coc.context.lites[0])
                    .then(p => API.publications(p.mainQuery.query!.key))
                    .then(pubs => SelectorModal.chooseElement(pubs))
                    .then(pps => pps && coc.defaultContextualClick(pps))
                    .done();
            }
        }
    }));

    Constructor.registerConstructor(PredictorEntity, () => PredictorEntity.New({
        mainQuery: PredictorMainQueryEmbedded.New(),
        settings: PredictorSettingsEmbedded.New(),
    }));

    registerInitializer(AccordPredictorAlgorithm.DiscreteNaiveBayes, a => a.algorithmSettings = NaiveBayesSettingsEntity.New());
    registerInitializer(CNTKPredictorAlgorithm.NeuralNetwork, a => a.algorithmSettings = NeuralNetworkSettingsEntity.New({
        predictionType: "Regression",
        lossFunction: "SquaredError",
        evalErrorFunction: "MeanAbsoluteError",

        learner: "MomentumSGD",

        learningRate: 0.1,
        learningMomentum: 0.01,
        learningUnitGain: false,
        learningVarianceMomentum: 0.001,

        minibatchSize: 1000,
        numMinibatches: 100,

        saveProgressEvery: 5,
        saveValidationProgressEvery: 10,
    }));

    registerResultRenderer(PredictorSimpleResultSaver.Full, ctx =>
        <ImportComponent onImportModule={() => import("./Templates/SimpleResultButton")} componentProps={{ ctx: ctx }} />
    );
}

export async function predict(predictor: Lite<PredictorEntity>, mainKeys: { [queryToken: string]: any } | undefined): Promise<void> {
    var predictRequest = await API.getPredict(predictor, mainKeys);

    var modal = await import("./Templates/PredictModal");

    return modal.PredictModal.show(predictRequest, mainKeys && mainKeys["Entity"]);
}

export const initializers: { [key: string]: (pred: PredictorEntity) => void } = {};
export function registerInitializer(symbol: PredictorAlgorithmSymbol, initialize: (predictor: PredictorEntity) => void) {
    initializers[symbol.key] = initialize;
}

export const resultRenderers: { [key: string]: (ctx: TypeContext<PredictorEntity>) => React.ReactFragment } = {};
export function registerResultRenderer(symbol: PredictorResultSaverSymbol, renderer: (ctx: TypeContext<PredictorEntity>) => React.ReactFragment) {
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

    export function availableDevices(algorithm: PredictorAlgorithmSymbol): Promise<string[]> {
        return ajaxGet<string[]>({ url: `~/api/predictor/availableDevices/${algorithm.key}` });
    }

    export function downloadCsvById(lite: Lite<PredictorEntity>): void {
        ajaxGetRaw({ url: `~/api/predictor/csv/${lite.id}` })
            .then(response => saveFile(response))
            .done();
    }

    export function downloadTsvById(lite: Lite<PredictorEntity>): void {
        ajaxGetRaw({ url: `~/api/predictor/tsv/${lite.id}` })
            .then(response => saveFile(response))
            .done();
    }

    export function downloadTsvMetadataById(lite: Lite<PredictorEntity>): void {
        ajaxGetRaw({ url: `~/api/predictor/tsv/${lite.id}/metadata` })
            .then(response => saveFile(response))
            .done();
    }

    export function getTrainingState(lite: Lite<PredictorEntity>): Promise<TrainingProgress> {
        return ajaxGet<TrainingProgress>({ url: `~/api/predictor/trainingProgress/${lite.id}` }).then(tp => {
            if (tp.EpochProgresses)
                tp.EpochProgressesParsed = tp.EpochProgresses.map(p => fromObjectArray(p));
            return tp;
        });
    }

    export function getEpochLosses(lite: Lite<PredictorEntity>): Promise<Array<EpochProgress>> {
        return ajaxGet<Array<(number | undefined)[]>>({ url: `~/api/predictor/epochProgress/${lite.id}` }).then(ps => ps.map(p => fromObjectArray(p)));
    }

    export function getPredict(predictor: Lite<PredictorEntity>, mainKeys: { [queryToken: string]: any } | undefined): Promise<PredictRequest> {
        return ajaxPost<PredictRequest>({ url: `~/api/predict/get/${predictor.id}` }, mainKeys);
    }

    export function updatePredict(predict: PredictRequest): Promise<PredictRequest> {
        return ajaxPost<PredictRequest>({ url: `~/api/predict/update/` }, predict);
    }

    export function publications(queryKey: string): Promise<PredictorPublicationSymbol[]> {
        return ajaxGet<PredictorPublicationSymbol[]>({ url: `~/api/predict/publications/${queryKey}` });
    }
}

export interface TrainingProgress {
    Message?: string;
    Progress?: number;
    State: PredictorState;
    Running: boolean;

    EpochProgresses?: Array<(number | undefined)[]>;
    EpochProgressesParsed?: Array<EpochProgress>;
}

export interface EpochProgress {
    Ellapsed: number;
    TrainingExamples: number;
    Epoch: number;
    LossTraining: number;
    EvaluationTraining: number;
    LossValidation?: number;
    EvaluationValidation?: number;
}

function fromObjectArray(array: (number | undefined)[]): EpochProgress {
    return {
        Ellapsed: array[0]!,
        TrainingExamples: array[1]!,
        Epoch: array[2]!,
        LossTraining: array[3]!,
        EvaluationTraining: array[4]!,
        LossValidation: array[5],
        EvaluationValidation: array[6],
    }
}

export interface PredictRequest {
    hasOriginal: boolean;
    predictor: Lite<PredictorEntity>;
    columns: PredictColumn[]
    subQueries: PredictSubQueryTable[]
}

export interface PredictColumn {
    token: QueryToken;
    usage: PredictorColumnUsage;
    value: object;
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