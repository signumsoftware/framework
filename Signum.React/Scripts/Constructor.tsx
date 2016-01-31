import * as React from "react"
import { Router, Route, Redirect, IndexRoute } from "react-router"
import { ajaxGet, ajaxPost } from './Services';
import { openModal } from './Modals';
import { IEntity, Lite, Entity, ModifiableEntity, EmbeddedEntity } from './Signum.Entities';
import { PropertyRoute, PseudoType, EntityKind, TypeInfo, IType, Type, getTypeInfo } from './Reflection';
import * as Finder from './Finder';


export function construct<T extends ModifiableEntity>(type: Type<T>): Promise<T>;
export function construct(type: string): Promise<ModifiableEntity>;
export function construct(type: string | Type<any>): Promise<ModifiableEntity> {

    const typeName = (type as Type<any>).typeName || type as string;

    return Promise.resolve({ Type: typeName } as ModifiableEntity);
}