
import * as React from 'react'
import { Route } from 'react-router'
import { ajaxPost, ajaxGet, WebApiHttpError } from '@framework/Services';
import { EntitySettings, ViewPromise } from '@framework/Navigator'
import * as Navigator from '@framework/Navigator'
import { EntityData, EntityKind } from '@framework/Reflection'
import { EntityOperationSettings } from '@framework/Operations'
import * as Operations from '@framework/Operations'
import { Entity } from '@framework/Signum.Entities'
import * as Constructor from '@framework/Constructor'
import { StyleContext } from '@framework/TypeContext'

import { ValueLine, EntityLine, EntityCombo, EntityList, EntityDetail, EntityStrip, EntityRepeater } from '@framework/Lines'
import * as AuthClient from '../Authorization/AuthClient'
import * as OmniboxClient from '../Omnibox/OmniboxClient'
import { ImportRoute } from "@framework/AsyncImport";

export namespace API {
    export function typeHelp(typeName: string, mode: TypeHelpMode): Promise<TypeHelp | undefined> {
        return ajaxGet<TypeHelp | undefined>({ url: `~/api/typeHelp/${typeName}/${mode}` });
    }

    export function autocompleteEntityCleanType(request: AutocompleteEntityCleanType): Promise<string[]> {
        return ajaxPost<string[]>({ url: "~/api/typeHelp/autocompleteEntityCleanType" }, request);
    }

    export function autocompleteType(request: AutocompleteTypeRequest): Promise<string[]> {
        return ajaxPost<string[]>({ url: "~/api/typeHelp/autocompleteType" }, request);
    }
}

export type TypeHelpMode = "TypeScript" | "CSharp";

export interface TypeHelp {
    type: string;
    cleanTypeName: string;
    isEnum: boolean;
    members: TypeMemberHelp[];
}

export interface TypeMemberHelp {
    propertyString: string;
    name?: string; //Mixins, MListElements
    type: string;
    isExpression: boolean;
    isEnum: boolean;
    cleanTypeName: string | null;
    subMembers: TypeMemberHelp[];
}

export interface AutocompleteTypeRequest {
    query: string;
    limit: number;
    includeBasicTypes: boolean;
    includeEntities?: boolean;
    includeEmbeddedEntities?: boolean;
    includeMList?: boolean;
    includeQueriable?: boolean;
}

export interface AutocompleteEntityCleanType {
    query: string;
    limit: number;
}


