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
import { WordTemplateEntity, WordTemplateOperation, SystemWordTemplateEntity, MultiEntityModel, WordTemplatePermission } from './Signum.Entities.Word'
import * as OmniboxClient from '../Omnibox/OmniboxClient'
import * as AuthClient from '../Authorization/AuthClient'
import * as QuickLinks from '../../../Framework/Signum.React/Scripts/QuickLinks'
import * as ContexualItems from '../../../Framework/Signum.React/Scripts/SearchControl/ContextualItems'
import { ContextualItemsContext, MenuItemBlock } from "../../../Framework/Signum.React/Scripts/SearchControl/ContextualItems";

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
                .then<Response | undefined>(ct => {
                    if (!ct)
                        return undefined;

                    var template = toLite(ctx.entity);

                    if (isTypeEntity(ct))
                        return getQueryType().then(ti => ti && Finder.find({ queryName: ti.name })).then<Response | undefined>(lite => lite && API.createAndDownloadReport({ template, lite }));
                    else if (MultiEntityModel.typeName == ct)
                        return getQueryType().then(ti => ti && Finder.findMany({ queryName: ti.name })).then<Response | undefined>(lites => lites && API.createAndDownloadReport({ template, entity: MultiEntityModel.New({ entities: toMList(lites) }) }));
                    else
                        return Constructor.construct(ct).then(e => e && Navigator.view(e)).then<Response | undefined>(entity => entity && API.createAndDownloadReport({ template, entity }));
                })
                .then(response => {
                    if (!response)
                        return;

                    saveFile(response);
                }).done();
        }
    }));

    ContexualItems.onContextualItems.push(getWordTemplates);
}

export function getWordTemplates(ctx: ContextualItemsContext<Entity>): Promise<MenuItemBlock | undefined> | undefined {

    if (ctx.lites.length == 0)
        return undefined;

    const types = ctx.lites.groupBy(lite => lite.EntityType);

    if (types.length != 1)
        return undefined;

    return API.getWordTemplates(types[0].key, ctx.lites.length > 1)
        .then(wts => {
            if (!wts.length)
                return undefined;

            return {
                header: WordTemplateEntity.nicePluralName(),
                menuItems: wts.map(wt =>
                    <MenuItem data-operation={wt.EntityType} onClick={() => handleMenuClick(wt, ctx.lites)}>
                        <span className={classes("icon", "fa fa-file-word-o")}></span>
                        {wt.toStr}
                    </MenuItem>
                )
            } as MenuItemBlock;
        });
}

export function handleMenuClick(wt: Lite<WordTemplateEntity>, lites: Lite<Entity>[]) {

    Navigator.API.fetchAndForget(wt)
        .then(wordTemplate => wordTemplate.systemWordTemplate ? API.getConstructorType(wordTemplate.systemWordTemplate) : Promise.resolve(undefined))
        .then<Response>(ct => ct == MultiEntityModel.typeName ?
            API.createAndDownloadReport({ template: wt, entity: MultiEntityModel.New({ entities: toMList(lites) }) }) :
            API.createAndDownloadReport({ template: wt, lite: lites.single() }))
        .then(response => saveFile(response))
        .done();
}

export namespace API {

    export interface CreateWordReportRequest {
        template: Lite<WordTemplateEntity>;
        lite?: Lite<Entity>;
        entity?: ModifiableEntity;
    }

    export function createAndDownloadReport(request: CreateWordReportRequest): Promise<Response> {
        return ajaxPostRaw({ url: "~/api/word/createReport" }, request);
    }

    export function getConstructorType(systemWordTemplate: SystemWordTemplateEntity): Promise<string> {
        return ajaxPost<string>({ url: "~/api/word/constructorType" }, systemWordTemplate);
    }

    export function getWordTemplates(typeName: string, isMultiple: boolean): Promise<Lite<WordTemplateEntity>[]> {
        return ajaxGet<Lite<WordTemplateEntity>[]>({ url: `~/api/word/wordTemplates?typeName=${typeName}&isMultiple=${isMultiple}` });
    }
}