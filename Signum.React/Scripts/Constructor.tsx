import * as React from "react"
import { Router, Route, Redirect, IndexRoute } from "react-router"
import { ajaxGet, ajaxPost } from 'Framework/Signum.React/Scripts/Services';
import { openModal } from 'Framework/Signum.React/Scripts/Modals';
import { IEntity, Lite, Entity, ModifiableEntity, EmbeddedEntity } from 'Framework/Signum.React/Scripts/Signum.Entities';
import { PropertyRoute, PseudoType, EntityKind, TypeInfo, IType, Type, getTypeInfo } from 'Framework/Signum.React/Scripts/Reflection';
import * as Finder from 'Framework/Signum.React/Scripts/Finder';


export function construct<T extends ModifiableEntity>(type: Type<T>): Promise<T>;
export function construct(type: string): Promise<ModifiableEntity>;
export function construct(type: string | Type<any>): Promise<ModifiableEntity> {

    var typeName = (type as Type<any>).typeName || type as string;

    return Promise.resolve({ Type: typeName } as ModifiableEntity);
}