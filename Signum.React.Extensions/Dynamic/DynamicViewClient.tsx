import * as React from 'react'
import * as QueryString from 'query-string'
import { globalModules } from './View/GlobalModules'
import { ajaxGet } from '@framework/Services';
import * as Search from '@framework/Search'
import { ValueSearchControlLine } from '@framework/Search'
import { EntitySettings, ViewPromise } from '@framework/Navigator'
import * as Navigator from '@framework/Navigator'
import { EntityOperationSettings } from '@framework/Operations'
import * as Operations from '@framework/Operations'
import { TypeContext } from '@framework/TypeContext'
import { isTypeEntity, getTypeInfo, PropertyRoute } from '@framework/Reflection'
import { Entity, ModifiableEntity } from '@framework/Signum.Entities'
import SelectorModal from '@framework/SelectorModal'
import { ViewReplacer } from '@framework/Frames/ReactVisitor';
import * as Lines from '@framework/Lines'
import * as FileLineModule from '../Files/FileLine'
import { DynamicViewEntity, DynamicViewSelectorEntity, DynamicViewOverrideEntity, DynamicViewMessage, DynamicViewOperation, DynamicViewSelectorOperation } from './Signum.Entities.Dynamic'
import DynamicViewEntityComponent from './View/DynamicView' //Just Typing
import * as DynamicClientOptions from './DynamicClientOptions'
import * as Nodes from './View/Nodes' //Typings-only
import * as NodeUtils from './View/NodeUtils' //Typings-only
import MessageModal from "@framework/Modals/MessageModal";
import { Dic } from "@framework/Globals";
import * as Components from "@framework/Components";

export function start(options: { routes: JSX.Element[] }) {

  Navigator.addSettings(new EntitySettings(DynamicViewEntity, w => import('./View/DynamicView')));
  Navigator.addSettings(new EntitySettings(DynamicViewSelectorEntity, w => import('./View/DynamicViewSelector')));
  Navigator.addSettings(new EntitySettings(DynamicViewOverrideEntity, w => import('./View/DynamicViewOverride')));

  DynamicClientOptions.Options.onGetDynamicLineForType.push((ctx, type) => <ValueSearchControlLine ctx={ctx} findOptions={{
    queryName: DynamicViewEntity,
    parentToken: DynamicViewEntity.token(a => a.entityType!.cleanName),
    parentValue: type
  }} />);

  DynamicClientOptions.Options.onGetDynamicLineForType.push((ctx, type) => <ValueSearchControlLine ctx={ctx} findOptions={{
    queryName: DynamicViewSelectorEntity,
    parentToken: DynamicViewSelectorEntity.token(a => a.entityType!.cleanName),
    parentValue: type
  }} />);

  Operations.addSettings(new EntityOperationSettings(DynamicViewOperation.Save, {
    onClick: ctx => {
      (ctx.frame.entityComponent as DynamicViewEntityComponent).beforeSave();
      cleanCaches();
      ctx.defaultClick();
    }
  }));

  Operations.addSettings(new EntityOperationSettings(DynamicViewOperation.Delete, {
    onClick: ctx => {
      cleanCaches();
      ctx.defaultClick();
    },
    contextual: { onClick: ctx => { cleanCaches(); ctx.defaultContextualClick(); } },
    contextualFromMany: { onClick: ctx => { cleanCaches(); ctx.defaultContextualClick(); } },
  }));

  Operations.addSettings(new EntityOperationSettings(DynamicViewSelectorOperation.Save, {
    onClick: ctx => {
      cleanCaches();
      ctx.defaultClick();
    }
  }));

  Operations.addSettings(new EntityOperationSettings(DynamicViewSelectorOperation.Delete, {
    onClick: ctx => {
      cleanCaches();
      ctx.defaultClick();
    },
    contextual: { onClick: ctx => { cleanCaches(); ctx.defaultContextualClick(); } },
    contextualFromMany: { onClick: ctx => { cleanCaches(); ctx.defaultContextualClick(); } },
  }));

  Navigator.setViewDispatcher(new DynamicViewViewDispatcher());
}

export const registeredCustomContexts: { [name: string]: CustomContextSettings } = {};

interface CustomContextSettings {
  getTypeContext: (ctx: TypeContext<any>) => TypeContext<any> | undefined;
  getCodeContext: (ctx: NodeUtils.CodeContext) => NodeUtils.CodeContext;
  getPropertyRoute: (dn: NodeUtils.DesignerNode<Nodes.CustomContextNode>) => PropertyRoute;
}

export class DynamicViewViewDispatcher implements Navigator.ViewDispatcher {

  hasDefaultView(typeName: string): boolean {
    return true;
  }

  getViewNames(typeName: string): Promise<string[]> {
    const es = Navigator.getSettings(typeName);
    var staticViewNames = es && es.namedViews && Dic.getKeys(es.namedViews) || [];

    if (!isTypeEntity(typeName))
      return Promise.resolve(staticViewNames);

    return getDynamicViewNames(typeName).then(dynamicViewNames => [
      ...staticViewNames,
      ...dynamicViewNames
    ]);
  }


  getViewOverrides(typeName: string, viewName?: string): Promise<Navigator.ViewOverride<ModifiableEntity>[]> {
    const es = Navigator.getSettings(typeName);
    var staticViewOverrides = es && es.viewOverrides && es.viewOverrides.filter(a => a.viewName == viewName) || [];

    if (!isTypeEntity(typeName))
      return Promise.resolve(staticViewOverrides);

    return getDynamicViewOverrides(typeName).then(dvos => [
      ...staticViewOverrides,
      ...dvos.filter(dvo => dvo.entity.viewName == viewName).map(dvo => ({
        override: dvo.override,
        viewName: dvo.entity.viewName
      } as Navigator.ViewOverride<ModifiableEntity>))
    ])
  }

  getViewPromise(entity: ModifiableEntity, viewName?: string): ViewPromise<ModifiableEntity> {

    if (viewName == "STATIC")
      return this.static(entity);

    if (viewName == "NEW")
      return ViewPromise.flat(createDefaultDynamicView(entity.Type).then(dv => dynamicViewComponent(dv)));

    if (!isTypeEntity(entity.Type) || viewName != undefined)
      return this.fallback(entity, viewName);

    return ViewPromise.flat(getSelector(entity.Type).then(sel => {

      if (!sel)
        return this.fallback(entity);

      try {
        var viewName = sel(entity as Entity);

        if (viewName == "STATIC")
          return this.static(entity);

        if (viewName == "NEW")
          return ViewPromise.flat(createDefaultDynamicView(entity.Type).then(dv => dynamicViewComponent(dv)));

        if (viewName == "CHOOSE")
          return this.chooseViewName(entity, true);

        return ViewPromise.flat(API.getDynamicView(entity.Type, viewName).then(dv => dynamicViewComponent(dv)));
      } catch (error) {
        return MessageModal.showError("There was an error executing the DynamicViewSelector. Fallback to default").then(() => this.fallback(entity));
      }
    }));
  }

  getViewPromiseWithName(entity: ModifiableEntity, viewName: string) {
    const es = Navigator.getSettings(entity.Type);
    var namedView = es && es.namedViews && es.namedViews[viewName];

    if (namedView)
      return namedView.getViewPromise(entity).applyViewOverrides(entity.Type, viewName);

    return ViewPromise.flat(API.getDynamicView(entity.Type, viewName).then(dve => dynamicViewComponent(dve)));
  }



  fallback(entity: ModifiableEntity, viewName?: string): ViewPromise<ModifiableEntity> {

    if (viewName)
      return this.getViewPromiseWithName(entity, viewName);

    const settings = Navigator.getSettings(entity.Type);

    if (!settings || !settings.getViewPromise) {

      if (!isTypeEntity(entity.Type))
        return new ViewPromise(import('@framework/Lines/DynamicComponent'));

      return this.chooseViewName(entity, true);
    }

    return settings.getViewPromise(entity).applyViewOverrides(entity.Type);
  }

  static(entity: ModifiableEntity): ViewPromise<ModifiableEntity> {
    const es = Navigator.getSettings(entity.Type);

    if (!es)
      throw new Error(`No EntitySettings registered for ${entity.Type}`);

    if (!es.getViewPromise)
      throw new Error(`The EntitySettings registered for ${entity.Type} has not getViewPromise`);

    return es.getViewPromise(entity).applyViewOverrides(entity.Type);
  }

  chooseViewName(entity: ModifiableEntity, avoidMessage = false): ViewPromise<ModifiableEntity> {
    return ViewPromise.flat(this.getViewNames(entity.Type)
      .then(names => SelectorModal.chooseElement(names, {
        title: DynamicViewMessage.ChooseAView.niceToString(),
        message: avoidMessage ? undefined : DynamicViewMessage.SinceThereIsNoDynamicViewSelectorYouNeedToChooseAViewManually.niceToString(),
      })).then(viewName => {
        if (!viewName)
          return createDefaultDynamicView(entity.Type).then(dv => dynamicViewComponent(dv));

        return this.getViewPromiseWithName(entity, viewName);
      }));
  }

  getOrCreateDynamicView(typeName: string, viewName: string | undefined): Promise<DynamicViewEntity> {

    if (viewName == undefined)
      return createDefaultDynamicView(typeName);

    return API.getDynamicView(typeName, viewName);
  }

}

export function patchComponent(component: React.ComponentClass<{ ctx: TypeContext<Entity> }>, viewOverride: (e: ViewReplacer<Entity>) => void) {

  if (!component.prototype.render)
    throw new Error("render function not defined in " + component);

  if (component.prototype.render.isDynamic)
    return;

  const staticRender = component.prototype.render as (this: React.Component<any>) => React.ReactElement<any>;

  component.prototype.render = function (this: React.Component<any, any>) {

    const ctx = this.props.ctx;

    const view = staticRender.call(this);

    const replacer = new ViewReplacer<Entity>(view, ctx);
    try {
      viewOverride(replacer);
      return replacer.result;
    } catch (error) {
      return <div className="alert alert-danger">ERROR: {error && error.message}</div>;
    }
  };

  component.prototype.render.isDynamic = true;
  component.prototype.render.staticRender = staticRender;
}

export function unPatchComponent(component: React.ComponentClass<{ ctx: TypeContext<Entity> }>) {

  if (!component.prototype.render)
    throw new Error("render function not defined in " + component);

  if (!component.prototype.render.isDynamic)
    return;

  component.prototype.render = component.prototype.render.staticRender;
}

function getOrCreate<V>(cache: { [key: string]: V }, key: string, onCreate: (key: string) => Promise<V>): Promise<V> {

  if ((cache as Object).hasOwnProperty(key))
    return Promise.resolve(cache[key]);

  return onCreate(key).then(v => cache[key] = v);
}


export function cleanCaches() {
  Dic.clear(viewNamesCache);
  Dic.clear(selectorCache);
}

const viewNamesCache: { [typeName: string]: string[] } = {};
export function getDynamicViewNames(typeName: string): Promise<string[]> {

  return getOrCreate(viewNamesCache, typeName, () =>
    API.getDynamicViewNames(typeName)
  );
}

const selectorCache: { [typeName: string]: ((e: Entity) => string) | undefined } = {};
export function getSelector(typeName: string): Promise<((e: Entity) => string) | undefined> {

  return getOrCreate(selectorCache, typeName, () =>
    API.getDynamicViewSelector(typeName)
      .then(dvs => dvs && asSelectorFunction(dvs))
  );
}

export function asSelectorFunction(dvs: DynamicViewSelectorEntity): (e: Entity) => string {

  const code = "e => " + dvs.script!;

  try {
    return evalWithScope(code, globalModules);
  } catch (e) {
    throw new Error("Syntax in DynamicViewSelector for '" + dvs.entityType!.toStr + "':\r\n" + code + "\r\n" + (e as Error).message);
  }
}

function evalWithScope(code: string, modules: any) {
  return eval(code);
}

interface DynamiViewOverridePair {
  override: (vr: ViewReplacer<Entity>) => void;
  entity: DynamicViewOverrideEntity;
}

const overrideCache: { [typeName: string]: DynamiViewOverridePair[] } = {};
export function getDynamicViewOverrides(typeName: string): Promise<DynamiViewOverridePair[]> {
  return getOrCreate(overrideCache, typeName, () =>
    API.getDynamicViewOverride(typeName)
      .then(dvos => dvos.map(dvo => ({ entity: dvo, override: asOverrideFunction(dvo) }) as DynamiViewOverridePair))
  );
}


export function asOverrideFunction(dvo: DynamicViewOverrideEntity): (vr: ViewReplacer<Entity>) => string {
  let code = dvo.script!;

  // Lines
  var ValueLine = Lines.ValueLine;
  var EntityLine = Lines.EntityLine;
  var EntityCombo = Lines.EntityCombo;
  var EnumCheckboxList = Lines.EnumCheckboxList;
  var EntityCheckboxList = Lines.EntityCheckboxList;
  var EntityDetail = Lines.EntityDetail;
  var EntityList = Lines.EntityList;
  var EntityRepeater = Lines.EntityRepeater;
  var EntityTabRepeater = Lines.EntityTabRepeater;
  var EntityStrip = Lines.EntityStrip;
  var EntityTable = Lines.EntityTable;
  var FormGroup = Lines.FormGroup;
  var FormControlReadonly = Lines.FormControlReadonly;
  var FileLine = FileLineModule.default;

  // Search
  var SearchControl = Search.SearchControl;
  var SearchControlLoaded = Search.SearchControlLoaded;
  var ValueSearchControl = Search.ValueSearchControl;
  var ValueSearchControlLine = Search.ValueSearchControlLine;

  // Components
  var Button = Components.Button;
  var Dropdown = Components.Dropdown;
  var DropdownItem = Components.DropdownItem;
  var DropdownMenu = Components.DropdownMenu;
  var DropdownToggle = Components.DropdownToggle;
  var Fade = Components.ModalFade;
  var Modal = Components.Modal;
  var NavItem = Components.NavItem;
  var PopperContent = Components.PopperContent;
  var Tooltip = Components.Tooltip;
  var UncontrolledDropdown = Components.UncontrolledDropdown;
  var UncontrolledTooltip = Components.UncontrolledTooltip;
  var Tab = Components.Tab;
  var Tabs = Components.Tabs;
  var UncontrolledTabs = Components.UncontrolledTabs;
  var LinkContainer = Components.LinkContainer;


  var modules = globalModules;

  code = "(function(vr){ " + code + "})";

  try {
    return eval(code);
  } catch (e) {
    throw new Error("Syntax in DynamicViewOverride for '" + dvo.entityType!.toStr + "':\r\n" + code + "\r\n" + (e as Error).message);
  }
}

export function createDefaultDynamicView(typeName: string): Promise<DynamicViewEntity> {
  return loadNodes().then(nodes =>
    Navigator.API.getType(typeName).then(t => DynamicViewEntity.New({
      entityType: t,
      viewName: "My View",
      viewContent: JSON.stringify(nodes.NodeConstructor.createDefaultNode(getTypeInfo(typeName))),
    })));
}

export function loadNodes(): Promise<typeof Nodes> {
  return import("./View/Nodes");
}

export function getDynamicViewEntity(typeName: string, viewName: string): ViewPromise<ModifiableEntity> {

  return ViewPromise.flat(
    API.getDynamicView(typeName, viewName)
      .then(vn => new ViewPromise(import('./View/DynamicViewComponent')).withProps({ initialDynamicView: vn }))
  );
}

export function dynamicViewComponent(dynamicView: DynamicViewEntity): ViewPromise<ModifiableEntity> {
  return new ViewPromise(import('./View/DynamicViewComponent'))
    .withProps({ initialDynamicView: dynamicView });
}


export namespace API {

  export function getDynamicView(typeName: string, viewName: string): Promise<DynamicViewEntity> {
    return ajaxGet<DynamicViewEntity>({ url: `~/api/dynamic/view/${typeName}?` + QueryString.stringify({ viewName }) });
  }

  export function getDynamicViewSelector(typeName: string): Promise<DynamicViewSelectorEntity | undefined> {
    return ajaxGet<DynamicViewSelectorEntity>({ url: `~/api/dynamic/selector/${typeName}` });
  }

  export function getDynamicViewOverride(typeName: string): Promise<DynamicViewOverrideEntity[]> {
    return ajaxGet<DynamicViewOverrideEntity[]>({ url: `~/api/dynamic/override/${typeName}` });
  }

  export function getDynamicViewNames(typeName: string): Promise<string[]> {
    return ajaxGet<string[]>({ url: `~/api/dynamic/viewNames/${typeName}` });
  }

  export function getSuggestedFindOptions(typeName: string): Promise<SuggestedFindOptions[]> {
    return ajaxGet<SuggestedFindOptions[]>({ url: `~/api/dynamic/suggestedFindOptions/${typeName}` });
  }
}

export interface SuggestedFindOptions {
  queryKey: string;
  parentToken: string;
}

