import { IconProp } from "@fortawesome/fontawesome-svg-core";
import * as React from "react";
import * as AppContext from './AppContext';
import { BsColor, KeyNames } from "./Components/Basic";
import { Finder } from './Finder';
import { ButtonBarManager } from './Frames/ButtonBar';
import Notify from './Frames/Notify';
import { Dic } from './Globals';
import { FunctionalAdapter } from "./Modals";
import { Navigator } from './Navigator';
import { CellOperationButton, defaultCellOperationClick } from "./Operations/CellOperationButton";
import { ContextualOperations } from './Operations/ContextualOperations';
import { EntityOperations, OperationButton } from './Operations/EntityOperations';
import { MultiOperationProgressModal } from "./Operations/MultiOperationProgressModal";
import { ProgressModal, ProgressModalOptions } from "./Operations/ProgressModal";
import { QuickLinkClient, QuickLinkExplore } from "./QuickLinkClient";
import { getOperationInfo, getQueryKey, getTypeInfo, getTypeName, GraphExplorer, OperationInfo, OperationType, QueryTokenString, Type, TypeInfo } from './Reflection';
import { SearchControlLoaded } from "./Search";
import * as ContexualItems from './SearchControl/ContextualItems';
import { ContextualItemsContext, ContextualMenuItem } from './SearchControl/ContextualItems';
import { ajaxPost, ajaxPostRaw, WebApiHttpError } from './Services';
import { FilterOperation } from "./Signum.DynamicQuery";
import { EngineMessage, Entity, EntityPack, getToString, JavascriptMessage, Lite, OperationMessage, toLite } from './Signum.Entities';
import { ConstructSymbol_From, ConstructSymbol_FromMany, ConstructSymbol_Simple, DeleteSymbol, ExecuteSymbol, OperationLogEntity, OperationSymbol, PropertyOperation } from './Signum.Operations';
import { ButtonBarElement, ButtonsContext, EntityFrame, IOperationVisible, TypeContext } from './TypeContext';


export namespace Operations {

  export namespace Options {
    export function maybeReadonly(ti: TypeInfo): boolean {
      return false;
    }
  }

  export function start(): void {
    ButtonBarManager.onButtonBarRender.push(EntityOperations.getEntityOperationButtons);
    ContexualItems.onContextualItems.push(ContextualOperations.getOperationsContextualItems);

    AppContext.clearSettingsActions.push(clearOperationSettings);

    QuickLinkClient.registerGlobalQuickLink(entityType => Promise.resolve([new QuickLinkExplore(entityType, ctx => ({
      queryName: OperationLogEntity, filterOptions: [{ token: OperationLogEntity.token(e => e.target), value: ctx.lite }]
    }),
      {
        key: getQueryKey(OperationLogEntity),
        text: () => OperationLogEntity.nicePluralName(),
        isVisible: getTypeInfo(entityType) && getTypeInfo(entityType).operations && Finder.isFindable(OperationLogEntity, false),
        icon: "clock-rotate-left",
        iconColor: "green",
        color: "success",
      }
    )]));

    Finder.formatRules.push({
      name: "CellOperation",
      isApplicable: c => {
        return c.type.name == "CellOperationDTO";
      },
      formatter: c => new Finder.CellFormatter(
        (dto: Operations.API.CellOperationDto, ctx, cp) =>
          dto && dto.lite ? <CellOperationButton coc={(new CellOperationContext({ cellContext: ctx, text: cp.column.displayName, ...dto }))} /> : undefined,
        false)
    });

  }

  export const operationSettings: { [operationKey: string]: OperationSettings } = {};

  export function clearOperationSettings(): void {
    Dic.clear(operationSettings);
  }

  export function addSettings(...settings: OperationSettings[]): void {
    settings.forEach(s => Dic.addOrThrow(operationSettings, s.operationSymbol, s));
  }

  export function overrideEntitySettings<T extends Entity>(operation: ExecuteSymbol<T> | ConstructSymbol_From<T, any> | DeleteSymbol<T>, options: EntityOperationOptions<T>): void {
    var es = getSettings(operation) as EntityOperationSettings<T>;

    var { contextual, contextualFromMany, cell, ...otherOptions } = options;

    Dic.assign(es, otherOptions);

    if (contextual) {
      if (es.contextual == null)
        es.contextual = new ContextualOperationSettings(operation as any, contextual);
      else
        Dic.assign(es.contextual, contextual);
    }

    if (contextualFromMany) {
      if (es.contextualFromMany == null)
        es.contextualFromMany = new ContextualOperationSettings(operation as any, contextualFromMany);
      else
        Dic.assign(es.contextualFromMany, contextualFromMany);
    }

    if (cell) {
      if (es.cell == null)
        es.cell = new CellOperationSettings(operation as any, cell);
      else
        Dic.assign(es.cell, cell);
    }

  }


  export function getSettings(operation: OperationSymbol | string): OperationSettings | undefined {
    const operationKey = (operation as OperationSymbol).key || operation as string;

    return operationSettings[operationKey];
  }

  export function operationInfos(ti: TypeInfo): OperationInfo[] {
    return Dic.getValues(ti.operations!);
  }

  export function notifySuccess(message?: string, timeout?: number): void {
    Notify.singleton && Notify.singleton.notifyTimeout({ text: message ?? JavascriptMessage.executed.niceToString(), type: "success", priority: 20 }, timeout);
  }

  /**
   * Operation Settings
   */
  

  export function isShortcut(e: KeyboardEvent, ks: KeyboardShortcut): boolean {

    function toLower(a: string | undefined) {
      return a?.toLowerCase();
    }

    return (toLower(e.key) == toLower(ks.key)) &&
      e.ctrlKey == (ks.ctrlKey || false) &&
      e.altKey == (ks.altKey || false) &&
      e.shiftKey == (ks.shiftKey || false);
  }



  export function getShortcutToString(ks: KeyboardShortcut): string {

    return (ks.ctrlKey ? "Ctrl+" : "") +
      (ks.altKey ? "Alt+" : "") +
      (ks.shiftKey ? "Shift+" : "") +
      (ks.key);

  }

  export const CreateGroup: EntityOperationGroup = {
    key: "create",
    text: () => OperationMessage.Create.niceToString(),
    simplifyName: cs => {
      const array = new RegExp(OperationMessage.CreateFromRegex.niceToString()).exec(cs);
      return array ? array[1].firstUpper() : cs;
    },
    cssClass: "sf-operation",
    order: 200,
  };

  





  export namespace Defaults {

    export function isSave(oi: OperationInfo): boolean {
      return oi.operationType == "Execute" && oi.canBeModified == true && oi.key.endsWith(".Save");
    }

    export function defaultSetterConfig(coc: ContextualOperationContext<any>): SettersConfig {
      if (!coc.operationInfo.canBeModified)
        return "NoDialog";

      if (isSave(coc.operationInfo)) {
        if (coc.context.lites.length == 1)
          return "NoButton";

        return "Mandatory"
      }
      else {
        if (coc.context.lites.length == 1)
          return "NoDialog";

        return "Optional";
      }
    }

    export function getColor(oi: OperationInfo): BsColor {
      return oi.operationType == "Delete" ? "danger" :
        oi.operationType == "Execute" && Defaults.isSave(oi) ? "primary" : "secondary";
    }

    export function getOutline(oi: OperationInfo): boolean {
      return oi.operationType == "Delete" ? true :
        oi.operationType == "Execute" && Defaults.isSave(oi) ? false : true;
    }

    export function getIcon(oi: OperationInfo): IconProp | undefined {
      return oi.operationType == "Delete" ? "trash-alt" :
        oi.operationType == "Execute" && Defaults.isSave(oi) ? "save" : undefined;
    }

    export function getGroup(oi: OperationInfo): EntityOperationGroup | undefined {
      return oi.operationType == "ConstructorFrom" ? CreateGroup : undefined;
    }

    export function getKeyboardShortcut(oi: OperationInfo): KeyboardShortcut | undefined {
      return oi.operationType == "Delete" ? ({ ctrlKey: true, shiftKey: true, key: KeyNames.delete }) :
        oi.operationType == "Execute" && Defaults.isSave(oi) ? ({ ctrlKey: true, key: "s" }) : undefined;
    }

    export function getAlternatives<T extends Entity>(eoc: EntityOperationContext<T>): AlternativeOperationSetting<T>[] | undefined {
      if (Defaults.isSave(eoc.operationInfo)) {
        return [
          eoc.frame.onClose ? EntityOperations.andClose(eoc) : undefined,
          EntityOperations.andNew(eoc)
        ].notNull()
      }

      return undefined;
    }
  }

  export function isEntityOperation(operationType: OperationType): boolean {
    return operationType == "ConstructorFrom" ||
      operationType == "Execute" ||
      operationType == "Delete";
  }



  export namespace API {

    export function construct<T extends Entity>(type: string | Type<T>, operationKey: string | ConstructSymbol_Simple<T>, ...args: any[]): Promise<EntityPack<T> | undefined> {
      return ajaxPost({ url: "/api/operation/construct/" + getOperationKey(operationKey) }, { args, type: getTypeName(type) });
    }

    export function constructFromEntity<T extends Entity, F extends Entity>(entity: F, operationKey: string | ConstructSymbol_From<T, F>, ...args: any[]): Promise<EntityPack<T> | undefined> {
      GraphExplorer.propagateAll(entity, args);
      return ajaxPost({ url: "/api/operation/constructFromEntity/" + getOperationKey(operationKey) }, { entity: entity, args: args } as EntityOperationRequest);
    }

    export function constructFromLite<T extends Entity, F extends Entity>(lite: Lite<F>, operationKey: string | ConstructSymbol_From<T, F>, ...args: any[]): Promise<EntityPack<T> | undefined> {
      GraphExplorer.propagateAll(lite, args);
      return ajaxPost({ url: "/api/operation/constructFromLite/" + getOperationKey(operationKey) }, { lite: lite, args: args } as LiteOperationRequest);
    }

    export function constructFromMultiple<T extends Entity, F extends Entity>(lites: Lite<F>[], operationKey: string | ConstructSymbol_From<T, F>, options: MultiOperationOptions, ...args: any[]): Promise<ErrorReport> {
      GraphExplorer.propagateAll(lites, args);
      var abortController = options.abortController ?? new AbortController();
      return MultiOperationProgressModal.show(lites, operationKey, options.progressModal, abortController,
        () => ajaxPostRaw({ url: "/api/operation/constructFromMultiple/" + getOperationKey(operationKey) }, { lites: lites, setters: options.setters, args: args } as MultiOperationRequest));;
    }

    export function constructFromMany<T extends Entity, F extends Entity>(lites: Lite<F>[], operationKey: string | ConstructSymbol_FromMany<T, F>, ...args: any[]): Promise<EntityPack<T> | undefined> {
      GraphExplorer.propagateAll(lites, args);
      return ajaxPost({ url: "/api/operation/constructFromMany/" + getOperationKey(operationKey) }, { lites: lites, args: args } as MultiOperationRequest);
    }

    export function executeEntity<T extends Entity>(entity: T, operationKey: string | ExecuteSymbol<T>, ...args: any[]): Promise<EntityPack<T>> {
      GraphExplorer.propagateAll(entity, args);
      return ajaxPost({ url: "/api/operation/executeEntity/" + getOperationKey(operationKey) }, { entity: entity, args: args } as EntityOperationRequest);
    }

    export function executeLite<T extends Entity>(lite: Lite<T>, operationKey: string | ExecuteSymbol<T>, ...args: any[]): Promise<EntityPack<T>> {
      GraphExplorer.propagateAll(lite, args);
      return ajaxPost({ url: "/api/operation/executeLite/" + getOperationKey(operationKey) }, { lite: lite, args: args } as LiteOperationRequest);
    }

    export function executeLiteWithProgress<T extends Entity>(lite: Lite<T>, operationKey: string | ExecuteSymbol<T>, options: OperationWithProgressOptions, ...args: any[]): Promise<EntityPack<T>> {
      GraphExplorer.propagateAll(lite, args);
      var abortController = options.abortController ?? new AbortController();
      var modalOptions: ProgressModalOptions = {
        title: options.title ?? OperationMessage.Executing0.niceToString(getOperationInfo(operationKey, lite.EntityType).niceName),
        message: options.message ?? getToString(lite),
        showCloseWarningMessage: options.showCloseWarningMessage,
      }
      return ProgressModal.show(abortController, modalOptions,
        () => ajaxPostRaw({ url: "/api/operation/executeLiteWithProgress/" + getOperationKey(operationKey), signal: abortController.signal }, { lite: lite, args: args } as LiteOperationRequest)
      );
    }

    export function executeMultiple<T extends Entity>(lites: Lite<T>[], operationKey: string | ExecuteSymbol<T>, options: MultiOperationOptions, ...args: any[]): Promise<ErrorReport> {
      GraphExplorer.propagateAll(lites, args);
      var abortController = options.abortController ?? new AbortController();
      return MultiOperationProgressModal.show(lites, operationKey, options.progressModal, abortController,
        () => ajaxPostRaw({ url: "/api/operation/executeMultiple/" + getOperationKey(operationKey), signal: abortController.signal }, { lites: lites, setters: options.setters, args: args } as MultiOperationRequest)
      );
    }

    export function deleteEntity<T extends Entity>(entity: T, operationKey: string | DeleteSymbol<T>, ...args: any[]): Promise<void> {
      GraphExplorer.propagateAll(entity, args);
      return ajaxPost({ url: "/api/operation/deleteEntity/" + getOperationKey(operationKey) }, { entity: entity, args: args } as EntityOperationRequest);
    }

    export function deleteLite<T extends Entity>(lite: Lite<T>, operationKey: string | DeleteSymbol<T>, ...args: any[]): Promise<void> {
      GraphExplorer.propagateAll(lite, args);
      return ajaxPost({ url: "/api/operation/deleteLite/" + getOperationKey(operationKey) }, { lite: lite, args: args } as LiteOperationRequest);
    }

    export function deleteMultiple<T extends Entity>(lites: Lite<T>[], operationKey: string | DeleteSymbol<T>, options: MultiOperationOptions, ...args: any[]): Promise<ErrorReport> {
      GraphExplorer.propagateAll(lites, args);
      var abortController = options.abortController ?? new AbortController();
      return MultiOperationProgressModal.show(lites, operationKey, options.progressModal, abortController,
        () => ajaxPostRaw({ url: "/api/operation/deleteMultiple/" + getOperationKey(operationKey), signal: abortController.signal }, { lites: lites, setters: options.setters, args: args } as MultiOperationRequest)
      );
    }


    export interface CellOperationDto {
      lite: Lite<Entity>;
      operationKey: string;
      canExecute?: string
    }

    export interface ErrorReport {
      errors: { [liteKey: string]: string; }
    }

    export interface OperationResult {
      entity: Lite<Entity>;
      error: string;
    }

    export interface ProgressStep<T> {
      currentTask?: string;
      min?: number;
      max?: number;
      position?: number;
      result?: T;
      error?: WebApiHttpError;
      isFinished: boolean;
    }

    export function getOperationKey(operationKey: string | OperationSymbol): string {
      return (operationKey as OperationSymbol).key || operationKey as string;
    }

    export interface OperationWithProgressOptions {
      abortController?: AbortController;
      title?: React.ReactNode;
      message?: React.ReactNode;
      showCloseWarningMessage: boolean;
    }

    export interface MultiOperationOptions {
      progressModal?: boolean;
      setters?: PropertySetter[];
      abortController?: AbortController;
    }

    export interface MultiOperationRequest {
      type?: string;
      lites: Lite<Entity>[];
      args: any[];

      setters?: PropertySetter[]
    }

    export interface PropertySetter {
      property: string;
      operation?: PropertyOperation;
      filterOperation?: FilterOperation;
      value?: any;
      entityType?: string;
      predicate?: PropertySetter[];
      setters?: PropertySetter[];
    }

    export interface ConstructOperationRequest {
      type?: string;
      args: any[];
    }

    export interface EntityOperationRequest {
      entity: Entity;
      type?: string;
      args: any[];
    }

    export interface LiteOperationRequest {
      lite: Lite<Entity>;
      type?: string;
      args: any[];
    }


    export function stateCanExecutes<T extends Entity>(lites: Lite<T>[], operationKeys: string[]): Promise<CanExecutesResponse> {
      return ajaxPost({ url: "/api/operation/stateCanExecutes" }, { lites, operationKeys });
    }

    export interface CanExecutesResponse {
      canExecutes: { [operationKey: string]: string };
      isReadOnly?: boolean;
    }
  }
}

export abstract class OperationSettings {

  operationSymbol: string;

  constructor(operationSymbol: OperationSymbol | string) {
    this.operationSymbol = typeof operationSymbol == "string" ? operationSymbol : operationSymbol.key;
  }
}



/**
 * Constructor Operation Settings
 */
export class ConstructorOperationSettings<T extends Entity> extends OperationSettings {

  text?: (coc: ConstructorOperationContext<T>) => string;
  isVisible?: (coc: ConstructorOperationContext<T>) => boolean;
  onConstruct?: (coc: ConstructorOperationContext<T>, props?: Partial<T>) => Promise<EntityPack<T> | undefined> | undefined;

  constructor(operationSymbol: ConstructSymbol_Simple<T> | string, options: ConstructorOperationOptions<T>) {
    super(operationSymbol);

    Dic.assign(this, options);
  }
}

export interface ConstructorOperationOptions<T extends Entity> {
  text?: (coc: ConstructorOperationContext<T>) => string;
  isVisible?: (coc: ConstructorOperationContext<T>) => boolean;
  onConstruct?: (coc: ConstructorOperationContext<T>, props?: Partial<T>) => Promise<EntityPack<T> | undefined> | undefined;
}

export class ConstructorOperationContext<T extends Entity> {
  operationInfo: OperationInfo;
  settings: ConstructorOperationSettings<T>;
  typeInfo: TypeInfo;

  constructor(operationInfo: OperationInfo, settings: ConstructorOperationSettings<T>, typeInfo: TypeInfo) {
    this.operationInfo = operationInfo;
    this.settings = settings;
    this.typeInfo = typeInfo;
  }

  defaultConstruct(...args: any[]): Promise<EntityPack<T> | undefined> {
    return Operations.API.construct<T>(this.typeInfo.name, this.operationInfo.key, ...args);
  }

  assignProps(pack: EntityPack<T> | undefined, props?: Partial<T>): EntityPack<T> | undefined {
    if (pack && props)
      Dic.assign(pack.entity, props);

    return pack;
  }
}




export type SettersConfig = "NoButton" | "NoDialog" | "Optional" | "Mandatory";

/**
 * Contextual Operation Settings
 */
export class ContextualOperationSettings<T extends Entity> extends OperationSettings {
  text?: (coc: ContextualOperationContext<T>) => string;
  isVisible?: (coc: ContextualOperationContext<T>) => boolean;
  hideOnCanExecute?: boolean;
  showOnReadOnly?: boolean;
  confirmMessage?: (coc: ContextualOperationContext<T>) => React.ReactElement | string | undefined | null | true;
  createMenuItems?: (eoc: ContextualOperationContext<T>) => ContextualMenuItem[];
  onClick?: (coc: ContextualOperationContext<T>) => Promise<void>;
  settersConfig?: (coc: ContextualOperationContext<T>) => SettersConfig;
  color?: BsColor;
  icon?: IconProp | React.ReactElement;
  iconColor?: string;
  order?: number;

  constructor(operationSymbol: ConstructSymbol_FromMany<any, T> | string, options: ContextualOperationOptions<T>) {
    super(operationSymbol);

    Dic.assign(this, options);
  }
}

export interface ContextualOperationOptions<T extends Entity> {
  text?: (coc: ContextualOperationContext<T>) => string;
  isVisible?: (coc: ContextualOperationContext<T>) => boolean;
  hideOnCanExecute?: boolean;
  showOnReadOnly?: boolean;
  confirmMessage?: (coc: ContextualOperationContext<T>) => React.ReactElement | string | undefined | null | true;
  onClick?: (coc: ContextualOperationContext<T>) => Promise<void>;
  createMenuItems?: (eoc: ContextualOperationContext<T>) => ContextualMenuItem[];
  settersConfig?: (coc: ContextualOperationContext<T>) => SettersConfig;
  color?: BsColor;
  icon?: IconProp | React.ReactElement;
  iconColor?: string;
  order?: number;
}

export class ContextualOperationContext<T extends Entity> {
  context: ContextualItemsContext<T>
  operationInfo: OperationInfo;
  settings?: ContextualOperationSettings<T>;
  entityOperationSettings?: EntityOperationSettings<T>;
  pack?: EntityPack<T>; /*only for single contextual*/
  canExecute?: string;
  isReadonly?: boolean;
  color?: BsColor;
  icon?: IconProp | React.ReactElement;

  progressModalOptions?: Operations.API.OperationWithProgressOptions;
  event?: React.MouseEvent<any>;
  onContextualSuccess?: (pack: Operations.API.ErrorReport) => void;
  onConstructFromSuccess?: (pack: EntityPack<Entity> | undefined) => void;

  defaultClick(...args: any[]): Promise<void> {
    return ContextualOperations.defaultContextualOperationClick(this, ...args);
  }

  constructor(operationInfo: OperationInfo, context: ContextualItemsContext<T>) {

    let cos: ContextualOperationSettings<T> | undefined = undefined;
    let eos: EntityOperationSettings<T> | undefined = undefined;
    if (operationInfo.operationType == "ConstructorFromMany") {
      cos = Operations.getSettings(operationInfo.key) as ContextualOperationSettings<T> | undefined;
    }
    else {
      eos = Operations.getSettings(operationInfo.key) as EntityOperationSettings<T> | undefined;
      cos = eos == undefined ? undefined :
        context.lites.length == 1 ? eos.contextual : eos.contextualFromMany
    }

    this.operationInfo = operationInfo;
    this.context = context;
    this.settings = cos;
    this.entityOperationSettings = eos;
    this.color = cos?.color ?? eos?.color ?? Operations.Defaults.getColor(this.operationInfo);
    this.icon = cos?.icon ?? eos?.icon ?? Operations.Defaults.getIcon(this.operationInfo) as any;
  }

  getValueFromSearchControl<T = unknown>(token: QueryTokenString<T> | string, automaticEntityPrefix = true): Finder.AddToLite<T> | undefined {

    if (!(this.context.container instanceof SearchControlLoaded))
      throw new Error(`No SearchControl found`);

    return this.context.container.getSelectedValue(token, automaticEntityPrefix);
  }

  tryGetValueFromSearchControl<T = unknown>(token: QueryTokenString<T> | string, automaticEntityPrefix = true): { value: Finder.AddToLite<T> | undefined } | undefined {
    if (!(this.context.container instanceof SearchControlLoaded))
      throw new Error(`No SearchControl found`);

    return this.context.container.tryGetSelectedValue(token, automaticEntityPrefix);
  }

  isVisibleInContextualMenu(): boolean {

    const cos = this.settings;

    if (cos?.isVisible)
      return cos.isVisible(this);

    const oi = this.operationInfo;

    if (oi.operationType == "ConstructorFromMany")
      return true;

    if ((cos?.settersConfig ?? Operations.Defaults.defaultSetterConfig)(this) == "NoButton")
      return false;

    if (oi.operationType == "ConstructorFrom" && this.context.lites.length > 1 && !oi.resultIsSaved)
      return false;

    const eos = this.entityOperationSettings;
    if (eos) {
      if (eos.isVisible != null) //If you override isVisible in EntityOperationsettings you have to override in ContextualOperationSettings too
        return false;

      //for onClick, if you have onClick in EntityOperationsettings you have to add there also commonOnClick or add specific onClick in ContextualOperationSettings
      if (eos.onClick != null && cos?.onClick == null && (eos.commonOnClick == null || this.context.lites.length > 1))
        return false;
    }

    return true;
  }

  getEntity(): Promise<T> {

    if (this.pack != null)
      return Promise.resolve(this.pack.entity);

    if (this.context.lites.length == 1)
      return Navigator.API.fetch(this.context.lites[0]);

    throw new Error("Pack is not available for Contextual with many selected entities");
  }

  getLite(): Lite<T> {
    if (this.context.lites.length == 1)
      return this.context.lites[0];

    throw new Error("Pack is not available for Contextual with many selected entities");
  }

  createMenuItems(): ContextualMenuItem[] {

    if (this.settings?.createMenuItems)
      return this.settings.createMenuItems(this);

    return [{ fullText: this.operationInfo.niceName, menu: <ContextualOperations.OperationMenuItem coc={ this} /> } as ContextualMenuItem];
  }

  raiseEntityChanged(): void {
    return this.context.lites.map(l => l.EntityType).distinctBy().forEach(type => Navigator.raiseEntityChanged(type));
  }
}



export interface EntityOperationGroup {
  key: string;
  text: () => React.ReactNode; /*Delayed for authorization reasons, not culture */
  simplifyName?: (complexName: string) => string;
  cssClass?: string;
  color?: BsColor;
  outline?: boolean;
  order?: number;
}

/**
 * Cell Operation Settings, rendered in search control cells
 */
export class CellOperationSettings<T extends Entity> extends OperationSettings {
  text?: (coc: CellOperationContext<T>) => string;
  isVisible?: (coc: CellOperationContext<T>) => boolean;
  confirmMessage?: (coc: CellOperationContext<T>) => React.ReactElement | string | undefined | null | true;
  onClick?: (coc: CellOperationContext<T>) => Promise<void>;
  hideOnCanExecute?: boolean;
  //showOnReadOnly?: boolean;
  color?: BsColor;
  icon?: IconProp | React.ReactElement;
  iconColor?: string;
  iconAlign?: "start" | "end";
  outline?: boolean;
  classes?: string;
  overrideCanExecute?: (ctx: CellOperationContext<T>) => string | undefined | null;
  createButton?: (eoc: CellOperationContext<T>, group?: EntityOperationGroup) => ButtonBarElement[];

  constructor(operationSymbol: OperationSymbol | string, options: CellOperationOptions<T>) {
    super(operationSymbol);

    Dic.assign(this, options);
  }
}

export interface CellOperationOptions<T extends Entity> {
  text?: (coc: CellOperationContext<T>) => string;
  isVisible?: (coc: CellOperationContext<T>) => boolean;
  confirmMessage?: (coc: CellOperationContext<T>) => React.ReactElement | string | undefined | null | true;
  onClick?: (coc: CellOperationContext<T>) => Promise<void>;
  hideOnCanExecute?: boolean;
  //showOnReadOnly?: boolean;
  color?: BsColor;
  icon?: IconProp | React.ReactElement;
  iconColor?: string;
  iconAlign?: "start" | "end";
  outline?: boolean;
  classes?: string;
  overrideCanExecute?: (ctx: CellOperationContext<T>) => string | undefined | null;
  createButton?: (eoc: CellOperationContext<T>, group?: EntityOperationGroup) => ButtonBarElement[];
}

export class CellOperationContext<T extends Entity> {

  tag?: string;
  readonly lite: Lite<T>;
  readonly canExecute?: string;
  readonly operationKey: string;

  readonly operationInfo: OperationInfo;
  readonly cellContext: Finder.CellFormatterContext;
  readonly settings?: CellOperationSettings<T>;
  readonly entityOperationSettings?: EntityOperationSettings<any>;
  event?: React.MouseEvent<any>;
  progressModalOptions?: Operations.API.OperationWithProgressOptions;

  text?: string;
  color?: BsColor;
  icon?: IconProp | React.ReactElement;
  iconColor?: string;
  iconAlign?: "start" | "end";
  outline?: boolean;

  onExecuteSuccess?: (pack: EntityPack<T> | undefined) => void;
  onConstructFromSuccess?: (pack: EntityPack<Entity> | undefined) => void;
  onDeleteSuccess?: () => void;

  constructor(init: Partial<CellOperationContext<T>>) {
    Object.assign(this, init);
    this.lite = init.lite as Lite<T>;
    this.operationKey = init.operationKey!;
    this.canExecute = init.canExecute;
    this.operationInfo = getOperationInfo(this.operationKey!, this.lite.EntityType);
    this.cellContext = init.cellContext!;
    this.entityOperationSettings = Operations.getSettings(this.operationKey) as EntityOperationSettings<Entity>;
    this.settings = this.entityOperationSettings?.cell;
  }

  raiseEntityChanged(): void {
    return Navigator.raiseEntityChanged(this.lite.EntityType);
  }

  defaultClick(...args: any[]): Promise<void> {
    return defaultCellOperationClick(this, ...args);
  }

  getEntity(): Promise<T> {
    return Navigator.API.fetch(this.lite);
  }

  getLite(): Lite<T> {
    return this.lite;
  }

  isVisibleInCell(): boolean {

    const cos = this.settings;
    const eos = this.entityOperationSettings;

    const hideOnCanExecute = cos?.hideOnCanExecute ? eos?.hideOnCanExecute : false;
    if (hideOnCanExecute && this.canExecute)
      return false;

    if (cos?.isVisible)
      return cos.isVisible(this);

    if (eos) {
      if (eos.isVisible != null) //If you override isVisible in EntityOperationsettings you have to override in CellOperationSettings too
        return false;

      //for onClick, if you have onClick in EntityOperationsettings you have to add there also commonOnClick or add specific onClick in CellOperationSettings
      if (eos.onClick != null && eos.commonOnClick == null && cos?.onClick == null)
        return false;
    }

    return true;
  }

}


export class EntityOperationContext<T extends Entity> {


  static fromChildTypeContext<T extends Entity>(ctx: TypeContext<T>, operation: ExecuteSymbol<T> | DeleteSymbol<T> | ConstructSymbol_From<any, T> | string, canExecute: string | undefined): EntityOperationContext<T> | undefined {

    const operationKey = (operation as OperationSymbol).key || operation as string;
    const oi = getTypeInfo(ctx.value.Type).operations![operationKey];

    if (oi == null)
      return undefined;

    const result = new EntityOperationContext<T>(ctx.frame!, ctx.value, oi);
    result.settings = Operations.getSettings(operationKey) as EntityOperationSettings<T>;
    result.canExecute = canExecute ?? (ctx.value.isNew && !oi.canBeNew ? EngineMessage.TheEntity0IsNew.niceToString(getToString(ctx.value)) : undefined);
    result.complete();
    return result;
  }

  static fromTypeContext<T extends Entity>(ctx: TypeContext<T>, operation: ExecuteSymbol<T> | DeleteSymbol<T> | ConstructSymbol_From<any, T> | string): EntityOperationContext<T> | undefined {
    if (!ctx.frame)
      throw new Error("a frame is necessary");

    if (!ctx.frame.pack)
      throw new Error("a pack is necessary");

    if (ctx.frame.pack.entity != ctx.value)
      throw new Error("The ctx.value is not ctx.frame.pack.entity");

    return EntityOperationContext.fromEntityPack(ctx.frame, ctx.frame.pack! as EntityPack<T>, operation);
  }

  static fromEntityPack<T extends Entity>(frame: EntityFrame<any>, pack: EntityPack<T>, operation: ExecuteSymbol<T> | DeleteSymbol<T> | ConstructSymbol_From<any, T> | string): EntityOperationContext<T> | undefined {
    const operationKey = (operation as OperationSymbol).key || operation as string;

    const oi = getTypeInfo(pack.entity.Type).operations![operationKey];

    if (oi == null)
      return undefined;

    const result = new EntityOperationContext<T>(frame, pack.entity, oi);
    result.settings = Operations.getSettings(operationKey) as EntityOperationSettings<T>;
    result.canExecute = (pack?.canExecute && pack.canExecute[operationKey]) ?? (pack.entity.isNew && !oi.canBeNew ? EngineMessage.TheEntity0IsNew.niceToString(getToString(pack.entity)) : undefined);
    result.complete();
    return result;
  }

  frame: EntityFrame;
  tag?: string;
  entity: T;
  operationInfo: OperationInfo;
  settings?: EntityOperationSettings<T>;
  canExecute?: string;
  event?: React.MouseEvent<any>;

  progressModalOptions?: Operations.API.OperationWithProgressOptions;

  onExecuteSuccess?: (pack: EntityPack<T>) => Promise<void> | undefined;
  onExecuteSuccess_Default = async (pack: EntityPack<T>): Promise<void> => {
    this.frame.onReload(pack);
    if (pack?.entity.id != null)
      Navigator.raiseEntityChanged(pack.entity);
    Operations.notifySuccess();
  }

  onConstructFromSuccess?: (pack: EntityPack<Entity> | undefined) => Promise<void> | undefined;
  onConstructFromSuccess_Default = (pack: EntityPack<Entity> | undefined): Promise<void> => {
    Operations.notifySuccess();
    if (pack?.entity.id != null)
      Navigator.raiseEntityChanged(pack.entity);
    return Navigator.createNavigateOrTab(pack, this.event ?? ({} as React.MouseEvent));
  }

  onDeleteSuccess?: () => Promise<void> | undefined;
  onDeleteSuccess_Default = (): void => {
    this.frame.onClose?.();
    Navigator.raiseEntityChanged(this.entity.Type);
    Operations.notifySuccess();
  }

  color?: BsColor;
  icon?: IconProp | React.ReactElement;
  outline?: boolean;
  group?: EntityOperationGroup;
  keyboardShortcut?: KeyboardShortcut;
  alternatives?: AlternativeOperationSetting<T>[];

  constructor(frame: EntityFrame, entity: T, operationInfo: OperationInfo) {
    this.frame = frame;
    this.entity = entity;
    this.operationInfo = operationInfo;
  }

  getEntity(): Promise<T> {
    return Promise.resolve(this.entity);
  }

  getLite(): Lite<T> {
    return toLite(this.entity);
  }

  isVisibleInButtonBar(ctx: ButtonsContext): unknown {
    if (ctx.isOperationVisible && !ctx.isOperationVisible(this))
      return false;

    var ov = FunctionalAdapter.innerRef(ctx.frame.entityComponent) as IOperationVisible | null;
    if (ov?.isOperationVisible && !ov.isOperationVisible(this))
      return false;

    var eos = this.settings;
    if (eos?.isVisible && !eos.isVisible(this))
      return false;

    if (eos?.hideOnCanExecute && this.canExecute)
      return false;

    if (Navigator.isReadOnly(ctx.pack, { ignoreTypeIsReadonly: true }) && !(eos?.showOnReadOnly))
      return false;

    return true;
  }

  createButton(group?: EntityOperationGroup): ButtonBarElement[] {
    const s = this.settings;

    if (s?.createButton != null)
      return s.createButton(this, group);

    return [{
      order: s?.order ?? 0,
      shortcut: e => this.onKeyDown(e),
      button: <OperationButton eoc={this} group={group} />,
    }];
  }

  complete(): void {
    var s = this.settings;
    this.color = s?.color ?? Operations.Defaults.getColor(this.operationInfo);
    this.outline = s?.outline ?? Operations.Defaults.getOutline(this.operationInfo);
    this.icon = s?.icon ?? Operations.Defaults.getIcon(this.operationInfo) as any;
    this.group = s?.group !== undefined ? (s.group ?? undefined) : Operations.Defaults.getGroup(this.operationInfo);
    this.keyboardShortcut = s?.keyboardShortcut !== undefined ? (s.keyboardShortcut ?? undefined) : Operations.Defaults.getKeyboardShortcut(this.operationInfo);
    this.alternatives = s?.alternatives != null ? s.alternatives(this) : Operations.Defaults.getAlternatives(this);
  }

  defaultClick(...args: any[]): Promise<void> {
    return EntityOperations.defaultOnClick(this, ...args);
  }

  click(): Promise<void> {
    return this.frame.execute(() => {
      if (this.settings?.onClick)
        return this.settings.onClick(this);
      else if (this.settings?.commonOnClick)
        return this.settings.commonOnClick(this);
      else
        return EntityOperations.defaultOnClick(this);
    });
  }

  textOrNiceName(): string {
    return (this.settings?.text?.(this)) ?? this.operationInfo.niceName
  }

  onKeyDown(e: KeyboardEvent): boolean {
    if (this.keyboardShortcut) {
      if (Operations.isShortcut(e, this.keyboardShortcut)) {
        this.click();
        return true;
      }
    }

    if (this.alternatives != null) {

      for (var i = 0; i < this.alternatives.length; i++) {
        const a = this.alternatives[i];
        if (a.isVisible != false && a.keyboardShortcut) {
          if (Operations.isShortcut(e, a.keyboardShortcut)) {
            a.onClick(this);
            return true;
          }
        }
      }
    }

    return false;
  }
}

export interface AlternativeOperationSetting<T extends Entity> {
  name: string;
  text: string;
  color?: BsColor;
  classes?: string;
  icon?: IconProp | React.ReactElement;
  iconAlign?: "start" | "end";
  iconColor?: string;
  isVisible?: boolean;
  inDropdown?: boolean;
  isDefault?: boolean;
  confirmMessage?: (eoc: EntityOperationContext<T>) => string | undefined | null;
  onClick: (eoc: EntityOperationContext<T>) => Promise<void>;
  keyboardShortcut?: KeyboardShortcut;
}

export class EntityOperationSettings<T extends Entity> extends OperationSettings {

  contextual?: ContextualOperationSettings<T>;
  contextualFromMany?: ContextualOperationSettings<T>;
  cell?: CellOperationSettings<T>;

  text?: (coc: EntityOperationContext<T>) => string;
  isVisible?: (eoc: EntityOperationContext<T>) => boolean;
  isVisibleOnlyType?: (typeName: string) => boolean;
  confirmMessage?: (eoc: EntityOperationContext<T>) => React.ReactElement | string | undefined | null | true;
  overrideCanExecute?: (ctx: EntityOperationContext<T>) => string | undefined | null;
  onClick?: (eoc: EntityOperationContext<T>) => Promise<void>;
  commonOnClick?: (oc: EntityOperationContext<T> | ContextualOperationContext<T> | CellOperationContext<T>) => Promise<void>;
  createButton?: (eoc: EntityOperationContext<T>, group?: EntityOperationGroup) => ButtonBarElement[];
  hideOnCanExecute?: boolean;
  showOnReadOnly?: boolean;
  showOnSaveChangesModal?: boolean;
  group?: EntityOperationGroup | null;
  order?: number;
  color?: BsColor;
  outline?: boolean;
  classes?: string;
  icon?: IconProp | React.ReactElement;
  iconAlign?: "start" | "end";
  iconColor?: string;
  alternatives?: (ctx: EntityOperationContext<T>) => AlternativeOperationSetting<T>[];
  keyboardShortcut?: KeyboardShortcut | null;

  constructor(operationSymbol: ExecuteSymbol<T> | DeleteSymbol<T> | ConstructSymbol_From<any, T> | string, options: EntityOperationOptions<T>) {
    super(operationSymbol)

    Dic.assign(this, options);

    this.contextual = options.contextual ? new ContextualOperationSettings(operationSymbol as any, options.contextual) : undefined;
    this.contextualFromMany = options.contextualFromMany ? new ContextualOperationSettings(operationSymbol as any, options.contextualFromMany) : undefined;
    this.cell = options.cell ? new CellOperationSettings(operationSymbol as any, options.cell) : undefined;
  }
}

export interface EntityOperationOptions<T extends Entity> {
  contextual?: ContextualOperationOptions<T>;
  contextualFromMany?: ContextualOperationOptions<T>;
  cell?: CellOperationOptions<T>;

  text?: (coc: EntityOperationContext<T>) => string;
  isVisible?: (eoc: EntityOperationContext<T>) => boolean;
  isVisibleOnlyType?: (typeName: string) => boolean;
  overrideCanExecute?: (eoc: EntityOperationContext<T>) => string | undefined | null;
  confirmMessage?: (eoc: EntityOperationContext<T>) => React.ReactElement | string | undefined | null | true;
  onClick?: (eoc: EntityOperationContext<T>) => Promise<void>;
  commonOnClick?: (oc: EntityOperationContext<T> | ContextualOperationContext<T> | CellOperationContext<T>) => Promise<void>;
  createButton?: (eoc: EntityOperationContext<T>, group?: EntityOperationGroup) => ButtonBarElement[];
  hideOnCanExecute?: boolean;
  showOnReadOnly?: boolean;
  showOnSaveChangesModal?: boolean;
  group?: EntityOperationGroup | null;
  order?: number;
  color?: BsColor;
  outline?: boolean;
  classes?: string;
  icon?: IconProp | React.ReactElement;
  iconAlign?: "start" | "end";
  iconColor?: string;
  keyboardShortcut?: KeyboardShortcut | null;
  alternatives?: (eoc: EntityOperationContext<T>) => AlternativeOperationSetting<T>[];
}

export interface KeyboardShortcut {
  ctrlKey?: boolean;
  altKey?: boolean;
  shiftKey?: boolean;
  key?: string;
}

