﻿import * as React from "react"
import { Router, Route, Redirect, IndexRoute } from "react-router"
import { ajaxGet, ajaxPost } from './Services';
import { openModal } from './Modals';
import { Dic } from './Globals';
import { Lite, Entity, ModifiableEntity, EmbeddedEntity, SelectorMessage, EntityPack, MixinEntity } from './Signum.Entities';
import { PropertyRoute, PseudoType, EntityKind, TypeInfo, IType, Type, getTypeInfo, OperationType, getTypeName, basicConstruct, OperationInfo } from './Reflection';
import SelectorModal from './SelectorModal';
import * as Operations from './Operations';
import * as Navigator from './Navigator';

export const customConstructors: { [typeName: string]: (typeName: string) => ModifiableEntity | Promise<ModifiableEntity> } = { }

export function construct<T extends ModifiableEntity>(type: Type<T>): Promise<EntityPack<T> | undefined>;
export function construct(type: string): Promise<EntityPack<ModifiableEntity> | undefined>;
export function construct(type: string | Type<any>): Promise<EntityPack<ModifiableEntity> | undefined> {
    
    const typeName = (type as Type<any>).typeName || type as string;

    const c = customConstructors[typeName];
    if (c)
        return asPromise(c(typeName)).then<EntityPack<ModifiableEntity> | undefined>(e => {
            if (e == undefined)
                return undefined;

            assertCorrect(e);
            return Navigator.toEntityPack(e, true);
        });

    const ti = getTypeInfo(typeName);

    if (ti) {

        const constructOperations = Dic.getValues(ti.operations!).filter(a => a.operationType == OperationType.Constructor);

        if (constructOperations.length) {

            const ctrs = constructOperations.filter(oi => Operations.isOperationAllowed(oi)); 

            if (!ctrs.length)
                throw new Error("No constructor is allowed!");

            return SelectorModal.chooseElement(ctrs, { display: c => c.niceName, name: c => c.key, message: SelectorMessage.PleaseSelectAConstructor.niceToString() })
                .then((oi: OperationInfo | undefined) => {

                    if (!oi)
                        return undefined;

                    const settings = Operations.getSettings(oi.key) as Operations.ConstructorOperationSettings<Entity>;

                    if (settings && settings.onConstruct)
                        return settings.onConstruct({ operationInfo: oi, settings: settings, typeInfo: ti });

                    return Operations.API.construct(ti.name, oi.key) as Promise<EntityPack<Entity> | undefined>
                }).then((p: EntityPack<Entity> | undefined) => {
                    if (p == undefined)
                        return undefined;

                    assertCorrect(p.entity);
                    return p;
                });
        }
    }

    const result = basicConstruct(typeName);

    assertCorrect(result);

    return Navigator.toEntityPack(result, true);
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