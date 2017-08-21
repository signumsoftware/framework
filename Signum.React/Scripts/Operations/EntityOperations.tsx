import * as React from "react"
import { Router, Route, Redirect } from "react-router"
import { Button, UncontrolledTooltip, DropdownItem, ButtonDropdown, Tooltip } from "reactstrap"
import {
    Lite, Entity, ModifiableEntity, EmbeddedEntity, LiteMessage, EntityPack, toLite, JavascriptMessage,
    OperationSymbol, ConstructSymbol_From, ConstructSymbol_FromMany, ConstructSymbol_Simple, ExecuteSymbol, DeleteSymbol, OperationMessage, getToString, NormalControlMessage, NormalWindowMessage
} from '../Signum.Entities';
import { PropertyRoute, PseudoType, EntityKind, TypeInfo, IType, Type, getTypeInfo, OperationInfo, OperationType, LambdaMemberType, GraphExplorer } from '../Reflection';
import { classes, ifError } from '../Globals';
import { ButtonsContext, IOperationVisible } from '../TypeContext';
import * as Navigator from '../Navigator';
import Notify from '../Frames/Notify';
import MessageModal from '../Modals/MessageModal'
import { ajaxPost, ValidationError } from '../Services';
import { TypeContext } from '../TypeContext';
import {
    operationInfos, getSettings, EntityOperationSettings, EntityOperationContext, EntityOperationGroup,
    CreateGroup, API, isEntityOperation, autoStyleFunction, isSave
} from '../Operations'


export function getEntityOperationButtons(ctx: ButtonsContext): Array<React.ReactElement<any> | undefined> | undefined {
    const ti = getTypeInfo(ctx.pack.entity.Type);

    if (ti == undefined)
        return undefined;

    const operations = operationInfos(ti)
        .filter(oi => isEntityOperation(oi.operationType) && (oi.allowsNew || !ctx.pack.entity.isNew))
        .map(oi => {
            const eos = getSettings(oi.key) as EntityOperationSettings<Entity>;

            const eoc = new EntityOperationContext<Entity>();
            eoc.entity = ctx.pack.entity as Entity;
            eoc.frame = ctx.frame;
            eoc.tag = ctx.tag;
            eoc.canExecute = ctx.pack.canExecute[oi.key];
            eoc.operationInfo = oi;
            eoc.settings = eos;

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

            const group = getGroup(gr.elements[0]) !;


            return [{
                order: group.order != undefined ? group.order : 100,
                button: (
                    <ButtonDropdown title={group.text()} data-key={group.key} key={i} id={group.key}>
                        {gr.elements
                            .orderBy(a => a.settings && a.settings.order)
                            .map((eoc, j) => createDefaultButton(eoc, group, true, j))
                        }
                    </ButtonDropdown>
                )
            }];
        }
    });

    return result.orderBy(a => a.order).map(a => a.button);
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

    return isSave(eoc.operationInfo);
}

function createDefaultButton(eoc: EntityOperationContext<Entity>, group: EntityOperationGroup | undefined, asDropDownItem: boolean, key: any) {

    const text = eoc.settings && eoc.settings.text ? eoc.settings.text() :
        group && group.simplifyName ? group.simplifyName(eoc.operationInfo.niceName) :
            eoc.operationInfo.niceName;

    const withClose = getWithClose(eoc);

    const bsStyle = eoc.settings && eoc.settings.style || autoStyleFunction(eoc.operationInfo);

    const disabled = !!eoc.canExecute;
    const btn = asDropDownItem ? <DropdownItem className={classes("btn-" + bsStyle, disabled ? "disabled" : undefined)} onClick={disabled ? undefined : e => onClick(eoc, e)} data-operation={eoc.operationInfo.key} key={key} > {text}</DropdownItem> :
        withClose ?
            <div className="btn-group" key={key}>
                <Button color={bsStyle} className={disabled ? "disabled" : undefined} onClick={disabled ? undefined : e => onClick(eoc, e)} data-operation={eoc.operationInfo.key}>{text}</Button>
                <Button color={bsStyle} className={classes("dropdown-toggle dropdown-toggle-split", disabled ? "disabled" : undefined)} onClick={disabled ? undefined : e => { eoc.closeRequested = true; onClick(eoc, e); }}
                    title={NormalWindowMessage._0AndClose.niceToString(eoc.operationInfo.niceName)}>
                    <span>&times;</span>
                </Button>

            </div>
            :
            <Button color={bsStyle} className={disabled ? "disabled" : undefined} onClick={disabled ? undefined : e => onClick(eoc, e)} data-operation={eoc.operationInfo.key} key={key}>{text}</Button>


    if (!eoc.canExecute)
        return btn;

    const id = "tooltipContainer_" + eoc.operationInfo.key.replace(".", "_");

    return <span key={key}>
        {React.cloneElement(btn, { id })}
        <Tooltip placement="bottom" target={id}>{eoc.canExecute}</Tooltip>
    </span>;
}

function onClick(eoc: EntityOperationContext<Entity>, event: React.MouseEvent<any>): void {
    eoc.event = event;
    event.persist();

    if (eoc.settings && eoc.settings.onClick)
        return eoc.settings.onClick(eoc);

    defaultOnClick(eoc);
}

export function defaultOnClick(eoc: EntityOperationContext<Entity>, ... args:any[])
{
    if (eoc.operationInfo.lite) {
        switch (eoc.operationInfo.operationType) {
            case OperationType.ConstructorFrom: defaultConstructFromLite(eoc, ...args); return;
            case OperationType.Execute: defaultExecuteLite(eoc, ...args); return;
            case OperationType.Delete: defaultDeleteLite(eoc, ...args); return;
        }
    } else {
        switch (eoc.operationInfo.operationType) {
            case OperationType.ConstructorFrom: defaultConstructFromEntity(eoc, ...args); return;
            case OperationType.Execute: defaultExecuteEntity(eoc, ...args); return;
            case OperationType.Delete: defaultDeleteEntity(eoc, ...args); return;
        }
    }

    throw new Error("Unexpected OperationType");
}

export function notifySuccess() {
    Notify.singletone.notifyTimeout({ text: JavascriptMessage.executed.niceToString(), type: "success" });
}

export function defaultConstructFromEntity(eoc: EntityOperationContext<Entity>, ...args: any[]) {

    confirmInNecessary(eoc).then(conf => {
        if (!conf)
            return;

        API.constructFromEntity(eoc.entity, eoc.operationInfo.key, ...args)
            .then(eoc.onConstructFromSuccess || (pack => {
                notifySuccess();
                Navigator.createNavigateOrTab(pack, eoc.event!);
            }))
            .catch(ifError(ValidationError, e => eoc.frame.setError(e.modelState, "request.entity")))
            .done();
    }).done();
}

export function defaultConstructFromLite(eoc: EntityOperationContext<Entity>, ...args: any[]) {

    confirmInNecessary(eoc).then(conf => {
        if (!conf)
            return;

        API.constructFromLite(toLite(eoc.entity), eoc.operationInfo.key, ...args)
            .then(eoc.onConstructFromSuccess || (pack => {
                notifySuccess();
                Navigator.createNavigateOrTab(pack, eoc.event!);
            }))
            .catch(ifError(ValidationError, e => eoc.frame.setError(e.modelState, "request.entity")))
            .done();
    }).done();
}


export function defaultExecuteEntity(eoc: EntityOperationContext<Entity>, ...args: any[]) {

    confirmInNecessary(eoc).then(conf => {
        if (!conf)
            return;

        API.executeEntity(eoc.entity, eoc.operationInfo.key, ...args)
            .then(eoc.onExecuteSuccess || (pack => {
                eoc.frame.onReload(pack);
                notifySuccess();
                if (eoc.closeRequested)
                    eoc.frame.onClose(true);
            }))
            .catch(ifError(ValidationError, e => eoc.frame.setError(e.modelState, "request.entity")))
            .done();
    }).done();
}

export function defaultExecuteLite(eoc: EntityOperationContext<Entity>, ...args: any[]) {

    confirmInNecessary(eoc).then(conf => {
        if (!conf)
            return;

        API.executeLite(toLite(eoc.entity), eoc.operationInfo.key, ...args)
            .then(eoc.onExecuteSuccess || (pack => {
                eoc.frame.onReload(pack);
                notifySuccess();
                if (eoc.closeRequested)
                    eoc.frame.onClose(true);
            }))
            .catch(ifError(ValidationError, e => eoc.frame.setError(e.modelState, "request.entity")))
            .done();
    }).done();
}

export function defaultDeleteEntity(eoc: EntityOperationContext<Entity>, ...args: any[]) {

    confirmInNecessary(eoc).then(conf => {
        if (!conf)
            return;

        API.deleteEntity(eoc.entity, eoc.operationInfo.key, ...args)
            .then(eoc.onDeleteSuccess || (() => {
                eoc.frame.onClose();
                notifySuccess();
            }))
            .catch(ifError(ValidationError, e => eoc.frame.setError(e.modelState, "request.entity")))
            .done();
    }).done();
}

export function defaultDeleteLite(eoc: EntityOperationContext<Entity>, ...args: any[]) {

    confirmInNecessary(eoc).then(conf => {
        if (!conf)
            return;

        API.deleteLite(toLite(eoc.entity), eoc.operationInfo.key, ...args)
            .then(eoc.onDeleteSuccess || (() => {
                eoc.frame.onClose();
                notifySuccess();
            }))
            .catch(ifError(ValidationError, e => eoc.frame.setError(e.modelState, "request.entity")))
            .done();
    }).done();
}


export function confirmInNecessary(eoc: EntityOperationContext<Entity>, checkLite = true): Promise<boolean> {

    if (eoc.operationInfo.lite) {
        GraphExplorer.propagateAll(eoc.entity);

        if (eoc.entity.modified)
            throw new Error(NormalControlMessage.SaveChangesFirst.niceToString());
    }

    const confirmMessage = getConfirmMessage(eoc);

    if (confirmMessage == undefined)
        return Promise.resolve(true);

    return MessageModal.show({
        title: OperationMessage.Confirm.niceToString(),
        message: confirmMessage,
        buttons: "yes_no",
        icon: "warning",
        style: "warning",
    }).then(result => { return result == "yes"; });
}

function getConfirmMessage(eoc: EntityOperationContext<Entity>) {
    if (eoc.settings && eoc.settings.confirmMessage === null)
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
