import * as React from 'react'
import { Route } from 'react-router'
import { Dic, classes } from '@framework/Globals';
import { ajaxPost, ajaxGet } from '@framework/Services';
import { EntitySettings, ViewPromise } from '@framework/Navigator'
import * as Navigator from '@framework/Navigator'
import { Lite, Entity, EntityPack, ExecuteSymbol, DeleteSymbol, ConstructSymbol_From } from '@framework/Signum.Entities'
import { EntityOperationSettings } from '@framework/Operations'
import { PseudoType, QueryKey, GraphExplorer, OperationType, IType, Type, KindOfType } from '@framework/Reflection'
import * as Operations from '@framework/Operations'
import { PrintLineEntity, PrintLineState, PrintPackageEntity, PrintPermission, PrintPackageProcess, PrintLineOperation } from './Signum.Entities.Printing'
import { ProcessEntity } from '../Processes/Signum.Entities.Processes'
import { FileTypeSymbol } from '../Files/Signum.Entities.Files'
import * as OmniboxClient from '../Omnibox/OmniboxClient'
import * as AuthClient from '../Authorization/AuthClient'
import { ImportRoute } from "@framework/AsyncImport";

export function start(options: { routes: JSX.Element[],}) {

    Navigator.addSettings(new EntitySettings(PrintLineEntity, e => import('./Templates/PrintLine'), { isCreable: "IsSearch" }));
    Navigator.addSettings(new EntitySettings(PrintPackageEntity, e => import('./Templates/PrintPackage')));

    options.routes.push(<ImportRoute path="~/printing/view" onImportModule={() => import("./PrintPanelPage")} />);

    Operations.addSettings(new EntityOperationSettings(PrintLineOperation.SaveTest, { hideOnCanExecute: true }));
    
    OmniboxClient.registerSpecialAction({
        allowed: () => AuthClient.isPermissionAuthorized(PrintPermission.ViewPrintPanel),
        key: "PrintPanel",
        onClick: () => Promise.resolve("~/printing/view")
    });

}

export module API {
    export function getStats(): Promise<PrintStat[]> {
        return ajaxGet<PrintStat[]>({ url: `~/api/printing/stats` });
    }

    export function createPrintProcess(fileType: FileTypeSymbol): Promise<ProcessEntity> {
        return ajaxPost<ProcessEntity>({ url: `~/api/printing/createProcess` }, fileType);
    }
}

export interface PrintStat {
    fileType: FileTypeSymbol;
    count: number;
}