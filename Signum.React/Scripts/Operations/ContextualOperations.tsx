import * as React from "react"
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { Entity, JavascriptMessage, OperationMessage, SearchMessage, Lite, External } from '../Signum.Entities';
import { getTypeInfo, OperationType } from '../Reflection';
import { classes } from '../Globals';
import * as Navigator from '../Navigator';
import MessageModal from '../Modals/MessageModal'
import { ContextualItemsContext, MenuItemBlock } from '../SearchControl/ContextualItems';
import {
  operationInfos, getSettings, notifySuccess, ContextualOperationSettings, ContextualOperationContext, EntityOperationSettings, API, isEntityOperation, Defaults
} from '../Operations'
import * as Operations from "../Operations";
import { IconProp } from "@fortawesome/fontawesome-svg-core";
import { Dropdown, OverlayTrigger, Tooltip } from "react-bootstrap";
import { MultiPropertySetterModal, PropertySetterComponentProps } from "./MultiPropertySetter";
import { BsColor } from "../Components";

export function getConstructFromManyContextualItems(ctx: ContextualItemsContext<Entity>): Promise<MenuItemBlock | undefined> | undefined {
  if (ctx.lites.length == 0)
    return undefined;

  const types = ctx.lites.groupBy(lite => lite.EntityType);

  if (types.length != 1)
    return undefined;

  const ti = getTypeInfo(types[0].key);

  const menuItems = operationInfos(ti)
    .filter(oi => oi.operationType == "ConstructorFromMany")
    .map(oi => {
      const os = getSettings(oi.key) as ContextualOperationSettings<Entity>;
      const coc = new ContextualOperationContext<Entity>(oi, ctx);
      coc.settings = os;
      if (os == undefined || os.isVisible == undefined || os.isVisible(coc))
        return coc;

      return undefined;
    })
    .filter(coc => coc != undefined)
    .map(coc => coc!)
    .orderBy(coc => coc.settings && coc.settings.order)
    .flatMap(coc => coc.createMenuItems());

  if (!menuItems.length)
    return undefined;

  return Promise.resolve({
    header: SearchMessage.Create.niceToString(),
    menuItems: menuItems
  } as MenuItemBlock);
}





export function getEntityOperationsContextualItems(ctx: ContextualItemsContext<Entity>): Promise<MenuItemBlock | undefined> | undefined {
  if (ctx.lites.length == 0)
    return undefined;

  const types = ctx.lites.groupBy(coc => coc.EntityType);

  if (types.length != 1)
    return undefined;

  const ti = getTypeInfo(types[0].key);
  const contexts = operationInfos(ti)
    .filter(oi => isEntityOperation(oi.operationType))
    .map(oi => {
      const eos = getSettings(oi.key) as EntityOperationSettings<Entity> | undefined;
      const cos = eos == undefined ? undefined :
        ctx.lites.length == 1 ? eos.contextual : eos.contextualFromMany
      const coc = new ContextualOperationContext<Entity>(oi, ctx);
      coc.settings = cos;
      coc.entityOperationSettings = eos;

        return coc;
    })
    .filter(coc => coc.isVisibleInContextualMenu())
    .map(coc => coc!)
    .orderBy(coc => coc.settings && coc.settings.order);

  if (!contexts.length)
    return undefined;

  let contextPromise: Promise<ContextualOperationContext<Entity>[]>;
  if (contexts.some(coc => coc.operationInfo.hasCanExecute) || Operations.Options.maybeReadonly(ti)) {
    if (ctx.lites.length == 1) {
      contextPromise = Navigator.API.fetchEntityPack(ctx.lites[0]).then(ep => {
        contexts.forEach(coc => {
          coc.pack = ep;
          coc.canExecute = ep.canExecute[coc.operationInfo.key];
          coc.isReadonly = Navigator.isReadOnly(ep, { ignoreTypeIsReadonly: true });
        });
        return contexts;
      });
    } else /*if (ctx.lites.length > 1)*/ {
      contextPromise = API.stateCanExecutes(ctx.lites, contexts.filter(coc => coc.operationInfo.hasStates).map(a => a.operationInfo.key))
        .then(response => {
          contexts.forEach(coc => {
            coc.canExecute = response.canExecutes[coc.operationInfo.key];
            coc.isReadonly = response.isReadOnly;
          });
          return contexts;
        });
    }
  } else {

    if (Navigator.isReadOnly(ti, { ignoreTypeIsReadonly: true })) {
      contexts.forEach(a => a.isReadonly = true);
    }

    contextPromise = Promise.resolve(contexts);
  }

  return contextPromise.then(ctxs => {
    const menuItems = ctxs
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
  });
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

  return false;
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

  if (coc.settings && coc.settings.confirmMessage != undefined)
    return coc.settings.confirmMessage(coc);

  if (coc.operationInfo.operationType == "Delete") {

    if (coc.context.lites.length > 1) {
      var message = coc.context.lites
        .groupBy(a => a.EntityType)
        .map(gr => gr.elements.length + " " + (gr.elements.length == 1 ? getTypeInfo(gr.key).niceName : getTypeInfo(gr.key).nicePluralName))
        .joinComma(External.CollectionMessage.And.niceToString());

      return OperationMessage.PleaseConfirmYouWouldLikeToDelete0FromTheSystem.niceToString().formatHtml(<strong>{message}</strong>);
    }
    else {
      var lite = coc.context.lites.single();
      return OperationMessage.PleaseConfirmYouWouldLikeToDelete0FromTheSystem.niceToString().formatHtml(<strong>{lite.toStr} ({getTypeInfo(lite.EntityType).niceName} {lite.id})</strong>);;
    }
  }

  return undefined;
}


export interface OperationMenuItemProps {
  coc: ContextualOperationContext<any>;
  onOperationClick?: (coc: ContextualOperationContext<Entity>) => void;
  extraButtons?: React.ReactNode;
  children?: React.ReactNode;
  color?: BsColor;
  icon?: IconProp;
  iconColor?: string;
}

export function OperationMenuItem({ coc, onOperationClick, extraButtons, color, icon, iconColor, children }: OperationMenuItemProps) {
  const text = children ?? OperationMenuItem.getText(coc);


  if (color == null)
    color = coc.settings?.color ?? coc.entityOperationSettings?.color ?? Defaults.getColor(coc.operationInfo);

  if (icon == null)
    icon = coalesceIcon(coc.settings?.icon, coc.entityOperationSettings?.icon);

  if (iconColor == null)
    iconColor = coc.settings?.iconColor || coc.entityOperationSettings?.iconColor;

  const disabled = !!coc.canExecute;

  const onClick = onOperationClick ?? coc.settings?.onClick ?? defaultContextualClick

  const handleOnClick = (me: React.MouseEvent<any>) => {
    coc.event = me;
    onClick(coc);
  }

  const item = (
    <Dropdown.Item
      onClick={disabled ? undefined : handleOnClick}
      disabled={disabled}
      style={{ pointerEvents: "initial" }}
      data-operation={coc.operationInfo.key}>
      {icon ? <FontAwesomeIcon icon={icon} className="icon" color={iconColor} fixedWidth /> :
        color ? <span className={classes("icon", "empty-icon", "btn-" + color)}></span> : undefined}
      {(icon != null || color != null) && " "}
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
    return coc.settings.text();

  if (coc.entityOperationSettings?.text)
    return coc.entityOperationSettings.text();

  return <>{OperationMenuItem.simplifyName(coc.operationInfo.niceName)}{coc.operationInfo.canBeModified ? <small className="ml-2">{OperationMessage.MultiSetter.niceToString()}</small> : null}</>;

};

OperationMenuItem.simplifyName = (niceName: string) => {
  const array = new RegExp(OperationMessage.CreateFromRegex.niceToString()).exec(niceName);
  return array ? (niceName.tryBefore(array[1]) ?? "") + array[1].firstUpper() : niceName;
}


export function defaultContextualClick(coc: ContextualOperationContext<any>, ...args: any[]) {

  coc.event!.persist();

  confirmInNecessary(coc).then(conf => {
    if (!conf)
      return;

    switch (coc.operationInfo.operationType) {
      case "ConstructorFromMany":
        {

          API.constructFromMany(coc.context.lites, coc.operationInfo.key, ...args)
            .then(coc.onConstructFromSuccess ?? (pack => {
              notifySuccess();
              Navigator.createNavigateOrTab(pack, coc.event!)
                .then(() => coc.context.markRows({}))
                .done();
            }))
            .done();

          break;
        }
      case "ConstructorFrom":
        if (coc.context.lites.length == 1) {
          API.constructFromLite(coc.context.lites[0], coc.operationInfo.key, ...args)
            .then(coc.onConstructFromSuccess ?? (pack => {
              notifySuccess();
              Navigator.createNavigateOrTab(pack, coc.event!)
                .then(() => coc.context.markRows({}))
                .done();
            }))
            .done();
        } else {
          getSetters(coc)
            .then(setters => setters && API.constructFromMultiple(coc.context.lites, coc.operationInfo.key, setters, ...args)
              .then(coc.onContextualSuccess ?? (report => {
                notifySuccess();
                coc.context.markRows(report.errors);
              })))
            .done();
        }
        break;
      case "Execute":
        getSetters(coc)
          .then(setters => setters && API.executeMultiple(coc.context.lites, coc.operationInfo.key, setters, ...args)
            .then(coc.onContextualSuccess ?? (report => {
              notifySuccess();
              coc.context.markRows(report.errors);
            })))
          .done();
        break;
      case "Delete":
        getSetters(coc)
          .then(setters => setters && API.deleteMultiple(coc.context.lites, coc.operationInfo.key, setters, ...args)
            .then(coc.onContextualSuccess ?? (report => {
              notifySuccess();
              coc.context.markRows(report.errors);
            })))
          .done();
        break;
    }
  }).done();

  function getSetters(coc: ContextualOperationContext<Entity>): Promise<Operations.API.PropertySetter[] | undefined> {

    if (!coc.operationInfo.canBeModified)
      return Promise.resolve([]);

    var settersConfig = (coc.settings?.settersConfig ?? Defaults.defaultSetterConfig)(coc);

    if (settersConfig == "NoDialog")
      return Promise.resolve([]);

    var onlyType = coc.context.lites.map(a => a.EntityType).distinctBy(a => a).onlyOrNull();

    if (!onlyType)
      return Promise.resolve([]);

    return MultiPropertySetterModal.show(getTypeInfo(onlyType), coc.context.lites, coc.operationInfo, settersConfig == "Mandatory");
  }
}


export function coalesceIcon(icon: IconProp | undefined, icon2: IconProp | undefined): IconProp | undefined{ //Till the error is fixed

  if (icon === null)
    return undefined;

  if (icon === undefined)
    return icon2

  return icon;
}

