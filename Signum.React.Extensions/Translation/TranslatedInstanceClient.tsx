import * as React from 'react'
import { ajaxPost, ajaxGet, ajaxGetRaw, saveFile } from '@framework/Services';
import * as AppContext from '@framework/AppContext';
import { TranslationPermission, TranslatedSummaryState, TranslateableRouteType, TranslationMessage } from './Signum.Entities.Translation'
import * as AuthClient from '../Authorization/AuthClient'
import * as OmniboxClient from '../Omnibox/OmniboxClient'
import { ImportRoute } from "@framework/AsyncImport";
import { QueryString } from '@framework/QueryString';
import { Lite, Entity, ModifiableEntity } from '@framework/Signum.Entities';
import * as CultureClient from './CultureClient'
import { DiffBlock } from '../DiffLog/DiffLogClient';
import { AutomaticTranslation } from './TranslationClient';
import { Binding, tasks } from '../../../Framework/Signum.React/Scripts/Lines';
import { LineBaseController, LineBaseProps } from '../../../Framework/Signum.React/Scripts/Lines/LineBase';
import { ValueLineController, ValueLineProps } from '../../../Framework/Signum.React/Scripts/Lines/ValueLine';
import { classes } from '../../../Framework/Signum.React/Scripts/Globals';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { getLambdaMembers } from '@framework/Reflection';

export function start(options: { routes: JSX.Element[] }) {

  OmniboxClient.registerSpecialAction({
    allowed: () => AuthClient.isPermissionAuthorized(TranslationPermission.TranslateInstances),
    key: "TranslateInstances",
    onClick: () => Promise.resolve("~/translatedInstance/status")
  });

  tasks.push(taskSetTranslatableIcon)

  options.routes.push(
    <ImportRoute path="~/translatedInstance/status" onImportModule={() => import("./Instances/TranslatedInstanceStatus")} />,
    <ImportRoute path="~/translatedInstance/view/:type/:culture?" onImportModule={() => import("./Instances/TranslatedInstanceView")} />,
    <ImportRoute path="~/translatedInstance/sync/:type/:culture" onImportModule={() => import("./Instances/TranslatedInstanceSync")} />,
  );
}

export function taskSetTranslatableIcon(lineBase: LineBaseController<any>, state: LineBaseProps) {
  if (lineBase instanceof ValueLineController) {
    const vProps = state as ValueLineProps;

    if (state.ctx.propertyRoute &&
      state.ctx.propertyRoute.propertyRouteType == "Field" &&
      state.ctx.propertyRoute.member!.translatable) {
      if (!vProps.extraButtons)
        vProps.extraButtons = vlc => <TranslateButton controller={lineBase} />;

      if (!vProps.helpText) {
        var binding = (vProps.ctx.binding as Binding<string>);
        var value = binding.parentObject[binding.member + "_translated"];
        if (value != null)
          vProps.helpText = <><strong>{CultureClient.currentCulture.name}:</strong> {value}</>;
      }
    }
  }
}

function TranslateButton(p: { controller: ValueLineController }) {

  var culture = CultureClient.currentCulture.name.tryBefore("-") ?? CultureClient.currentCulture.name;

  var ctx = p.controller.props.ctx.tryFindRootEntity();

  return (
    <a href="#" className={classes("sf-line-button sf-view", "btn input-group-text")}
      onClick={e => {
        e.preventDefault();

        const url =
          ctx == null ? `~/translatedInstance/status/` :
          (ctx.value as Entity).id == null ? `~/translatedInstance/view/${ctx.value.Type}/${culture}` :
            `~/translatedInstance/view/${ctx.value.Type}/${culture}?filter=${ctx.value.Type};${(ctx.value as Entity).id}`;

        window.open(AppContext.toAbsoluteUrl(url));
      }}
      title={p.controller.props.ctx.titleLabels ? TranslationMessage.ThisFieldIsTranslatable.niceToString() : undefined}>
      <FontAwesomeIcon icon="language" />
    </a>
  );
}

declare module '@framework/Reflection' {

  export interface MemberInfo {
    translatable: boolean;
  }
}

export module API {

  export function status(): Promise<TranslatedTypeSummary[]> {
    return ajaxGet({ url: "~/api/translatedInstance" });
  }

  export function downloadView(type: string, culture: string | undefined) {
    ajaxGetRaw({ url: `~/api/translatedInstance/viewFile/${type}?${QueryString.stringify({ culture })}` })
      .then(response => saveFile(response))
      .done();
  }

  export function downloadSync(type: string, culture: string | undefined) {
    ajaxGetRaw({ url: `~/api/translatedInstance/syncFile/${type}?${QueryString.stringify({ culture })}` })
      .then(response => saveFile(response))
      .done();
  }

  export function uploadFile(request: FileUpload): Promise<void> {
    return ajaxPost({ url: "~/api/translatedInstance/uploadFile/" }, request);
  }

  export interface FileUpload {
    fileName: string;
    content: string;
  }

  export function viewTranslatedInstanceData(type: string, culture: string | undefined, filter: string | undefined): Promise<TranslatedInstanceViewType> {
    return ajaxGet({ url: `~/api/translatedInstance/view/${type}?${QueryString.stringify({ culture, filter })}` });
  }

  export function syncTranslatedInstance(type: string, culture: string): Promise<TypeInstancesChanges> {
    return ajaxGet({ url: `~/api/translatedInstance/sync/${type}?${QueryString.stringify({ culture })}` });
  }

  export function saveTranslatedInstanceData(records: TranslationRecord[], type: string, culture?: string | undefined): Promise<void> {
    return ajaxPost({ url: `~/api/translatedInstance/save/${type}?${QueryString.stringify({ culture })}` }, records);
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
  routes: { [propertyRoute: string]: TranslateableRouteType }
  instances: InstanceChange[]
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
  diff?: DiffBlock;
  original: string;
  automaticTranslations: AutomaticTranslation[];
}

export interface TranslatedInstanceViewType {
  type: string;
  masterCulture: string;
  routes: { [propertyRoute: string]: TranslateableRouteType }
  instances: {
    lite: Lite<Entity>; 
    master: { [prAndRowId: string]: string };
    translations: {
      [prAndRowId: string]: { [culture: string]: TranslatedPairView }
    }
  }[]
}

export interface TranslatedPairView {
  translatedText: string;
  originalText: string;
  diff?: DiffBlock;
}
