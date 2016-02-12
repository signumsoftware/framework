import * as React from "react"
import { Router, Route, Redirect, IndexRoute } from "react-router"
import { ajaxGet, ajaxPost } from './Services';
import { openModal } from './Modals';
import { Dic } from './Globals';
import { IEntity, Lite, Entity, ModifiableEntity, EmbeddedEntity, SelectorMessage, EntityPack } from './Signum.Entities';
import { PropertyRoute, PseudoType, EntityKind, TypeInfo, IType, Type, getTypeInfo, OperationType } from './Reflection';
import SelectorPopup from './SelectorPopup';
import { API, operationInfos } from './Operations';

export var customConstructors: { [typeName: string]: (typeName: string) => Promise<ModifiableEntity> } = { }

export function construct<T extends ModifiableEntity>(type: Type<T>): Promise<EntityPack<T>>;
export function construct(type: string): Promise<EntityPack<ModifiableEntity>>;
export function construct(type: string | Type<any>): Promise<EntityPack<ModifiableEntity>> {
    
    const typeName = (type as Type<any>).typeName || type as string;

    var c = customConstructors[typeName];
    if (c)
        return c(typeName).then(assertCorrect);

    var ti = getTypeInfo(typeName);

    if (!ti) {
        var ctrs = operationInfos(ti).filter(a => a.operationType == OperationType.Constructor);

        if (ctrs.length) {

            var ctr = ctrs.length == 1 ? Promise.resolve(ctrs[0]) :
                SelectorPopup.chooseElement(ctrs, c => c.niceName, SelectorMessage.PleaseSelectAConstructor.niceToString());

            return ctr.then(c => API.construct(c.key));
        }
    }

    return Promise.resolve(assertCorrect({ Type: typeName, isNew: true, modified: true } as ModifiableEntity));
}



function assertCorrect(m: ModifiableEntity): EntityPack<ModifiableEntity> {
    if (m && !m.isNew && !(m as Entity).id)
        throw new Error("Member 'isNew' expected after constructor");

    return { entity: m, canExecute: null };
}

export function registerConstructor<T extends ModifiableEntity>(type: Type<T>, constructor: (typeName: string) => Promise<T>) {
    customConstructors[type.typeName] = constructor;
}