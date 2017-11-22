import * as React from 'react'
import { Route } from 'react-router'
import { Dic, classes } from '../../../Framework/Signum.React/Scripts/Globals';
import * as Constructor from '../../../Framework/Signum.React/Scripts/Constructor';
import { Button, OverlayTrigger, Tooltip, MenuItem } from "react-bootstrap"
import { ajaxPost, ajaxPostRaw, ajaxGet, saveFile, ajaxGetRaw } from '../../../Framework/Signum.React/Scripts/Services';
import { EntitySettings, ViewPromise } from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Navigator from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Finder from '../../../Framework/Signum.React/Scripts/Finder'
import { QueryRequest } from '../../../Framework/Signum.React/Scripts/FindOptions'
import { Lite, Entity, EntityPack, ExecuteSymbol, DeleteSymbol, ConstructSymbol_From, registerToString, JavascriptMessage, toLite } from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import { EntityOperationSettings } from '../../../Framework/Signum.React/Scripts/Operations'
import { PseudoType, QueryKey, GraphExplorer, OperationType, Type, getTypeName  } from '../../../Framework/Signum.React/Scripts/Reflection'
import * as Operations from '../../../Framework/Signum.React/Scripts/Operations'
import * as ContextualOperations from '../../../Framework/Signum.React/Scripts/Operations/ContextualOperations'
import {
    PredictorEntity, PredictorSubQueryEntity, PredictorMessage, PredictorAlgorithmSymbol, AccordPredictorAlgorithm, CNTKPredictorAlgorithm,
    NaiveBayesSettingsEntity, NeuralNetworkSettingsEntity, PredictorSettingsEmbedded, PredictorState, PredictorRegressionMetricsEmbedded,
    PredictorClassificationMetricsEmbedded, PredictorMainQueryEmbedded, PredictorColumnUsage
} from './Signum.Entities.MachineLearning'
import * as OmniboxClient from '../Omnibox/OmniboxClient'
import * as AuthClient from '../Authorization/AuthClient'
import * as ChartClient from '../Chart/ChartClient'
import { ChartPermission } from '../Chart/Signum.Entities.Chart'
import * as QuickLinks from '../../../Framework/Signum.React/Scripts/QuickLinks'
import { ChartRequest  } from '../Chart/Signum.Entities.Chart'
import { QueryToken } from '../../../Framework/Signum.React/Scripts/FindOptions';

export function start(options: { routes: JSX.Element[] }) {

    Navigator.addSettings(new EntitySettings(PredictorEntity, e => import('./Templates/Predictor')));
    Navigator.addSettings(new EntitySettings(PredictorSubQueryEntity, e => import('./Templates/PredictorSubQuery')));
    Navigator.addSettings(new EntitySettings(PredictorRegressionMetricsEmbedded, e => import('./Templates/PredictorRegressionMetrics')));
    Navigator.addSettings(new EntitySettings(PredictorClassificationMetricsEmbedded, e => import('./Templates/PredictorClassificationMetrics')));

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
    
    Constructor.registerConstructor(PredictorEntity, () => PredictorEntity.New({
        mainQuery: PredictorMainQueryEmbedded.New(),
        settings: PredictorSettingsEmbedded.New(),
    }));

    registerInitializer(AccordPredictorAlgorithm.DiscreteNaiveBayes, a => a.algorithmSettings = NaiveBayesSettingsEntity.New());
    registerInitializer(CNTKPredictorAlgorithm.NeuralNetwork, a => a.algorithmSettings = NeuralNetworkSettingsEntity.New());
}

export async function predict(predictor: Lite<PredictorEntity>, entity: Lite<Entity>): Promise<void> {
    var predictRequest = await API.getPredict(predictor, entity);

    var modal = await import("./Templates/PredictModal");

    return modal.PredictModal.show(predictor, predictRequest);

}

export function registerInitializer(symbol: PredictorAlgorithmSymbol, initialize: (predictor: PredictorEntity) => void) {
    initializers[symbol.key] = initialize;
}

export let initializers: { [key: string]: (pred: PredictorEntity) => void } = {};

export namespace API {

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

    export function getPredict(predictor: Lite<PredictorEntity>, entity?: Lite<Entity>): Promise<PredictRequest> {
        return ajaxPost<PredictRequest>({ url: `~/api/predict/get/${predictor.id}` }, entity);
    }

    export function updatePredict(predict: PredictRequest): Promise<PredictRequest> {
        return ajaxPost<PredictRequest>({ url: `~/api/predict/update/` }, predict);
    }
}

export interface TrainingProgress {
    Message?: string;
    Progress?: number;
    State: PredictorState;

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
    subQuery: PredictSubQueryTable[]
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
    original: any;
    predicted: any;
}

export interface PredictSubQueryHeader {
    token: QueryToken;
    headerType: PredictorHeaderType 
}

export type PredictorHeaderType = "Key" | "Input" | "Output";