import * as React from "react"
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { Entity, JavascriptMessage, OperationMessage, SearchMessage, Lite, getToString, liteKey } from '../Signum.Entities';
import { getTypeInfo, OperationType } from '../Reflection';
import { classes, softCast } from '../Globals';
import { Navigator } from '../Navigator';
import MessageModal from '../Modals/MessageModal'
import { ContextualItemsContext, MenuItemBlock } from '../SearchControl/ContextualItems';
import { Operations, ContextualOperationSettings, ContextualOperationContext, EntityOperationSettings } from '../Operations'
import { IconProp } from "@fortawesome/fontawesome-svg-core";
import { Dropdown, OverlayTrigger, Tooltip } from "react-bootstrap";
import { MultiPropertySetterModal, PropertySetterComponentProps } from "./MultiPropertySetter";
import { BsColor } from "../Components";
import SearchControlLoaded from "../SearchControl/SearchControlLoaded";
import { CollectionMessage } from "../Signum.External";


export namespace ContextualOperations {

  export async function getOperationsContextualItems(ctx: ContextualItemsContext<Entity>): Promise<MenuItemBlock | undefined> {
    if (ctx.lites.length == 0)
      return undefined;

    if (ctx.container instanceof SearchControlLoaded && ctx.container.state.resultFindOptions?.systemTime)
      return undefined;

    const types = ctx.lites.groupBy(coc => coc.EntityType);

    if (types.length != 1)
      return undefined;

    const ti = getTypeInfo(types[0].key);
    const contexts = Operations.operationInfos(ti)
      .filter(oi => Operations.isEntityOperation(oi.operationType) || oi.operationType == "ConstructorFromMany")
      .map(oi =>  new ContextualOperationContext<Entity>(oi, ctx))
      .filter(coc => coc.isVisibleInContextualMenu())
      .map(coc => coc!)
      .orderBy(coc => coc.settings && coc.settings.order);

    if (!contexts.length)
      return undefined;

    if (contexts.some(coc => coc.operationInfo.hasCanExecute || coc.operationInfo.hasCanExecuteExpression || coc.operationInfo.hasStates) || Operations.Options.maybeReadonly(ti)) {
      if (ctx.lites.length == 1) {
        var normal = contexts.filter(a => a.operationInfo.operationType != "ConstructorFromMany");
        if (normal.length) {
          var ep = await Navigator.API.fetchEntityPack(ctx.lites[0]);
          normal.forEach(coc => {
            if (!(coc.operationInfo.key in ep.canExecute)) //Not allowed
              contexts.remove(coc);
            else {
              coc.pack = ep;
              coc.canExecute = ep.canExecute[coc.operationInfo.key];
              coc.isReadonly = Navigator.isReadOnly(ep, { ignoreTypeIsReadonly: true });
            }
          });
        }

        var cfm = contexts.filter(a => a.operationInfo.operationType == "ConstructorFromMany");
        if (cfm.length) {
          const response = await Operations.API.stateCanExecutes(ctx.lites, cfm.filter(coc => coc.operationInfo.hasStates || coc.operationInfo.hasCanExecuteExpression).map(a => a.operationInfo.key))
          cfm.forEach(coc => {
            coc.canExecute = response.canExecutes[coc.operationInfo.key];
            coc.isReadonly = response.isReadOnly;
          });
        }

      } else /*if (ctx.lites.length > 1)*/ {
        const response = await Operations.API.stateCanExecutes(ctx.lites, contexts.filter(coc => coc.operationInfo.hasStates || coc.operationInfo.hasCanExecuteExpression).map(a => a.operationInfo.key))
        contexts.forEach(coc => {
          coc.canExecute = response.canExecutes[coc.operationInfo.key];
          coc.isReadonly = response.isReadOnly;
        });
      }
    } else {

      if (Navigator.isReadOnly(ti, { ignoreTypeIsReadonly: true })) {
        contexts.forEach(a => a.isReadonly = true);
      }
    }

    const menuItems = contexts
      .filter(coc => coc.canExecute == undefined || !hideOnCanExecute(coc))
      .filter(coc => !coc.isReadonly || showOnReadonly(coc))
      .orderBy(coc => coc.settings && coc.settings.order != undefined ? coc.settings.order :
        coc.entityOperationSettings && coc.entityOperationSettings.order != undefined ? coc.entityOperationSettings.order : 0)
      .flatMap(coc => coc.createMenuItems());

    if (menuItems.length == 0)
      return undefined;

    return {
      header: SearchMessage.Operations.niceToString(),
      menuItems: menuItems
    } as MenuItemBlock;
  }


  function hideOnCanExecute(coc: ContextualOperationContext<Entity>) {
    if (coc.settings && coc.settings.hideOnCanExecute != undefined)
      return coc.settings.hideOnCanExecute;

    if (coc.entityOperationSettings && coc.entityOperationSettings.hideOnCanExecute != undefined)
      return coc.entityOperationSettings.hideOnCanExecute;

    return false;
  }


  function showOnReadonly(coc: ContextualOperationContext<Entity>) {
    if (coc.settings && coc.settings.showOnReadOnly != undefined)
      return coc.settings.showOnReadOnly;

    if (coc.entityOperationSettings && coc.entityOperationSettings.showOnReadOnly != undefined)
      return coc.entityOperationSettings.showOnReadOnly;

    return coc.operationInfo.forReadonlyEntity ?? false;
  }


  export function confirmInNecessary(coc: ContextualOperationContext<Entity>): Promise<boolean> {

    const confirmMessage = getConfirmMessage(coc);

    if (confirmMessage == undefined)
      return Promise.resolve(true);

    return MessageModal.show({
      title: OperationMessage.Confirm.niceToString(),
      message: confirmMessage,
      buttons: "yes_no",
      icon: "warning",
      style: "warning",
    }).then(result => { return result == "yes"; });
  }

  function getConfirmMessage(coc: ContextualOperationContext<Entity>) {
    if (coc.settings && coc.settings.confirmMessage === null)
      return undefined;

    if (coc.settings && coc.settings.confirmMessage != undefined) {

      var result = coc.settings.confirmMessage(coc);
      if (result == true)
        return getDefaultConfirmMessage(coc);
      else
        return result;
    }


    if (defaultHasConfirmMessage(coc)) {
      return getDefaultConfirmMessage(coc);
    }

    return undefined;
  }

  function defaultHasConfirmMessage(coc: ContextualOperationContext<Entity>) {
    return coc.operationInfo.operationType == "Delete" ||
      coc.operationInfo.operationType != "ConstructorFromMany" && coc.context.lites.length > 1;
  }

  function getDefaultConfirmMessage(coc: ContextualOperationContext<Entity>) {

    if (coc.context.lites.length > 1) {
      var message = coc.context.lites
        .groupBy(a => a.EntityType)
        .map(gr => gr.elements.length + " " + (gr.elements.length == 1 ? getTypeInfo(gr.key).niceName : getTypeInfo(gr.key).nicePluralName))
        .joinComma(CollectionMessage.And.niceToString());

      if (coc.operationInfo.operationType == "Delete")
        return OperationMessage.PleaseConfirmYouWouldLikeToDelete0FromTheSystem.niceToString().formatHtml(<strong>{message}</strong>);
      else
        return OperationMessage.PleaseConfirmYouWouldLikeTo01.niceToString().formatHtml(<strong>{coc.operationInfo.niceName}</strong>, <strong>{message}</strong>);

    }
    else {
      var lite = coc.context.lites.single();
      if (coc.operationInfo.operationType == "Delete")
        return OperationMessage.PleaseConfirmYouWouldLikeToDelete0FromTheSystem.niceToString().formatHtml(<strong>{getToString(lite)} ({getTypeInfo(lite.EntityType).niceName} {lite.id})</strong>);
      else
        return OperationMessage.PleaseConfirmYouWouldLikeTo01.niceToString().formatHtml(<strong>{coc.operationInfo.niceName}</strong>, <strong>{getToString(lite)} ({getTypeInfo(lite.EntityType).niceName} {lite.id})</strong>);

    }
  }


  export interface OperationMenuItemProps {
    coc: ContextualOperationContext<any>;
    onOperationClick?: (coc: ContextualOperationContext<Entity>) => Promise<void>;
    onClick?: (me: React.MouseEvent<any>) => void; /*used to hide contextual menu*/
    extraButtons?: React.ReactNode;
    children?: React.ReactNode;
    color?: BsColor;
    icon?: IconProp | React.ReactElement;
    iconColor?: string;
  }

  export const OperationMenuItem: {
    (p: OperationMenuItemProps): React.ReactElement;
    getText: (coc: ContextualOperationContext<any>) => React.ReactNode;
    simplifyName: (niceName: string) => string;
  } = function OperationMenuItem({ coc, onOperationClick, onClick, extraButtons, color, icon, iconColor, children }: OperationMenuItemProps): React.ReactElement {
    const text = children ?? OperationMenuItem.getText(coc);

    const eos = coc.entityOperationSettings;

    if (color == null)
      color = coc.color;

    if (icon == null)
      icon = coc.icon;

    if (iconColor == null)
      iconColor = coc.settings?.iconColor || eos?.iconColor;

    const disabled = !!coc.canExecute;

    const resolvedOnClick = onOperationClick ?? coc.settings?.onClick ?? (coc.context.lites.length > 1 ? null : eos?.commonOnClick) ?? defaultContextualOperationClick;

    const handleOnClick = (me: React.MouseEvent<any>) => {
      coc.event = me;
      onClick?.(me);
      resolvedOnClick(coc);
    }

    const item = (
      <Dropdown.Item
        onClick={disabled ? undefined : handleOnClick}
        aria-disabled={disabled}
        disabled={disabled}
        style={{ pointerEvents: "initial" }}
        data-operation={coc.operationInfo.key}
        className={color && !disabled ? "text-" + color : undefined}>
        {icon ? (React.isValidElement(icon) ? icon:  <FontAwesomeIcon aria-hidden={true} icon={icon} className="fa-fw icon" color={iconColor} />) :
          color ? <span className={classes("icon", "empty-icon")}></span> : undefined}
        {text}
        {extraButtons}
      </Dropdown.Item>
    );

    if (!coc.canExecute)
      return item;

    return (
      <OverlayTrigger placement="right"
        overlay={<Tooltip id={coc.operationInfo.key + "_tooltip"}>{coc.canExecute}</Tooltip>} >
        {item}
      </OverlayTrigger >
    );
  }


  OperationMenuItem.getText = (coc: ContextualOperationContext<any>): React.ReactNode => {

    if (coc.settings && coc.settings.text)
      return coc.settings.text(coc);

    var cos = coc.settings;

    var multiSetter = coc.operationInfo.canBeModified && !((cos?.settersConfig ?? Operations.Defaults.defaultSetterConfig)(coc) == "NoDialog") ?
      <small className="ms-2">{OperationMessage.MultiSetter.niceToString()}</small> : null;

    return <>{OperationMenuItem.simplifyName(coc.operationInfo.niceName)}{multiSetter}</>;

  };

  OperationMenuItem.simplifyName = (niceName: string) => {
    const array = new RegExp(OperationMessage.CreateFromRegex.niceToString()).exec(niceName);
    return array ? OperationMessage.Create0.niceToString(array[1].firstUpper()) : niceName;
  }

  export function cloneWithPrototype<T extends object>(obj: T, overrides: Partial<T> = {}): T {
    const proto = Object.getPrototypeOf(obj);
    const clone = Object.create(proto);
    return Object.assign(clone, obj, overrides);
  }


  export async function defaultContextualOperationClick(coc: ContextualOperationContext<any>, ...args: any[]): Promise<void> {

    coc.event!.persist();

    const lites = coc.context.container instanceof SearchControlLoaded ?
      await coc.context.container.askAllLites(coc.context, coc.operationInfo.niceName) :
      coc.context.lites;

    if (lites == null)
      return;

    coc = cloneWithPrototype(coc, { context: { ...coc.context, lites } });

    var conf = await confirmInNecessary(coc);
    if (!conf)
      return;

    switch (coc.operationInfo.operationType) {
      case "ConstructorFromMany":
        {
          return Operations.API.constructFromMany(lites, coc.operationInfo.key, ...args)
            .then(coc.onConstructFromSuccess ?? (pack => {
              Operations.notifySuccess();
              if (pack?.entity.id != null)
                Navigator.raiseEntityChanged(pack.entity);
              Navigator.createNavigateOrTab(pack, coc.event!)
                .then(() => coc.context.markRows({}));
            }));
        }
      case "ConstructorFrom":
        if (lites.length == 1) {
          return Operations.API.constructFromLite(lites[0], coc.operationInfo.key, ...args)
            .then(coc.onConstructFromSuccess ?? (pack => {
              if (pack?.entity.id != null)
                Navigator.raiseEntityChanged(pack.entity);
              Operations.notifySuccess();
              return Navigator.createNavigateOrTab(pack, coc.event!)
                .then(() => coc.context.markRows({}))
            }));
        } else {

          var setters = await getSetters(coc);
          if (!setters)
            return;

          return Operations.API.constructFromMultiple(lites, coc.operationInfo.key, { setters }, ...args)
            .then(coc.onContextualSuccess ?? (report => {
              //Navigator.raiseEntityChanged(??);
              Operations.notifySuccess();
              coc.context.markRows(report.errors);
            }));
        }
      case "Execute":
        if (coc.progressModalOptions && lites.length == 1) {
          return Operations.API.executeLiteWithProgress(lites[0], coc.operationInfo.key, coc.progressModalOptions, ...args)
            .then(pack => softCast<Operations.API.ErrorReport>({ errors: {} })/*, error => softCast<API.ErrorReport>({ errors: { [liteKey(lites[0])]: (error as Error).message } })*/)
            .then(coc.onContextualSuccess ?? (report => {
              coc.raiseEntityChanged();
              Operations.notifySuccess();
              coc.context.markRows(report.errors);
            }));
        } else {
          var setters = await getSetters(coc);
          if (!setters)
            return;

          return Operations.API.executeMultiple(lites, coc.operationInfo.key, { setters }, ...args)
            .then(coc.onContextualSuccess ?? (report => {
              coc.raiseEntityChanged();
              Operations.notifySuccess();
              coc.context.markRows(report.errors);
            }));
        }
      case "Delete": {
        var setters = await getSetters(coc);
        if (!setters)
          return;

        return Operations.API.deleteMultiple(lites, coc.operationInfo.key, { setters }, ...args)
          .then(coc.onContextualSuccess ?? (report => {
            coc.raiseEntityChanged();
            Operations.notifySuccess();
            coc.context.markRows(report.errors);
          }));
      }
    }
  }

  function getSetters(coc: ContextualOperationContext<Entity>): Promise<Operations.API.PropertySetter[] | undefined> {

    if (!coc.operationInfo.canBeModified)
      return Promise.resolve([]);

    var settersConfig = (coc.settings?.settersConfig ?? Operations.Defaults.defaultSetterConfig)(coc);

    if (settersConfig == "NoDialog")
      return Promise.resolve([]);

    var onlyType = coc.context.lites.map(a => a.EntityType).distinctBy().onlyOrNull();

    if (!onlyType)
      return Promise.resolve([]);

    return MultiPropertySetterModal.show(getTypeInfo(onlyType), coc.context.lites, coc.operationInfo, settersConfig == "Mandatory");
  }
}
