import * as React from 'react'
import { Route } from 'react-router'
import { Dic, classes } from '../../../Framework/Signum.React/Scripts/Globals';
import { Button, OverlayTrigger, Tooltip, MenuItem } from "react-bootstrap"
import { ajaxPost, ajaxGet } from '../../../Framework/Signum.React/Scripts/Services';
import { EntitySettings, ViewPromise } from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Navigator from '../../../Framework/Signum.React/Scripts/Navigator'
import { Lite, Entity, EntityPack, ExecuteSymbol, DeleteSymbol, ConstructSymbol_From } from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import { EntityOperationSettings } from '../../../Framework/Signum.React/Scripts/Operations'
import { PseudoType, QueryKey, GraphExplorer, OperationType  } from '../../../Framework/Signum.React/Scripts/Reflection'
import * as Operations from '../../../Framework/Signum.React/Scripts/Operations'
import * as ContextualOperations from '../../../Framework/Signum.React/Scripts/Operations/ContextualOperations'
import { PrintLineEntity, PrintLineState, PrintPackageEntity, PrintPermission, PrintPackageProcess } from './Signum.Entities.Printing'
import * as OmniboxClient from '../Omnibox/OmniboxClient'
import * as AuthClient from '../Authorization/AuthClient'

require("!style!css!./Printing.css");

export function start(options: { routes: JSX.Element[],}) {
  
    //Navigator.addSettings(new EntitySettings(ProcessEntity, e => new ViewPromise(resolve => require(['./Templates/Process'], resolve))));

    options.routes.push(<Route path="printing">
        <Route path="view" getComponent={(loc, cb) => require(["./printingPanelPage"], (Comp) => cb(undefined, Comp.default))} />
    </Route>);
    
    OmniboxClient.registerSpecialAction({
        allowed: () => AuthClient.isPermissionAuthorized(PrintPermission.ViewPrintPanel),
        key: "PrintPanel",
        onClick: () => Promise.resolve(Navigator.currentHistory.createHref("~/printing/view"))
    });

}

export module API {


    export function start(): Promise<void> {
        return ajaxPost<void>({ url: "~/api/printing/start" }, undefined);
    }
}