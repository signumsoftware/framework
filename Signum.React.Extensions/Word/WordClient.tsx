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
import { PseudoType, QueryKey, GraphExplorer, OperationType, Type, getTypeName  } from '../../../Framework/Signum.React/Scripts/Reflection'
import * as Operations from '../../../Framework/Signum.React/Scripts/Operations'
import * as ContextualOperations from '../../../Framework/Signum.React/Scripts/Operations/ContextualOperations'
import { WordTemplateEntity, WordTemplateOperation, WordTemplateTableSourceEntity } from './Signum.Entities.Word'
import * as OmniboxClient from '../Omnibox/OmniboxClient'
import * as AuthClient from '../Authorization/AuthClient'
import * as QuickLinks from '../../../Framework/Signum.React/Scripts/QuickLinks'

export function start(options: { routes: JSX.Element[] }) {
    Navigator.addSettings(new EntitySettings(WordTemplateEntity, e => new ViewPromise(resolve => require(['./Templates/WordTemplate'], resolve))));
    Navigator.addSettings(new EntitySettings(WordTemplateTableSourceEntity, e => new ViewPromise(resolve => require(['./Templates/WordTemplateTableSource'], resolve))));

    Operations.addSettings(new EntityOperationSettings(WordTemplateOperation.CreateWordReport, {
        onClick: ctx => {
            Finder.find({ queryName: ctx.entity.query!.key }).then(lite => {
                if (!lite)
                    return;

                ajaxPostRaw({ url: "~/api/word/createReport" }, { template: toLite(ctx.entity), entity: lite })
                    .then(response => saveFile(response))
                    .done();
            }).done();
        }
    }));
}