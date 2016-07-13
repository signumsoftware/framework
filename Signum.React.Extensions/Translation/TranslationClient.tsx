import * as React from 'react'
import { Route } from 'react-router'
import { ajaxPost, ajaxGet } from '../../../Framework/Signum.React/Scripts/Services';
import { EntitySettings } from '../../../Framework/Signum.React/Scripts/Navigator'
import { Entity, Lite } from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as Navigator from '../../../Framework/Signum.React/Scripts/Navigator'
import { EntityOperationSettings } from '../../../Framework/Signum.React/Scripts/Operations'
import * as Operations from '../../../Framework/Signum.React/Scripts/Operations'
import { CultureInfoEntity } from '../Basics/Signum.Entities.Basics'
import { TranslationPermission } from './Signum.Entities.Translation'
import * as AuthClient from '../Authorization/AuthClient'
import * as OmniboxClient from '../Omnibox/OmniboxClient'

export function start(options: { routes: JSX.Element[] }) {

    OmniboxClient.registerSpecialAction({
        allowed: () => AuthClient.isPermissionAuthorized(TranslationPermission.TranslateCode),
        key: "TranslateCode",
        onClick: () => Promise.resolve(Navigator.currentHistory.createHref("~/translation/status"))
    });

    options.routes.push(
        <Route path="translation" >
            <Route path="status" getComponent= {(loc, cb) => require(["./Code/TranslationCodeStatus"], (Comp) => cb(undefined, Comp.default)) }/>
            <Route path="view/:assembly(/:culture)" getComponent= {(loc, cb) => require(["./Code/TranslationCodeView"], (Comp) => cb(undefined, Comp.default)) }/>
            <Route path="sync/:assembly/:culture" getComponent= {(loc, cb) => require(["./Code/TranslationCodeSync"], (Comp) => cb(undefined, Comp.default)) }/>
        </Route>
    );
}


export module API {
    export function status(): Promise<TranslationFileStatus[]> {
        return ajaxGet<TranslationFileStatus[]>({ url: "~/api/translation/state" });
    }

    export function retrieve(assembly: string, culture: string, filter: string): Promise<AssemblyResult> {
        return ajaxPost<AssemblyResult>({ url: `~/api/translation/retrieve?assembly=${assembly}&culture=${culture}&filter=${filter}` }, undefined);
    }

    export function sync(assembly: string, culture: string): Promise<AssemblyResult> {
        return ajaxPost<AssemblyResult>({ url: `~/api/translation/sync?assembly=${assembly}&culture=${culture}` }, undefined);
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

export interface TranslationFileStatus {
    assembly: string;
    culture: string;
    isDefault: boolean;
    status: TranslatedSummaryState;
}

export type TranslatedSummaryState = "Completed" | "Pending" | "None";

export interface AssemblyResult {
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
    gender: string;
    description: string;
    pluralDescription: string;
    translatedDescription: string;
    members: { [member: string]: LocalizedMember };
}

export interface LocalizedMember {
    name: string;
    description: string;
    translatedDescription: string;
}
