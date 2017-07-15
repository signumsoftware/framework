import * as React from 'react'
import { Route } from 'react-router'
import { ajaxPost, ajaxGet } from '../../../Framework/Signum.React/Scripts/Services';
import { EntitySettings, ViewPromise } from '../../../Framework/Signum.React/Scripts/Navigator'
import { Entity, Lite } from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as Navigator from '../../../Framework/Signum.React/Scripts/Navigator'
import { EntityOperationSettings } from '../../../Framework/Signum.React/Scripts/Operations'
import * as Operations from '../../../Framework/Signum.React/Scripts/Operations'
import { CultureInfoEntity } from '../Basics/Signum.Entities.Basics'
import { TranslationPermission } from './Signum.Entities.Translation'
import * as AuthClient from '../Authorization/AuthClient'
import * as OmniboxClient from '../Omnibox/OmniboxClient'
import { ImportRoute } from "../../../Framework/Signum.React/Scripts/AsyncImport";

export function start(options: { routes: JSX.Element[] }) {

    OmniboxClient.registerSpecialAction({
        allowed: () => AuthClient.isPermissionAuthorized(TranslationPermission.TranslateCode),
        key: "TranslateCode",
        onClick: () => Promise.resolve("~/translation/status")
    });

    options.routes.push(
        <ImportRoute path="~/translation/status" onImportModule={() => import("./Code/TranslationCodeStatus")} />,
        <ImportRoute path="~/translation/view/:assembly/:culture?" onImportModule={() => import("./Code/TranslationCodeView")} />,
        <ImportRoute path="~/translation/syncNamespaces/:assembly/:culture" onImportModule={() => import("./Code/TranslationCodeSyncNamespaces")} />,
        <ImportRoute path="~/translation/sync/:assembly/:culture/:namespace?" onImportModule={() => import("./Code/TranslationCodeSync")} />
    );
}


export module API {
    export function status(): Promise<TranslationFileStatus[]> {
        return ajaxGet<TranslationFileStatus[]>({ url: "~/api/translation/state" });
    }

    export function retrieve(assembly: string, culture: string, filter: string): Promise<AssemblyResult> {
        return ajaxPost<AssemblyResult>({ url: `~/api/translation/retrieve?assembly=${assembly}&culture=${culture}&filter=${filter}` }, undefined);
    }

    export function namespaceStatus(assembly: string, culture: string): Promise<Array<NamespaceSyncStats>> {
        return ajaxGet<Array<NamespaceSyncStats>>({ url: `~/api/translation/syncStats?assembly=${assembly}&culture=${culture}` });
    }

    export function sync(assembly: string, culture: string, namespace?: string): Promise<AssemblyResult> {
        return ajaxPost<AssemblyResult>({ url: `~/api/translation/sync?assembly=${assembly}&culture=${culture}&namespace=${namespace || ""}` }, undefined);
    }

    export function save(assembly: string, culture: string, result: AssemblyResult): Promise<void> {
        return ajaxPost<void>({ url: `~/api/translation/save?assembly=${assembly}&culture=${culture}` }, result);
    }

    export function pluralize(culture: string, singular: string): Promise<string> {
        return ajaxPost<string>({ url: `~/api/translation/pluralize?culture=${culture}` }, singular);
    }

    export function gender(culture: string, singular: string): Promise<string> {
        return ajaxPost<string>({ url: `~/api/translation/gender?culture=${culture}` }, singular);
    }
}

export interface NamespaceSyncStats {
    namespace: string;
    types: number;
    translations: number;
}

export interface TranslationFileStatus {
    assembly: string;
    culture: string;
    isDefault: boolean;
    status: TranslatedSummaryState;
}

export type TranslatedSummaryState = "Completed" | "Pending" | "None";

export interface AssemblyResult {
    totalTypes: number;
    cultures: {
        [cultureName: string]: {
            name: string;
            pronoms: { Gender: string; Singular: string; Plural: string }[];
        }
    }
    types: { [typeName: string]: LocalizableType };
}

export interface LocalizableType {
    type: string;
    hasMembers: boolean;
    hasGender: boolean;
    hasDescription: boolean;
    hasPluralDescription: boolean;
    cultures: { [culture: string]: LocalizedType };
}

export interface LocalizedType {
    culture: string;
    typeDescription?: LocalizedDescription;
    members: { [member: string]: LocalizedMember };
}

export interface LocalizedDescription {
    gender: string;
    description: string;
    pluralDescription: string;
    translatedDescription: string;
}

export interface LocalizedMember {
    name: string;
    description: string;
    translatedDescription: string;
}
