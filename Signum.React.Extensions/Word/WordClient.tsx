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
import { WordTemplateEntity, WordTemplateOperation, SystemWordTemplateEntity, MultiEntityModel, WordTemplatePermission, QueryModel, WordTemplateVisibleOn } from './Signum.Entities.Word'
import * as OmniboxClient from '../Omnibox/OmniboxClient'
import * as AuthClient from '../Authorization/AuthClient'
import * as QuickLinks from '../../../Framework/Signum.React/Scripts/QuickLinks'
import * as ContexualItems from '../../../Framework/Signum.React/Scripts/SearchControl/ContextualItems'
import { ContextualItemsContext, MenuItemBlock } from "../../../Framework/Signum.React/Scripts/SearchControl/ContextualItems";
import { ModelEntity } from "../../../Framework/Signum.React/Scripts/Signum.Entities";
import { QueryRequest, FilterRequest } from "../../../Framework/Signum.React/Scripts/FindOptions";
import WordMenu from "./WordMenu";


export const constructorForTesting: { [typeName: string]: (wordTemplate: WordTemplateEntity) => Promise<ModelEntity | undefined>; } = {};
export const constructorContextual: { [typeName: string]: (wt: Lite<WordTemplateEntity>, lites?: Array<Lite<Entity>>, req?: QueryRequest) => Promise<ModelEntity | undefined>; } = {};

export function start(options: { routes: JSX.Element[] }) {

    constructorForTesting[QueryModel.typeName] = wt => Navigator.view(QueryModel.New({ queryKey: wt.query!.key }));
    constructorContextual[QueryModel.typeName] = (wt, lites, req) => {
        if (req) {
            return Promise.resolve(QueryModel.New({
                queryKey: req.queryKey,
                filters: [...req.filters, ...(!lites ? [] : [{ token: "Entity", operation: "IsIn", value: lites } as FilterRequest])],
                orders: req.orders,
                pagination: req.pagination,
            }));
        } else {
            return Navigator.API.fetchAndForget(wt).then(template => QueryModel.New({
                queryKey: template.query!.key,
                filters: [{ token: "Entity", operation: "IsIn", value: lites }],
                orders: [],
                pagination: { mode: "All" },
            }));
        }
    };

    constructorForTesting[MultiEntityModel.typeName] = wt =>
        Finder.findMany({ queryName: wt.query!.key })
        .then(lites => lites && MultiEntityModel.New({ entities: toMList(lites) }));

    constructorContextual[MultiEntityModel.typeName] = (wt, lites, req) => Navigator.view(MultiEntityModel.New({ entities: toMList(lites!) }));

    Navigator.addSettings(new EntitySettings(WordTemplateEntity, e => _import('./Templates/WordTemplate')));
    Navigator.addSettings(new EntitySettings(QueryModel, e => _import('./Templates/QueryModel')));

    Operations.addSettings(new EntityOperationSettings(WordTemplateOperation.CreateWordReport, {
        onClick: ctx => {

            var promise: Promise<string | undefined> = ctx.entity.systemWordTemplate ? API.getConstructorType(ctx.entity.systemWordTemplate) : Promise.resolve(undefined);
            promise
                .then<Response | undefined>(ct => {
                    var template = toLite(ctx.entity);

                    if (!ct || isTypeEntity(ct))
                        return Finder.find({ queryName: ctx.entity.query!.key })
                            .then<Response | undefined>(lite => lite && API.createAndDownloadReport({ template, lite }));
                    else
                        return (constructorForTesting[ct] && constructorForTesting[ct](ctx.entity) || Constructor.construct(ct))
                            .then<Response | undefined>(entity => entity && API.createAndDownloadReport({ template, entity }));
                })
                .then(response => {
                    if (!response)
                        return;

                    saveFile(response);
                }).done();
        }
    }));

    ContexualItems.onContextualItems.push(getWordTemplates);

    Finder.ButtonBarQuery.onButtonBarElements.push(ctx => {

        if (!ctx.searchControl.props.showBarExtension)
            return undefined;

        return <WordMenu searchControl={ctx.searchControl} />;
    }); 
}

export function getWordTemplates(ctx: ContextualItemsContext<Entity>): Promise<MenuItemBlock | undefined> | undefined {

    if (ctx.lites.length == 0)
        return undefined;
    
    return API.getWordTemplates(ctx.queryDescription.queryKey, ctx.lites.length > 1 ? "Multiple" : "Single")
        .then(wts => {
            if (!wts.length)
                return undefined;

            return {
                header: WordTemplateEntity.nicePluralName(),
                menuItems: wts.map(wt =>
                    <MenuItem data-operation={wt.EntityType} onClick={() => handleMenuClick(wt, ctx)}>
                        <span className={classes("icon", "fa fa-file-word-o")}></span>
                        {wt.toStr}
                    </MenuItem>
                )
            } as MenuItemBlock;
        });
}

export function handleMenuClick(wt: Lite<WordTemplateEntity>, ctx: ContextualItemsContext<Entity>) {

    Navigator.API.fetchAndForget(wt)
        .then(wordTemplate => wordTemplate.systemWordTemplate ? API.getConstructorType(wordTemplate.systemWordTemplate) : Promise.resolve(undefined))
        .then(ct => {
            if (!ct)
                return API.createAndDownloadReport({ template: wt, lite: ctx.lites.single() });

            const constructor = constructorContextual[ct];
            if (!constructor)
                throw new Error("No 'constructorContextual' defined for '" + ct + "'");

            return constructorContextual[ct](wt, ctx.lites, ctx.searchControl.getQueryRequest())
                .then<Response | undefined>(m => m && API.createAndDownloadReport({ template: wt, entity: m }));
        })
        .then(response => response && saveFile(response))
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

    export function getWordTemplates(queryKey: string, visibleOn: WordTemplateVisibleOn): Promise<Lite<WordTemplateEntity>[]> {
        return ajaxGet<Lite<WordTemplateEntity>[]>({ url: `~/api/word/wordTemplates?queryKey=${queryKey}&visibleOn=${visibleOn}` });
    }
}