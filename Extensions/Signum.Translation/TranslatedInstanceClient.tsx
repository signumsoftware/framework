import * as React from 'react'
import { RouteObject } from 'react-router'
import { ajaxPost, ajaxGet, ajaxGetRaw, saveFile } from '@framework/Services';
import * as AppContext from '@framework/AppContext';
import { TranslationPermission, TranslationMessage } from './Signum.Translation'
import { AuthClient } from '../Signum.Authorization/AuthClient'
import * as OmniboxSpecialAction from '@framework/OmniboxSpecialAction'
import { ImportComponent } from '@framework/ImportComponent'
import { QueryString } from '@framework/QueryString';
import { Lite, Entity, ModifiableEntity } from '@framework/Signum.Entities';
import { CultureClient } from '@framework/Basics/CultureClient'
import { TranslationClient } from './TranslationClient';
import { Binding, TextAreaLineController, TextBoxLineController, TextBoxLineProps, tasks } from '@framework/Lines';
import { LineBaseController, LineBaseProps } from '@framework/Lines/LineBase';
import { classes } from '@framework/Globals';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { getLambdaMembers } from '@framework/Reflection';
import { TranslatedSummaryState, MatchTranslatedInstances } from './Signum.Translation.Instances';
import { TranslatableRouteType } from '@framework/Signum.Basics';
import { TextAreaLineProps } from '../../Signum/React/Lines/TextAreaLine';

export namespace TranslatedInstanceClient {
  
  export function start(options: { routes: RouteObject[] }): void {
  
    OmniboxSpecialAction.registerSpecialAction({
      allowed: () => AppContext.isPermissionAuthorized(TranslationPermission.TranslateInstances),
      key: "TranslateInstances",
      onClick: () => Promise.resolve("/translatedInstance/status")
    });
  
  
    options.routes.push(
      { path: "/translatedInstance/status", element: <ImportComponent onImport={() => import("./Instances/TranslatedInstanceStatus")} /> },
      { path: "/translatedInstance/view/:type/:culture?", element: <ImportComponent onImport={() => import("./Instances/TranslatedInstanceView")} /> },
      { path: "/translatedInstance/sync/:type/:culture", element: <ImportComponent onImport={() => import("./Instances/TranslatedInstanceSync")} /> },
    );
  }
  
  tasks.push(taskSetTranslatableIcon)
  export function taskSetTranslatableIcon(lineBase: LineBaseController<LineBaseProps, unknown>, state: LineBaseProps): void {
    if (lineBase instanceof TextBoxLineController || lineBase instanceof TextAreaLineController) {
      const vProps = lineBase instanceof TextBoxLineController ? state as TextBoxLineProps : state as TextAreaLineProps;
  
      if (state.ctx.propertyRoute &&
        state.ctx.propertyRoute.propertyRouteType == "Field" &&
        state.ctx.propertyRoute.member!.translatable && 
        AppContext.isPermissionAuthorized(TranslationPermission.TranslateInstances)) {
        if (!vProps.extraButtons)
          vProps.extraButtons = vlc => <TranslateButton controller={lineBase instanceof TextBoxLineController ? vlc as TextBoxLineController : vlc as TextAreaLineController} />;
  
        if (!vProps.helpText) {
          var binding = (vProps.ctx.binding as Binding<string>);
          var value = binding.parentObject[binding.member + "_translated"];
          if (value != null)
            vProps.helpText = <><strong>{CultureClient.currentCulture.name}:</strong> {value}</>;
        }
      }
    }
  }

  export function TranslateButton(p: { controller: TextBoxLineController | TextAreaLineController }): React.JSX.Element {
  
    var ctx = p.controller.props.ctx.tryFindRootEntity();
  
    return (
      <a href="#" className={classes("sf-line-button sf-view", "btn input-group-text", "sf-translate-button")}
        onClick={e => {
          e.preventDefault();
  
          const url =
            ctx == null ? `/translatedInstance/status/` :
            (ctx.value as Entity).id == null ? `/translatedInstance/view/${ctx.value.Type}/` :
              `/translatedInstance/view/${ctx.value.Type}/?filter=${ctx.value.Type};${(ctx.value as Entity).id}`;
  
          window.open(AppContext.toAbsoluteUrl(url));
        }}
        title={p.controller.props.ctx.titleLabels ? TranslationMessage.ThisFieldIsTranslatable.niceToString() : undefined}>
        <FontAwesomeIcon icon="language" />
      </a>
    );
  }
  

  
  export namespace API {
  
    export function status(): Promise<TranslatedTypeSummary[]> {
      return ajaxGet({ url: "/api/translatedInstance" });
    }
  
    export function downloadView(type: string, culture: string | undefined): void {
      ajaxGetRaw({ url: `/api/translatedInstance/viewFile/${type}?${QueryString.stringify({ culture })}` })
        .then(response => saveFile(response));
    }
  
    export function downloadSync(type: string, culture: string | undefined): void {
      ajaxGetRaw({ url: `/api/translatedInstance/syncFile/${type}?${QueryString.stringify({ culture })}` })
        .then(response => saveFile(response));
    }
  
    export function uploadFile(request: FileUpload, mode: MatchTranslatedInstances): Promise<void> {
      return ajaxPost({ url: "/api/translatedInstance/uploadFile?" + QueryString.stringify({mode}) }, request);
    }
  
    export interface FileUpload {
      fileName: string;
      content: string;
    }
  
    export function viewTranslatedInstanceData(type: string, culture: string | undefined, filter: string | undefined): Promise<TranslatedInstanceViewType> {
      return ajaxGet({ url: `/api/translatedInstance/view/${type}?${QueryString.stringify({ culture, filter })}` });
    }
  
    export function syncTranslatedInstance(type: string, culture: string): Promise<TypeInstancesChanges> {
      return ajaxGet({ url: `/api/translatedInstance/sync/${type}?${QueryString.stringify({ culture })}` });
    }
  
    export function autoTranslate(type: string, culture: string): Promise<void> {
      return ajaxGet({ url: `~/api/translatedInstance/autoTranslate/${type}?${QueryString.stringify({ culture })}` });
    }
  
    export function autoTranslateAll(culture: string): Promise<void> {
      return ajaxGet({ url: `~/api/translatedInstance/autoTranslateAll?${QueryString.stringify({ culture })}` });
    }
  
    export function saveTranslatedInstanceData(records: TranslationRecord[], type: string, isSync: boolean, culture?: string | undefined): Promise<void> {
      return ajaxPost({ url: `/api/translatedInstance/save/${type}?${QueryString.stringify({ isSync, culture })}` }, records);
    }
  
  }
  
  export interface TranslationRecord {
    culture: string;
    propertyRoute: string;
    rowId?: string;
    lite: Lite<Entity>;
    originalText: string;
    translatedText: string;
  }
  
  export interface TranslatedTypeSummary {
    type: string;
    culture: string;
    state: TranslatedSummaryState;
    isDefaultCulture: boolean;
  }
  
  export interface TypeInstancesChanges {
    type: string;
    masterCulture: string;
    totalInstances: number;
    routes: { [propertyRoute: string]: TranslatableRouteType }
    instances: InstanceChange[];
    deletedTranslations: number;
  }
  
  export interface InstanceChange {
    instance: Lite<Entity>;
    routeConflicts: { [prAndRowId: string]: PropertyChange }
  }
  
  export interface PropertyChange {
    translatedText: string;
    support: { [culture: string]: PropertyRouteConflic };
  }
  
  export interface PropertyRouteConflic {
    oldOriginal?: string;
    oldTranslation?: string;
    original: string;
    automaticTranslations: TranslationClient.AutomaticTranslation[];
  }
  
  export interface TranslatedInstanceViewType {
    type: string;
    masterCulture: string;
    routes: { [propertyRoute: string]: TranslatableRouteType }
    instances: TranslatedInstanceView[];
  }
  
  export interface TranslatedInstanceView {
    lite: Lite<Entity>;
    master: { [prAndRowId: string]: string };
    translations: {
      [prAndRowId: string]: { [culture: string]: TranslatedPairView }
    }
  }
  
  export interface TranslatedPairView {
    translatedText: string;
    newText: string;
    originalText: string;
  }
}

declare module '@framework/Reflection' {

  export interface MemberInfo {
    translatable: boolean;
  }
}
