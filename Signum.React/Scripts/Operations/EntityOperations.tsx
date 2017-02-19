import * as React from "react"
import { Router, Route, Redirect, IndexRoute } from "react-router"
import { Button, OverlayTrigger, Tooltip, MenuItem, DropdownButton } from "react-bootstrap"
import {
    Lite, Entity, ModifiableEntity, EmbeddedEntity, LiteMessage, EntityPack, toLite, JavascriptMessage,
    OperationSymbol, ConstructSymbol_From, ConstructSymbol_FromMany, ConstructSymbol_Simple, ExecuteSymbol, DeleteSymbol, OperationMessage, getToString, NormalControlMessage, NormalWindowMessage
} from '../Signum.Entities';
import { PropertyRoute, PseudoType, EntityKind, TypeInfo, IType, Type, getTypeInfo, OperationInfo, OperationType, LambdaMemberType, GraphExplorer } from '../Reflection';
import { classes, ifError } from '../Globals';
import { ButtonsContext, IOperationVisible } from '../TypeContext';
import * as Navigator from '../Navigator';
import Notify from '../Frames/Notify';
import { ajaxPost, ValidationError }  from '../Services';
import { TypeContext }  from '../TypeContext';
import { operationInfos, getSettings, EntityOperationSettings, EntityOperationContext, EntityOperationGroup,
    CreateGroup, API, isEntityOperation, autoStyleFunction } from '../Operations'


export function getEntityOperationButtons(ctx: ButtonsContext): Array<React.ReactElement<any> | undefined> | undefined {
    const ti = getTypeInfo(ctx.pack.entity.Type);

    if (ti == undefined)
        return undefined;

    const operations = operationInfos(ti)
        .filter(oi => isEntityOperation(oi.operationType) && (oi.allowsNew || !ctx.pack.entity.isNew))
        .map(oi => {
            const eos = getSettings(oi.key) as EntityOperationSettings<Entity>;

            const eoc: EntityOperationContext<Entity> = {
                entity: ctx.pack.entity as Entity,
                frame: ctx.frame,
                tag: ctx.tag,
                canExecute: ctx.pack.canExecute[oi.key],
                operationInfo: oi,
                settings: eos
            };

            if (ctx.isOperationVisible && !ctx.isOperationVisible(eoc))
                return undefined;

            var ov = ctx.frame.entityComponent as any as IOperationVisible;
            if (ov && ov.isOperationVisible && !ov.isOperationVisible(eoc))
                return undefined;

            if (eos && eos.isVisible && !eos.isVisible(eoc))
                return undefined;

            if (eos && eos.hideOnCanExecute && eoc.canExecute)
                return undefined;

            return eoc;
        })
        .filter(eoc => eoc != undefined)
        .map(eoc => eoc!);

    const groups = operations.groupBy(eoc => {

        const group = getGroup(eoc);

        if (group == undefined)
            return "";

        return group.key;
    });

    const result = groups.flatMap((gr, i) => {
        if (gr.key == "") {
            return gr.elements.map((eoc, j) => ({
                order: eoc.settings && eoc.settings.order != undefined ? eoc.settings.order : 0,
                button: createDefaultButton(eoc, undefined, false, i + "-" + j)
            }));
        } else {

            const group = getGroup(gr.elements[0])!;


            return [{
                order: group.order != undefined ? group.order : 100,
                button: (
                    <DropdownButton title={group.text() } data-key={group.key} key={i} id={group.key}>
                        { gr.elements
                            .orderBy(a => a.settings && a.settings.order)
                            .map((eoc, j) => createDefaultButton(eoc, group, true, j))
                        }
                    </DropdownButton>
                )
            }];
        }
    });

    return result.orderBy(a => a.order).map(a => a.button);
}

export function createEntityOperationContext<T extends Entity>(ctx: TypeContext<T>, operation: ExecuteSymbol<T> | DeleteSymbol<T> | ConstructSymbol_From<T, any>): EntityOperationContext<T> {

    if (!ctx.frame)
        throw new Error("a frame is necessary");

    return {
        frame: ctx.frame,
        entity: ctx.value,
        settings: getSettings(operation) as EntityOperationSettings<T>,
        operationInfo: getTypeInfo(ctx.value.Type).operations![operation.key!],
        canExecute: undefined,
    };
}

function getGroup(eoc: EntityOperationContext<Entity>) {
    if (eoc.settings != undefined && eoc.settings.group !== undefined) {
        return eoc.settings.group;
    }

    if (eoc.operationInfo.operationType == OperationType.ConstructorFrom)
        return CreateGroup;

    return undefined;
}

function getWithClose(eoc: EntityOperationContext<Entity>) {
    let withClose = eoc.settings && eoc.settings.withClose;

    if (withClose != undefined)
        return withClose;

    return eoc.operationInfo.key.after(".") == "Save";
}

function createDefaultButton(eoc: EntityOperationContext<Entity>, group: EntityOperationGroup | undefined, asMenuItem: boolean, key: any) {
    
    const text = eoc.settings && eoc.settings.text ? eoc.settings.text() :
        group && group.simplifyName ? group.simplifyName(eoc.operationInfo.niceName) :
            eoc.operationInfo.niceName;

    const withClose = getWithClose(eoc);

    const bsStyle = eoc.settings && eoc.settings.style || autoStyleFunction(eoc.operationInfo);

    const disabled = !!eoc.canExecute;

    const btn = asMenuItem ? <MenuItem className={classes("btn-" + bsStyle, disabled ? "disabled" : undefined)} onClick={disabled ? undefined : e => onClick(eoc, e)} data-operation={eoc.operationInfo.key} key={key} > {text}</MenuItem> :
        withClose ?
            <div className="btn-group" key={key}>
                <Button bsStyle={bsStyle} className={disabled ? "disabled" : undefined} onClick={disabled ? undefined : e => onClick(eoc, e)} data-operation={eoc.operationInfo.key}>{text}</Button>
                <Button bsStyle={bsStyle} className={classes("dropdown-toggle dropdown-toggle-split", disabled ? "disabled" : undefined)} onClick={disabled ? undefined : e => { eoc.closeRequested = true; onClick(eoc, e); }}
                    title={NormalWindowMessage._0AndClose.niceToString(eoc.operationInfo.niceName)}>
                    <span>&times;</span>
                </Button>

            </div>
        :
        <Button bsStyle={bsStyle} className={disabled ? "disabled" : undefined} onClick={disabled ? undefined : e => onClick(eoc, e)} data-operation={eoc.operationInfo.key} key={key}>{text}</Button>


    if (!eoc.canExecute)
        return btn;

    const tooltip = <Tooltip id={"tooltip_" + eoc.operationInfo.key.replace(".", "_") }>{eoc.canExecute}</Tooltip>;

    return <OverlayTrigger placement="bottom" overlay={tooltip} key={key}>{btn}</OverlayTrigger>;
}

function onClick(eoc: EntityOperationContext<Entity>, event: React.MouseEvent<any>): void{

    event.persist();

    if (eoc.settings && eoc.settings.onClick)
        return eoc.settings.onClick(eoc, event);

    if (eoc.operationInfo.lite) {
        switch (eoc.operationInfo.operationType) {
            case OperationType.ConstructorFrom: defaultConstructFromLite(eoc, event); return;
            case OperationType.Execute: defaultExecuteLite(eoc); return;
            case OperationType.Delete: defaultDeleteLite(eoc); return;
        }
    } else {
        switch (eoc.operationInfo.operationType) {
            case OperationType.ConstructorFrom: defaultConstructFromEntity(eoc, event); return;
            case OperationType.Execute: defaultExecuteEntity(eoc); return;
            case OperationType.Delete: defaultDeleteEntity(eoc); return;
        }
    }

    throw new Error("Unexpected OperationType");
}

export function notifySuccess() {
    Notify.singletone.notifyTimeout({ text: JavascriptMessage.executed.niceToString(), type: "success" });
}

export function defaultConstructFromEntity(eoc: EntityOperationContext<Entity>, event: React.MouseEvent<any>, ...args: any[]) {

    if (!confirmInNecessary(eoc))
        return;

    API.constructFromEntity(eoc.entity, eoc.operationInfo.key, ...args)
        .then(pack => {
            notifySuccess();
            Navigator.createNavigateOrTab(pack, event);
        })
        .catch(ifError(ValidationError, e => eoc.frame.setError(e.modelState, "request.entity")))
        .done();

}

export function defaultConstructFromLite(eoc: EntityOperationContext<Entity>, event: React.MouseEvent<any>, ...args: any[]) {

    if (!confirmInNecessary(eoc))
        return;

    API.constructFromLite(toLite(eoc.entity), eoc.operationInfo.key, ...args)
        .then(pack => {
            notifySuccess();
            Navigator.createNavigateOrTab(pack, event);
        })
        .catch(ifError(ValidationError, e => eoc.frame.setError(e.modelState, "request.entity")))
        .done();
}


export function defaultExecuteEntity(eoc: EntityOperationContext<Entity>, ...args: any[]){

    if (!confirmInNecessary(eoc))
        return;

    API.executeEntity(eoc.entity, eoc.operationInfo.key, ...args)
        .then(pack => { eoc.frame.onReload(pack); notifySuccess(); if (eoc.closeRequested) { eoc.frame.onClose(true); }  })
        .catch(ifError(ValidationError, e => eoc.frame.setError(e.modelState, "request.entity")))
        .done();
}

export function defaultExecuteLite(eoc: EntityOperationContext<Entity>, ...args: any[]) {

    if (!confirmInNecessary(eoc))
        return;

    API.executeLite(toLite(eoc.entity), eoc.operationInfo.key, ...args)
        .then(pack => { eoc.frame.onReload(pack); notifySuccess(); if (eoc.closeRequested) { eoc.frame.onClose(true); } })
        .catch(ifError(ValidationError, e => eoc.frame.setError(e.modelState, "request.entity")))
        .done();
}

export function defaultDeleteEntity(eoc: EntityOperationContext<Entity>, ...args: any[]){

    if (!confirmInNecessary(eoc))
        return;

    API.deleteEntity(eoc.entity, eoc.operationInfo.key, ...args)
        .then(() => { eoc.frame.onClose(); notifySuccess(); })
        .catch(ifError(ValidationError, e => eoc.frame.setError(e.modelState, "request.entity")))
        .done();
}

export function defaultDeleteLite(eoc: EntityOperationContext<Entity>, ...args: any[]) {

    if (!confirmInNecessary(eoc))
        return;

    API.deleteLite(toLite(eoc.entity), eoc.operationInfo.key, ...args)
        .then(() => { eoc.frame.onClose(); notifySuccess(); })
        .catch(ifError(ValidationError, e => eoc.frame.setError(e.modelState, "request.entity")))
        .done();
}


export function confirmInNecessary(eoc: EntityOperationContext<Entity>, checkLite = true): boolean {

    if (eoc.operationInfo.lite) {
        GraphExplorer.propagateAll(eoc.entity);

        if (eoc.entity.modified)
            throw new Error(NormalControlMessage.SaveChangesFirst.niceToString());
    }

    const confirmMessage = getConfirmMessage(eoc);

    return confirmMessage == undefined || confirm(confirmMessage);
}

function getConfirmMessage(eoc: EntityOperationContext<Entity>) {
    if (eoc.settings && eoc.settings.confirmMessage === undefined)
        return undefined;
  
    if (eoc.settings && eoc.settings.confirmMessage != undefined)
        return eoc.settings.confirmMessage(eoc);

    //eoc.settings.confirmMessage === undefined
    if (eoc.operationInfo.operationType == OperationType.Delete)
        return OperationMessage.PleaseConfirmYouDLikeToDeleteTheEntityFromTheSystem.niceToString(getToString(eoc.entity));

    return undefined;
}





export function needsCanExecute(entity: ModifiableEntity) {

    const ti = getTypeInfo(entity.Type);

    if (!ti)
        return false;

    return operationInfos(ti).some(a => a.hasCanExecute && isEntityOperation(a.operationType) && (a.allowsNew || !entity.isNew));
}