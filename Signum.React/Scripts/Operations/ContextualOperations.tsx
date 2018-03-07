import * as React from "react"
import { Router, Route, Redirect } from "react-router"
import {
    Lite, Entity, ModifiableEntity, EmbeddedEntity, LiteMessage, EntityPack, toLite, JavascriptMessage,
    OperationSymbol, ConstructSymbol_From, ConstructSymbol_FromMany, ConstructSymbol_Simple, ExecuteSymbol, DeleteSymbol, OperationMessage, getToString, SearchMessage
} from '../Signum.Entities';
import { PropertyRoute, PseudoType, EntityKind, TypeInfo, IType, Type, getTypeInfo, OperationInfo, OperationType, LambdaMemberType } from '../Reflection';
import { classes } from '../Globals';
import * as Navigator from '../Navigator';
import MessageModal from '../Modals/MessageModal'
import Notify from '../Frames/Notify';
import { ContextualItemsContext, MenuItemBlock } from '../SearchControl/ContextualItems';
import { EntityFrame } from '../TypeContext';
import { Dic } from '../Globals';
import { ajaxPost, ValidationError } from '../Services';
import {
    operationInfos, getSettings, ContextualOperationSettings, ContextualOperationContext, EntityOperationSettings, EntityOperationContext,
    CreateGroup, API, autoColorFunction, isEntityOperation
} from '../Operations'
import { DropdownItem } from "../Components/DropdownItem";
import { UncontrolledTooltip } from "../Components/Tooltip";


export function getConstructFromManyContextualItems(ctx: ContextualItemsContext<Entity>): Promise<MenuItemBlock | undefined> | undefined {
    if (ctx.lites.length == 0)
        return undefined;

    const types = ctx.lites.groupBy(lite => lite.EntityType);

    if (types.length != 1)
        return undefined;

    const ti = getTypeInfo(types[0].key);

    const menuItems = operationInfos(ti)
        .filter(oi => oi.operationType == OperationType.ConstructorFromMany)
        .map(oi => {
            const os = getSettings(oi.key) as ContextualOperationSettings<Entity>;
            const coc = {
                context: ctx,
                operationInfo: oi,
                settings: os,
            } as ContextualOperationContext<Entity>;

            if (os == undefined || os.isVisible == undefined || os.isVisible(coc))
                return coc;

            return undefined;
        })
        .filter(coc => coc != undefined)
        .map(coc => coc!)
        .orderBy(coc => coc.settings && coc.settings.order)
        .flatMap(coc => MenuItemConstructor.createContextualMenuItem(coc, defaultConstructFromMany));

    if (!menuItems.length)
        return undefined;

    return Promise.resolve({
        header: SearchMessage.Create.niceToString(),
        menuItems: menuItems
    } as MenuItemBlock);
}



function defaultConstructFromMany(coc: ContextualOperationContext<Entity>, ...args: any[]) {

    confirmInNecessary(coc).then(conf => {
        if (!conf)
            return;

        API.constructFromMany<Entity, Entity>(coc.context.lites, coc.operationInfo.key, ...args).then(pack => {
            Navigator.createNavigateOrTab(pack, coc.event!);
        }).done();
    }).done();

}

export function getEntityOperationsContextualItems(ctx: ContextualItemsContext<Entity>): Promise<MenuItemBlock | undefined> | undefined {
    if (ctx.lites.length == 0)
        return undefined;

    const types = ctx.lites.groupBy(coc => coc.EntityType);

    if (types.length != 1)
        return undefined;

    const ti = getTypeInfo(types[0].key);

    const contexts = operationInfos(ti)
        .filter(oi => isEntityOperation(oi.operationType))
        .map(oi => {
            const eos = getSettings(oi.key) as EntityOperationSettings<Entity> | undefined;
            const cos = eos == undefined ? undefined :
                ctx.lites.length == 1 ? eos.contextual : eos.contextualFromMany
            const coc = new ContextualOperationContext<Entity>(oi, ctx);
            coc.settings = cos;
            coc.entityOperationSettings = eos;

            const visibleByDefault = oi.lite && (ctx.lites.length == 1 || oi.operationType != OperationType.ConstructorFrom)

            if (eos == undefined ? visibleByDefault :
                cos == undefined || cos.isVisible == undefined ? (visibleByDefault && eos.isVisible == undefined && (eos.onClick == undefined || cos != undefined && cos.onClick != undefined)) :
                    cos.isVisible(coc))
                return coc;

            return undefined;
        })
        .filter(coc => coc != undefined)
        .map(coc => coc!)
        .orderBy(coc => coc.settings && coc.settings.order);

    if (!contexts.length)
        return undefined;

    let contextPromise: Promise<ContextualOperationContext<Entity>[]>;
    if (ctx.lites.length == 1 && contexts.some(coc => coc.operationInfo.hasCanExecute)) {
        contextPromise = Navigator.API.fetchEntityPack(ctx.lites[0]).then(ep => {
            contexts.forEach(coc => coc.canExecute = ep.canExecute[coc.operationInfo.key]);
            return contexts;
        });
    } else if (ctx.lites.length > 1 && contexts.some(coc => coc.operationInfo.hasStates)) {
        contextPromise = API.stateCanExecutes(ctx.lites, contexts.filter(coc => coc.operationInfo.hasStates).map(a => a.operationInfo.key))
            .then(response => {
                contexts.forEach(coc => coc.canExecute = response.canExecutes[coc.operationInfo.key]);
                return contexts;
            });
    } else {
        contextPromise = Promise.resolve(contexts);
    }


    return contextPromise.then(ctxs => {
        const menuItems = ctxs.filter(coc => coc.canExecute == undefined || !hideOnCanExecute(coc))
            .orderBy(coc => coc.settings && coc.settings.order != undefined ? coc.settings.order :
                coc.entityOperationSettings && coc.entityOperationSettings.order != undefined ? coc.entityOperationSettings.order : 0)
            .flatMap(coc => MenuItemConstructor.createContextualMenuItem(coc, defaultContextualClick));

        if (menuItems.length == 0)
            return undefined;

        return {
            header: SearchMessage.Operations.niceToString(),
            menuItems: menuItems
        } as MenuItemBlock;
    });
}


function hideOnCanExecute(coc: ContextualOperationContext<Entity>) {
    if (coc.settings && coc.settings.hideOnCanExecute != undefined)
        return coc.settings.hideOnCanExecute;

    if (coc.entityOperationSettings && coc.entityOperationSettings.hideOnCanExecute != undefined)
        return coc.entityOperationSettings.hideOnCanExecute;

    return false;
}



export function confirmInNecessary(coc: ContextualOperationContext<Entity>): Promise<boolean> {

    const confirmMessage = getConfirmMessage(coc);

    if (confirmMessage == undefined)
        return Promise.resolve(true);

    return MessageModal.show({
        title: OperationMessage.Confirm.niceToString(),
        message: confirmMessage,
        buttons: "yes_no",
        icon: "question"
    }).then(result => { return result == "yes"; });
}

function getConfirmMessage(coc: ContextualOperationContext<Entity>) {
    if (coc.settings && coc.settings.confirmMessage === null)
        return undefined;

    if (coc.settings && coc.settings.confirmMessage != undefined)
        return coc.settings.confirmMessage(coc);

    //eoc.settings.confirmMessage === undefined
    if (coc.operationInfo.operationType == OperationType.Delete)
        return coc.context.lites.length > 1 ?
            OperationMessage.PleaseConfirmYouDLikeToDeleteTheSelectedEntitiesFromTheSystem.niceToString() :
            OperationMessage.PleaseConfirmYouDLikeToDeleteTheEntityFromTheSystem.niceToString();

    return undefined;
}


export namespace MenuItemConstructor { //To allow monkey patching

    export function simplifyName(niceName: string) {
        const array = new RegExp(OperationMessage.CreateFromRegex.niceToString()).exec(niceName);
        return array ? (niceName.tryBefore(array[1]) || "") + array[1].firstUpper() : niceName;
    }
    export function createContextualMenuItem(coc: ContextualOperationContext<Entity>, defaultClick: (coc: ContextualOperationContext<Entity>) => void) {

        const text = coc.settings && coc.settings.text ? coc.settings.text() :
            coc.entityOperationSettings && coc.entityOperationSettings.text ? coc.entityOperationSettings.text() :
                simplifyName(coc.operationInfo.niceName);

        const color = coc.settings && coc.settings.color || coc.entityOperationSettings && coc.entityOperationSettings.color || autoColorFunction(coc.operationInfo);
        const icon = coc.settings && coc.settings.icon;

        const disabled = !!coc.canExecute;

        const onClick = (me: React.MouseEvent<any>) => {
            coc.event = me;
            coc.settings && coc.settings.onClick ? coc.settings!.onClick!(coc) : defaultClick(coc)
        }

        let innerRef: HTMLElement | null;

        return [
            <DropdownItem
                innerRef={b => innerRef = b}
                onClick={disabled ? undefined : onClick}
                disabled={disabled}
                data-operation={coc.operationInfo.key}>
                {icon ? <span className={classes("icon", icon)} style={{ color: coc.settings && coc.settings.iconColor }}></span> :
                    color ? <span className={classes("icon", "empty-icon", "btn-" + color)}></span> : undefined}
                {(icon || color) && " "}
                {text}
            </DropdownItem>,
            coc.canExecute ? <UncontrolledTooltip placement="right" target={() => innerRef!}>{coc.canExecute}</UncontrolledTooltip> : undefined
        ].filter(a => a != null);
    }
}


export function defaultContextualClick(coc: ContextualOperationContext<any>, ...args: any[]) {

    coc.event!.persist();

    confirmInNecessary(coc).then(conf => {
        if (!conf)
            return;

        switch (coc.operationInfo.operationType) {
            case OperationType.ConstructorFrom:
                if (coc.context.lites.length == 1) {
                    API.constructFromLite(coc.context.lites[0], coc.operationInfo.key, ...args)
                        .then(coc.onConstructFromSuccess || (pack => {
                            coc.context.markRows({});
                            Navigator.createNavigateOrTab(pack, coc.event!);
                        }))
                        .done();
                } else {
                    API.constructFromMultiple(coc.context.lites, coc.operationInfo.key, ...args)
                        .then(coc.onContextualSuccess || (report => coc.context.markRows(report.errors)))
                        .done();
                }
                break;
            case OperationType.Execute:
                API.executeMultiple(coc.context.lites, coc.operationInfo.key, ...args)
                    .then(coc.onContextualSuccess || (report => coc.context.markRows(report.errors)))
                    .done();
                break;
            case OperationType.Delete:
                API.deleteMultiple(coc.context.lites, coc.operationInfo.key, ...args)
                    .then(coc.onContextualSuccess || (report => coc.context.markRows(report.errors)))
                    .done();
                break;
        }
    }).done();

}

