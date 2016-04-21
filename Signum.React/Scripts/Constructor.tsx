import * as React from "react"
import { Router, Route, Redirect, IndexRoute } from "react-router"
import { ajaxGet, ajaxPost } from './Services';
import { openModal } from './Modals';
import { Dic } from './Globals';
import { Lite, Entity, ModifiableEntity, EmbeddedEntity, SelectorMessage, EntityPack } from './Signum.Entities';
import { PropertyRoute, PseudoType, EntityKind, TypeInfo, IType, Type, getTypeInfo, OperationType, getTypeName } from './Reflection';
import SelectorPopup from './SelectorPopup';
import * as Operations from './Operations';
import * as Navigator from './Navigator';

export var customConstructors: { [typeName: string]: (typeName: string) => ModifiableEntity | Promise<ModifiableEntity> } = { }

export function construct<T extends ModifiableEntity>(type: Type<T>): Promise<EntityPack<T>>;
export function construct(type: string): Promise<EntityPack<ModifiableEntity>>;
export function construct(type: string | Type<any>): Promise<EntityPack<ModifiableEntity>> {
    
    const typeName = (type as Type<any>).typeName || type as string;

    var c = customConstructors[typeName];
    if (c)
        return asPromise(c(typeName)).then(e => { assertCorrect(e); return Navigator.toEntityPack(result, true); });

    var ti = getTypeInfo(typeName);

    if (ti) {
        var ctrs = Operations.operationInfos(ti).filter(a => a.operationType == OperationType.Constructor);

        if (ctrs.length) {

            return SelectorPopup.chooseElement(ctrs, c => c.niceName, SelectorMessage.PleaseSelectAConstructor.niceToString())
                .then(c => Operations.API.construct(typeName, c.key)).then(p => { assertCorrect(p.entity); return p; });
        }
    }

    const result = { Type: typeName, isNew: true, modified: true } as ModifiableEntity;

    assertCorrect(result);

    return Navigator.toEntityPack(result, true);
}

export function basicConstruct(type: PseudoType): ModifiableEntity {
    return { Type: getTypeName(type), isNew: true, modified: true } as any as ModifiableEntity;
}

function asPromise<T>(valueOrPromise: T | Promise<T>) {
    if (valueOrPromise && (valueOrPromise as Promise<T>).then)
        return valueOrPromise as Promise<T>;

    return Promise.resolve(valueOrPromise as T);
}

function assertCorrect(m: ModifiableEntity) {
    if (m && !m.isNew && !(m as Entity).id)
        throw new Error("Member 'isNew' expected after constructor");

    if (m.modified == undefined)
        throw new Error("Member 'modified' expected after constructor");
}

export function registerConstructor<T extends ModifiableEntity>(type: Type<T>, constructor: (typeName: string) => T) {
    customConstructors[type.typeName] = constructor;
}

export function registerConstructorPromise<T extends ModifiableEntity>(type: Type<T>, constructor: (typeName: string) => Promise<T>) {
    customConstructors[type.typeName] = constructor;
}