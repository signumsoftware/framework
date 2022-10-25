import * as React from "react"
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { Entity, JavascriptMessage, OperationMessage, SearchMessage, Lite, External, getToString } from '../Signum.Entities';
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
      const cos = getSettings(oi.key) as ContextualOperationSettings<Entity> | undefined;
      return new ContextualOperationContext<Entity>(oi, ctx, cos);
    })
    .filter(coc => coc.isVisibleInContextualMenu())
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
      const coc = new ContextualOperationContext<Entity>(oi, ctx, cos, eos);
      return coc;
    })
    .filter(coc => coc.isVisibleInContextualMenu())
    .map(coc => coc!)
    .orderBy(coc => coc.settings && coc.settings.order);

  if (!contexts.length)
    return undefined;

  let contextPromise: Promise<ContextualOperationContext<Entity>[]>;
  if (contexts.some(coc => coc.operationInfo.hasCanExecute || coc.operationInfo.hasStates) || Operations.Options.maybeReadonly(ti)) {
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

  if (coc.settings && coc.settings.confirmMessage != undefined) {

    var result = coc.settings.confirmMessage(coc);
    if (result == true)
      return getDefaultConfirmMessage(coc);
    else
      return result;
  }


  if (coc.operationInfo.operationType == "Delete") {
    return getDefaultConfirmMessage(coc);
  }

  return undefined;
}

function getDefaultConfirmMessage(coc: ContextualOperationContext<Entity>) {

  if (coc.context.lites.length > 1) {
    var message = coc.context.lites
      .groupBy(a => a.EntityType)
      .map(gr => gr.elements.length + " " + (gr.elements.length == 1 ? getTypeInfo(gr.key).niceName : getTypeInfo(gr.key).nicePluralName))
      .joinComma(External.CollectionMessage.And.niceToString());

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
  icon?: IconProp;
  iconColor?: string;
}

export function OperationMenuItem({ coc, onOperationClick, onClick, extraButtons, color, icon, iconColor, children }: OperationMenuItemProps) {
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
      disabled={disabled}
      style={{ pointerEvents: "initial" }}
      data-operation={coc.operationInfo.key}
      className={color ? "text-" + color : undefined}>
      {icon ? <FontAwesomeIcon icon={icon} className="icon" color={iconColor} fixedWidth /> :
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

  var multiSetter = coc.operationInfo.canBeModified && !((cos?.settersConfig ?? Defaults.defaultSetterConfig)(coc) == "NoDialog") ?
    <small className="ms-2">{OperationMessage.MultiSetter.niceToString()}</small> : null;

  return <>{OperationMenuItem.simplifyName(coc.operationInfo.niceName)}{multiSetter}</>;

};

OperationMenuItem.simplifyName = (niceName: string) => {
  const array = new RegExp(OperationMessage.CreateFromRegex.niceToString()).exec(niceName);
  return array ? OperationMessage.Create0.niceToString(array[1].firstUpper()) : niceName;
}


export function defaultContextualOperationClick(coc: ContextualOperationContext<any>, ...args: any[]) : Promise<void> {

  coc.event!.persist();

  return confirmInNecessary(coc).then(conf => {
    if (!conf)
      return;

    switch (coc.operationInfo.operationType) {
      case "ConstructorFromMany":
        {
          return API.constructFromMany(coc.context.lites, coc.operationInfo.key, ...args)
            .then(coc.onConstructFromSuccess ?? (pack => {
              notifySuccess();
              if (pack?.entity.id != null)
                Navigator.raiseEntityChanged(pack.entity);
              Navigator.createNavigateOrTab(pack, coc.event!)
                .then(() => coc.context.markRows({}));
            }));
        }
      case "ConstructorFrom":
        if (coc.context.lites.length == 1) {
          return API.constructFromLite(coc.context.lites[0], coc.operationInfo.key, ...args)
            .then(coc.onConstructFromSuccess ?? (pack => {
              if (pack?.entity.id != null)
                Navigator.raiseEntityChanged(pack.entity);
              notifySuccess();
              return Navigator.createNavigateOrTab(pack, coc.event!)
                .then(() => coc.context.markRows({}))
            }));
        } else {
          return getSetters(coc)
            .then(setters => setters && API.constructFromMultiple(coc.context.lites, coc.operationInfo.key, { setters }, ...args)
              .then(coc.onContextualSuccess ?? (report => {
                //Navigator.raiseEntityChanged(??);
                notifySuccess();
                coc.context.markRows(report.errors);
              })));
        }
      case "Execute":
        return getSetters(coc)
          .then(setters => setters && API.executeMultiple(coc.context.lites, coc.operationInfo.key, { setters }, ...args)
            .then(coc.onContextualSuccess ?? (report => {
              coc.raiseEntityChanged();
              notifySuccess();
              coc.context.markRows(report.errors);
            })));
      case "Delete":
        return getSetters(coc)
          .then(setters => setters && API.deleteMultiple(coc.context.lites, coc.operationInfo.key, { setters }, ...args)
            .then(coc.onContextualSuccess ?? (report => {
              coc.raiseEntityChanged();
              notifySuccess();
              coc.context.markRows(report.errors);
            })));
    }
  });

  function getSetters(coc: ContextualOperationContext<Entity>): Promise<Operations.API.PropertySetter[] | undefined> {

    if (!coc.operationInfo.canBeModified)
      return Promise.resolve([]);

    var settersConfig = (coc.settings?.settersConfig ?? Defaults.defaultSetterConfig)(coc);

    if (settersConfig == "NoDialog")
      return Promise.resolve([]);

    var onlyType = coc.context.lites.map(a => a.EntityType).distinctBy().onlyOrNull();

    if (!onlyType)
      return Promise.resolve([]);

    return MultiPropertySetterModal.show(getTypeInfo(onlyType), coc.context.lites, coc.operationInfo, settersConfig == "Mandatory");
  }
}
