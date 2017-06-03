import * as React from 'react'
import { Route } from 'react-router'
import { Dic, classes } from '../../../Framework/Signum.React/Scripts/Globals';
import { Button, OverlayTrigger, Tooltip, MenuItem } from "react-bootstrap"
import { ajaxPost, ajaxPostRaw, ajaxGet, saveFile } from '../../../Framework/Signum.React/Scripts/Services';
import { EntitySettings, ViewPromise } from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Navigator from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Finder from '../../../Framework/Signum.React/Scripts/Finder'
import { Lite, Entity, EntityPack, ExecuteSymbol, DeleteSymbol, ConstructSymbol_From, registerToString, JavascriptMessage, toLite, ModifiableEntity, toMList } from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import { EntityOperationSettings } from '../../../Framework/Signum.React/Scripts/Operations'
import { PseudoType, QueryKey, GraphExplorer, OperationType, Type, getTypeName, isTypeEntity, getTypeInfos } from '../../../Framework/Signum.React/Scripts/Reflection'
import * as Operations from '../../../Framework/Signum.React/Scripts/Operations'
import SelectorModal from '../../../Framework/Signum.React/Scripts/SelectorModal'
import * as Constructor from '../../../Framework/Signum.React/Scripts/Constructor'
import { WordTemplateEntity, WordTemplateOperation, SystemWordTemplateEntity, MultiEntityModel } from './Signum.Entities.Word'
import * as OmniboxClient from '../Omnibox/OmniboxClient'
import * as AuthClient from '../Authorization/AuthClient'
import * as QuickLinks from '../../../Framework/Signum.React/Scripts/QuickLinks'

export function start(options: { routes: JSX.Element[] }) {
    Navigator.addSettings(new EntitySettings(WordTemplateEntity, e => _import('./Templates/WordTemplate')));

    Operations.addSettings(new EntityOperationSettings(WordTemplateOperation.CreateWordReport, {
        onClick: ctx => {

            function getQueryType() {
                return Finder.getQueryDescription(ctx.entity.query!.key)
                    .then(a => SelectorModal.chooseType(getTypeInfos(a.columns["Entity"].type.name)));
            }


            var promise: Promise<string | undefined> = ctx.entity.systemWordTemplate ? API.getConstructorType(ctx.entity.systemWordTemplate) : Promise.resolve(undefined);
            promise
                .then<ModifiableEntity | undefined>(ct => {
                    if (!ct)
                        return undefined;

                    if (isTypeEntity(ct))
                        return getQueryType().then(ti => ti && Finder.find({ queryName: ti.name })).then<Entity | undefined>(lite => lite && Navigator.API.fetchAndForget<Entity>(lite));
                    else if (MultiEntityModel.typeName == ct)
                        return getQueryType().then(ti => ti && Finder.findMany({ queryName: ti.name })).then(lites => lites && MultiEntityModel.New({ entities: toMList(lites) }));
                    else
                        return Constructor.construct(ct).then(e => e && Navigator.view(e));
                })
                .then(mod => {
                    if (!mod)
                        return;

                    API.createAndDownloadReport(toLite(ctx.entity), mod)
                        .then(response => saveFile(response))
                        .done();
                }).done();
        }
    }));

    
}

export namespace API {
    export function createAndDownloadReport(template: Lite<WordTemplateEntity>, entity: ModifiableEntity): Promise<Response> {
        return ajaxPostRaw({ url: "~/api/word/createReport" }, { template, entity });
    }

    export function getConstructorType(systemWordTemplate: SystemWordTemplateEntity): Promise<string> {
        return ajaxPost<string>({ url: "~/api/word/constructorType" }, systemWordTemplate);
    }
}