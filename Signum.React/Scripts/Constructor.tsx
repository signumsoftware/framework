import * as React from "react"
import { Router, Route, Redirect } from "react-router"
import { ajaxGet, ajaxPost } from './Services';
import { openModal } from './Modals';
import { Dic } from './Globals';
import { Lite, Entity, ModifiableEntity, EmbeddedEntity, SelectorMessage, EntityPack, MixinEntity } from './Signum.Entities';
import { PropertyRoute, PseudoType, EntityKind, TypeInfo, IType, Type, getTypeInfo, OperationType, getTypeName, New, OperationInfo } from './Reflection';
import SelectorModal from './SelectorModal';
import * as Operations from './Operations';
import * as Navigator from './Navigator';

export const customConstructors: { [typeName: string]: (typeName: string) => ModifiableEntity | Promise<ModifiableEntity | undefined> } = {}

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
            return Navigator.toEntityPack(e);
        });

    const ti = getTypeInfo(typeName);

    if (ti) {

        const constructOperations = Dic.getValues(ti.operations!).filter(a => a.operationType == OperationType.Constructor);

        if (constructOperations.length) {

            const ctrs = constructOperations.filter(oi => Operations.isOperationInfoAllowed(oi));

            if (!ctrs.length)
                throw new Error("No constructor is allowed!");

            return SelectorModal.chooseElement(ctrs, { buttonDisplay: c => c.niceName, buttonName: c => c.key, message: SelectorMessage.PleaseSelectAConstructor.niceToString() })
                .then((oi: OperationInfo | undefined) => {

                    if (!oi)
                        return undefined;

                    const settings = Operations.getSettings(oi.key) as Operations.ConstructorOperationSettings<Entity>;

                    var ctx = new Operations.ConstructorOperationContext(oi, settings, ti);

                    if (settings && settings.onConstruct)
                        return settings.onConstruct(ctx);

                    return ctx.defaultConstruct();
                }).then((p: EntityPack<Entity> | undefined) => {
                    if (p == undefined)
                        return undefined;

                    assertCorrect(p.entity);
                    return p;
                });
        }
    }

    const result = New(typeName);

    assertCorrect(result);

    return Navigator.toEntityPack(result);
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

export function registerConstructor<T extends ModifiableEntity>(type: Type<T>, constructor: (typeName: string) => T | Promise<T | undefined>) {
    customConstructors[type.typeName] = constructor;
}
