import * as React from 'react'
import { Route } from 'react-router'
import { Dic, classes } from '../../../Framework/Signum.React/Scripts/Globals';
import { Button, OverlayTrigger, Tooltip, MenuItem } from "react-bootstrap"
import { ajaxPost, ajaxPostRaw, ajaxGet, saveFile } from '../../../Framework/Signum.React/Scripts/Services';
import { EntitySettings, ViewPromise } from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Navigator from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Finder from '../../../Framework/Signum.React/Scripts/Finder'
import { Lite, Entity, EntityPack, ExecuteSymbol, DeleteSymbol, ConstructSymbol_From, registerToString, JavascriptMessage, toLite } from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import { EntityOperationSettings } from '../../../Framework/Signum.React/Scripts/Operations'
import { PseudoType, QueryKey, GraphExplorer, OperationType, Type, getTypeName } from '../../../Framework/Signum.React/Scripts/Reflection'
import * as Operations from '../../../Framework/Signum.React/Scripts/Operations'
import * as ContextualOperations from '../../../Framework/Signum.React/Scripts/Operations/ContextualOperations'
import { AlertEntity, AlertTypeEntity, AlertOperation } from './Signum.Entities.Alerts'
import * as OmniboxClient from '../Omnibox/OmniboxClient'
import * as AuthClient from '../Authorization/AuthClient'
import * as QuickLinks from '../../../Framework/Signum.React/Scripts/QuickLinks'

export function start(options: { routes: JSX.Element[], creatingJustFromCertainTypes: PseudoType[] | undefined  }) {
    Navigator.addSettings(new EntitySettings(AlertEntity, e => new ViewPromise(resolve => require(['./Templates/Alert'], resolve))));
    Navigator.addSettings(new EntitySettings(AlertTypeEntity, e => new ViewPromise(resolve => require(['./Templates/AlertType'], resolve))));

    Operations.addSettings(new EntityOperationSettings(AlertOperation.SaveNew, {
        isVisible: eoc => eoc.entity.isNew
    }));

    if (options.creatingJustFromCertainTypes) {
        Operations.addSettings(new EntityOperationSettings(AlertOperation.CreateAlertFromEntity, {
            isVisible: eoc => options.creatingJustFromCertainTypes!.contains(eoc.entity.Type)
        }));
    }
}
