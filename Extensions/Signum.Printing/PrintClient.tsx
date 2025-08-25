import * as React from 'react'
import { RouteObject } from 'react-router'
import { ajaxPost, ajaxGet } from '@framework/Services';
import { Navigator, EntitySettings } from '@framework/Navigator'
import { Operations, EntityOperationSettings } from '@framework/Operations'
import { PrintLineEntity, PrintPackageEntity, PrintPermission, PrintLineOperation } from './Signum.Printing'
import { ProcessEntity } from '../Signum.Processes/Signum.Processes'
import { FileTypeSymbol } from '../Signum.Files/Signum.Files'
import * as OmniboxSpecialAction from '@framework/OmniboxSpecialAction'
import { AuthClient } from '../Signum.Authorization/AuthClient'
import { ImportComponent } from '@framework/ImportComponent'
import { isPermissionAuthorized } from '@framework/AppContext';

export namespace PrintClient {
  
  export function start(options: { routes: RouteObject[], }): void {
    Navigator.addSettings(new EntitySettings(PrintLineEntity, e => import('./Templates/PrintLine'), { isCreable: "IsSearch" }));
    Navigator.addSettings(new EntitySettings(PrintPackageEntity, e => import('./Templates/PrintPackage')));
  
    options.routes.push({ path: "/printing/view", element: <ImportComponent onImport={() => import("./PrintPanelPage")} /> });
  
    Operations.addSettings(new EntityOperationSettings(PrintLineOperation.SaveTest, { hideOnCanExecute: true }));
  
    OmniboxSpecialAction.registerSpecialAction({
      allowed: () => isPermissionAuthorized(PrintPermission.ViewPrintPanel),
      key: "PrintPanel",
      onClick: () => Promise.resolve("/printing/view")
    });
  }
  
  export namespace API {
    export function getStats(): Promise<PrintStat[]> {
      return ajaxGet({ url: `/api/printing/stats` });
    }
  
    export function createPrintProcess(fileType: FileTypeSymbol): Promise<ProcessEntity> {
      return ajaxPost({ url: `/api/printing/createProcess` }, fileType);
    }
  }
  
  export interface PrintStat {
    fileType: FileTypeSymbol;
    count: number;
  }
}
