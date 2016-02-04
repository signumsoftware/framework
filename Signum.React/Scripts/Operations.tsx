import * as React from "react"
import { Router, Route, Redirect, IndexRoute } from "react-router"
import { Button, OverlayTrigger, Tooltip, MenuItem, DropdownButton } from "react-bootstrap"
import { Dic, hasFlag } from './Globals';
import { ajaxGet, ajaxPost } from './Services';
import { openModal } from './Modals';
import { IEntity, Lite, Entity, ModifiableEntity, EmbeddedEntity, LiteMessage, OperationMessage, 
    OperationSymbol, ConstructSymbol_From, ConstructSymbol_FromMany, ConstructSymbol_Simple, ExecuteSymbol, DeleteSymbol,  } from './Signum.Entities';
import { PropertyRoute, PseudoType, EntityKind, TypeInfo, IType, Type, getTypeInfo, OperationInfo, OperationType  } from './Reflection';
import { TypeContext } from './TypeContext';
import * as Finder from './Finder';
import * as Navigator from './Navigator';
import NormalPopup from './NormalPage/NormalPopup';
import { EntityComponent }  from './Lines';
import { getButtonBarElements }  from './Operations/EntityOperations';

export function start() {
    Navigator.onButtonBarRender.push(getButtonBarElements);
}


export const operationSettings: { [operationKey: string]: OperationSettings } = {};

export function addSettings(...settings: OperationSettings[]) {
    settings.forEach(s => Dic.addOrThrow(operationSettings, s.operationSymbol.key, s));
}


export function getSettings(operation: OperationSymbol | string): OperationSettings {
    const operationKey = (operation as OperationSymbol).key || operation as string; 

    return operationSettings[operationKey];
}

var isOperationAllowed: (oi: OperationInfo) => boolean;

export function operationInfos(ti: TypeInfo) {
    return Dic.getValues(ti.operations).filter(isOperationAllowed);
}


export abstract class OperationSettings {

    text: () => string;
    operationSymbol: OperationSymbol;

    constructor(operationSymbol: OperationSymbol) {
        this.operationSymbol = operationSymbol;
    }
}

export interface ConstructorOperationContext<T extends Entity> {
    operationInfo: OperationInfo;
    settings: ConstructorOperationSettings<T>
}

export class ConstructorOperationSettings<T extends Entity> extends OperationSettings {

    isVisible: (ctx: ConstructorOperationContext<T>) => boolean;
    onConstruct: (ctx: ConstructorOperationContext<T>) => Promise<T>;

    constructor(operationSymbol: ConstructSymbol_Simple<T>) {
        super(operationSymbol)
    }
}

export interface ContextualOperationContext<T extends Entity> {
    entity: Lite<T>[];
    operationInfo: OperationInfo;
    settings: ContextualOperationSettings<T>;
    entityOperationSettings: EntityOperationSettings<T>;
    canExecute: string;
    queryKey: string;
}

export class ContextualOperationSettings<T extends Entity> extends OperationSettings {

    isVisible: (ctx: ContextualOperationContext<T>) => boolean;
    confirmMessage: (ctx: ContextualOperationContext<T>) => string;
    onClick: (ctx: ContextualOperationContext<T>) => Promise<void>;

    constructor(operationSymbol: ExecuteSymbol<T> | DeleteSymbol<T> | ConstructSymbol_From<any, T> | ConstructSymbol_FromMany<any, T>) {
        super(operationSymbol);
    }
}

export interface EntityOperationContext<T extends Entity> {
    component: EntityComponent<T>;
    entity: T;
    operationInfo: OperationInfo;
    settings: EntityOperationSettings<T>;
    canExecute: string;
}

export class EntityOperationSettings<T extends Entity> extends OperationSettings {

    contextual: ContextualOperationSettings<T>;
    contextualFromMany: ContextualOperationSettings<T>;
    
    isVisible: (ctx: EntityOperationContext<T>) => boolean;
    confirmMessage: (ctx: EntityOperationContext<T>) => string;
    onClick: (ctx: EntityOperationContext<T>) => Promise<void>;
    hideOnCanExecute: boolean;
    group: EntityOperationGroup;
    order: number;
    style: string;

    constructor(operationSymbol: ExecuteSymbol<T> | DeleteSymbol<T> | ConstructSymbol_From<any, T>) {
        super(operationSymbol)
    }
}


export var CreateGroup: EntityOperationGroup = {
    key: "create",
    text: () => OperationMessage.Create.niceToString(),
    simplifyName: cs => {
        var array = new RegExp(OperationMessage.CreateFromRegex.niceToString()).exec(cs);
        return array ? array[1] : cs;
    },
    cssClass: "sf-operation",
    order: 200,
};

export interface EntityOperationGroup {
    key: string;
    text: () => string;
    simplifyName?: (complexName: string) => string;
    cssClass?: string;
    order?: number;
}
