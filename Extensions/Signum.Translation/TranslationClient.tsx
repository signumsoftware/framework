import * as React from 'react'
import { RouteObject } from 'react-router'
import { ajaxPost, ajaxGet, ajaxGetRaw } from '@framework/Services';
import { TranslationPermission } from './Signum.Translation'
import { AuthClient } from '../Signum.Authorization/AuthClient'
import * as OmniboxSpecialAction from '@framework/OmniboxSpecialAction'
import { ImportComponent } from '@framework/ImportComponent'
import { TranslatedSummaryState } from './Signum.Translation.Instances';
import { isPermissionAuthorized } from '@framework/AppContext';
import { ChangeLogClient } from '@framework/Basics/ChangeLogClient';

export namespace TranslationClient {
  
  export function start(options: { routes: RouteObject[] }): void {
  
    ChangeLogClient.registerChangeLogModule("Signum.Translation", () => import("./Changelog"));
  
    OmniboxSpecialAction.registerSpecialAction({
      allowed: () => isPermissionAuthorized(TranslationPermission.TranslateCode),
      key: "TranslateCode",
      onClick: () => Promise.resolve("/translation/status")
    });
  
    options.routes.push(
      { path: "/translation/status", element: <ImportComponent onImport={() => import("./Code/TranslationCodeStatus")} /> },
      { path: "/translation/view/:assembly/:culture?", element: <ImportComponent onImport={() => import("./Code/TranslationCodeView")} /> },
      { path: "/translation/syncNamespaces/:assembly/:culture", element: <ImportComponent onImport={() => import("./Code/TranslationCodeSyncNamespaces")} /> },
      { path: "/translation/sync/:assembly/:culture/:namespace?", element: <ImportComponent onImport={() => import("./Code/TranslationCodeSync")} /> }
    );
  }
  
  
  export module API {
    export function status(): Promise<TranslationFileStatus[]> {
      return ajaxGet({ url: "/api/translation/state" });
    }
  
    export function retrieve(assembly: string, culture: string, filter: string): Promise<AssemblyResult> {
      return ajaxGet({ url: `/api/translation/retrieve?assembly=${assembly}&culture=${culture}&filter=${filter}` });
    }
  
    export function download(assembly: string, culture: string): Promise<Response> {
      return ajaxGetRaw({ url: `/api/translation/download?assembly=${assembly}&culture=${culture}` });
    }
  
    export function namespaceStatus(assembly: string, culture: string): Promise<Array<NamespaceSyncStats>> {
      return ajaxGet({ url: `/api/translation/syncStats?assembly=${assembly}&culture=${culture}` });
    }
  
    export function sync(assembly: string, culture: string, namespace?: string): Promise<AssemblyResult> {
      return ajaxPost({ url: `/api/translation/sync?assembly=${assembly}&culture=${culture}&namespace=${namespace || ""}` }, undefined);
    }
  
    export function save(assembly: string, culture: string, result: AssemblyResult): Promise<void> {
      return ajaxPost({ url: `/api/translation/save?assembly=${assembly}&culture=${culture}` }, result);
    }
  
    export function autoTranslate(assembly: string, culture: string): Promise<void> {
      return ajaxGet({ url: `~/api/translation/autoTranslate?assembly=${assembly}&culture=${culture}` });
    }

    export function autoTranslateAll(culture: string): Promise<void> {
      return ajaxGet({ url: `~/api/translation/autoTranslateAll?culture=${culture}` });
    }

    export function pluralize(culture: string, singular: string): Promise<string> {
      return ajaxPost({ url: `/api/translation/pluralize?culture=${culture}` }, singular);
    }
  
    export function gender(culture: string, singular: string): Promise<string> {
      return ajaxPost({ url: `/api/translation/gender?culture=${culture}` }, singular);
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
  
  export interface AssemblyResult {
    totalTypes: number;
    cultures: {
      [cultureName: string]: {
        name: string;
        pronoms: {
          gender: string;
          singular: string;
          plural: string
        }[];
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
    gender?: string;
    description?: string;
    pluralDescription?: string;
    automaticTranslations: AutomaticTypeTranslation[];
  }
  
  export interface AutomaticTypeTranslation {
    translatorName: string;
    gender?: string;
    singular: string;
    plural: string;
  }
  
  export interface LocalizedMember {
    name: string;
    description?: string;
    automaticTranslations: AutomaticTranslation[];
  }
  
  export interface AutomaticTranslation {
    translatorName: string;
    text: string;
  }
}


