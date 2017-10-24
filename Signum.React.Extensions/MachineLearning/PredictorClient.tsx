import * as React from 'react'
import { Route } from 'react-router'
import { Dic, classes } from '../../../Framework/Signum.React/Scripts/Globals';
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
import { PredictorEntity, PredictorMultiColumnEntity, PredictorMessage } from './Signum.Entities.MachineLearning'
import * as OmniboxClient from '../Omnibox/OmniboxClient'
import * as AuthClient from '../Authorization/AuthClient'
import * as ChartClient from '../Chart/ChartClient'
import { ChartPermission } from '../Chart/Signum.Entities.Chart'
import * as QuickLinks from '../../../Framework/Signum.React/Scripts/QuickLinks'
import { ChartRequest  } from '../Chart/Signum.Entities.Chart'

export function start(options: { routes: JSX.Element[] }) {

    Navigator.addSettings(new EntitySettings(PredictorEntity, e => import('./Templates/Predictor')));
    Navigator.addSettings(new EntitySettings(PredictorMultiColumnEntity, e => import('./Templates/PredictorMultiColumn')));

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

}

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

}