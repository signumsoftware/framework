import * as React from "react"
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { Entity, JavascriptMessage, OperationMessage, SearchMessage } from '../Signum.Entities';
import { getTypeInfo, OperationType } from '../Reflection';
import { classes } from '../Globals';
import * as Navigator from '../Navigator';
import MessageModal from '../Modals/MessageModal'
import Notify from '../Frames/Notify';
import { ContextualItemsContext, MenuItemBlock } from '../SearchControl/ContextualItems';
import {
  operationInfos, getSettings, ContextualOperationSettings, ContextualOperationContext, EntityOperationSettings, API, isEntityOperation, Defaults
} from '../Operations'
import * as Operations from "../Operations";
import { IconProp } from "@fortawesome/fontawesome-svg-core";
import { Dropdown, OverlayTrigger, Tooltip } from "react-bootstrap";

export function getConstructFromManyContextualItems(ctx: ContextualItemsContext<Entity>): Promise<MenuItemBlock | undefined> | undefined {
  if (ctx.lites.length == 0)
    return undefined;

  const types = ctx.lites.groupBy(lite => lite.EntityType);

  if (types.length != 1)
    return undefined;

  const ti = getTypeInfo(types[0].key);

  const menuItems = operationInfos(ti)
    .filter(oi => oi.operationType == OperationType.ConstructorFromMany)
    .map(oi => {
      const os = getSettings(oi.key) as ContextualOperationSettings<Entity>;
      const coc = {
        context: ctx,
        operationInfo: oi,
        settings: os,
      } as ContextualOperationContext<Entity>;

      if (os == undefined || os.isVisible == undefined || os.isVisible(coc))
        return coc;

      return undefined;
    })
    .filter(coc => coc != undefined)
    .map(coc => coc!)
    .orderBy(coc => coc.settings && coc.settings.order)
    .map(coc => MenuItemConstructor.createContextualMenuItem(coc, defaultConstructFromMany));

  if (!menuItems.length)
    return undefined;

  return Promise.resolve({
    header: SearchMessage.Create.niceToString(),
    menuItems: menuItems
  } as MenuItemBlock);
}



function defaultConstructFromMany(coc: ContextualOperationContext<Entity>, ...args: any[]) {

  confirmInNecessary(coc).then(conf => {
    if (!conf)
      return;

    API.constructFromMany<Entity, Entity>(coc.context.lites, coc.operationInfo.key, ...args).then(pack => {
      Navigator.createNavigateOrTab(pack, coc.event!)
        .then(() => coc.context.markRows({}))
        .done();
    }).done();
  }).done();

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

      const visibleByDefault = !oi.canBeModified && (ctx.lites.length == 1 || oi.operationType != OperationType.ConstructorFrom)

      if (eos == undefined ? visibleByDefault :
        cos == undefined || cos.isVisible == undefined ? (visibleByDefault && eos.isVisible == undefined && (eos.onClick == undefined || cos != undefined && cos.onClick != undefined)) :
          cos.isVisible(coc))
        return coc;

      return undefined;
    })
    .filter(coc => coc != undefined)
    .map(coc => coc!)
    .orderBy(coc => coc.settings && coc.settings.order);

  if (!contexts.length)
    return undefined;

  let contextPromise: Promise<ContextualOperationContext<Entity>[]>;
  if (contexts.some(coc => coc.operationInfo.hasCanExecute) || Operations.Options.maybeReadonly(ti)) {
    if (ctx.lites.length == 1) {
      contextPromise = Navigator.API.fetchEntityPack(ctx.lites[0]).then(ep => {
        contexts.forEach(coc => {
          coc.canExecute = ep.canExecute[coc.operationInfo.key];
          coc.isReadonly = Navigator.isReadOnly(ep, true);
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

    if (Navigator.isReadOnly(ti, true)) {
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
      .map(coc => MenuItemConstructor.createContextualMenuItem(coc, defaultContextualClick));

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
    icon: "question"
  }).then(result => { return result == "yes"; });
}

function getConfirmMessage(coc: ContextualOperationContext<Entity>) {
  if (coc.settings && coc.settings.confirmMessage === null)
    return undefined;

  if (coc.settings && coc.settings.confirmMessage != undefined)
    return coc.settings.confirmMessage(coc);

  if (coc.operationInfo.operationType == OperationType.Delete)
    return coc.context.lites.length > 1 ?
      OperationMessage.PleaseConfirmYouDLikeToDeleteTheSelectedEntitiesFromTheSystem.niceToString() :
      OperationMessage.PleaseConfirmYouDLikeToDeleteTheEntityFromTheSystem.niceToString();

  return undefined;
}


export namespace MenuItemConstructor { //To allow monkey patching

  export function simplifyName(niceName: string) {
    const array = new RegExp(OperationMessage.CreateFromRegex.niceToString()).exec(niceName);
    return array ? (niceName.tryBefore(array[1]) ?? "") + array[1].firstUpper() : niceName;
  }
  export function createContextualMenuItem(coc: ContextualOperationContext<Entity>, defaultClick: (coc: ContextualOperationContext<Entity>) => void) {

    const text = coc.settings && coc.settings.text ? coc.settings.text() :
      coc.entityOperationSettings?.text ? coc.entityOperationSettings.text() :
        simplifyName(coc.operationInfo.niceName);

    const color = coc.settings?.color ?? coc.entityOperationSettings?.color ?? Defaults.getColor(coc.operationInfo);
    const icon = coalesceIcon(coc.settings?.icon, coc.entityOperationSettings?.icon);
    const iconColor = coc.settings?.iconColor || coc.entityOperationSettings?.iconColor;

    const disabled = !!coc.canExecute;

    const onClick = (me: React.MouseEvent<any>) => {
      coc.event = me;
      coc.settings && coc.settings.onClick ? coc.settings!.onClick!(coc) : defaultClick(coc)
    }

    const item = (
      <Dropdown.Item
        onClick={disabled ? undefined : onClick}
        disabled={disabled}
        style={{ pointerEvents: "initial" }}
        data-operation={coc.operationInfo.key}>
        {icon ? <FontAwesomeIcon icon={icon} className="icon" color={iconColor} fixedWidth /> :
          color ? <span className={classes("icon", "empty-icon", "btn-" + color)}></span> : undefined}
        {(icon != null || color != null) && " "}
        {text}
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
}

export function notifySuccess() {
  Notify.singleton && Notify.singleton.notifyTimeout({ text: JavascriptMessage.executed.niceToString(), type: "success" });
}

export function defaultContextualClick(coc: ContextualOperationContext<any>, ...args: any[]) {

  coc.event!.persist();

  confirmInNecessary(coc).then(conf => {
    if (!conf)
      return;

    switch (coc.operationInfo.operationType) {
      case OperationType.ConstructorFrom:
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
          API.constructFromMultiple(coc.context.lites, coc.operationInfo.key, ...args)
            .then(coc.onContextualSuccess ?? (report => {
              notifySuccess();
              coc.context.markRows(report.errors);
            }))
            .done();
        }
        break;
      case OperationType.Execute:
        API.executeMultiple(coc.context.lites, coc.operationInfo.key, ...args)
          .then(coc.onContextualSuccess ?? (report => {
            notifySuccess();
            coc.context.markRows(report.errors);
          }))
          .done();
        break;
      case OperationType.Delete:
        API.deleteMultiple(coc.context.lites, coc.operationInfo.key, ...args)
          .then(coc.onContextualSuccess ?? (report => {
            notifySuccess();
            coc.context.markRows(report.errors);
          }))
          .done();
        break;
    }
  }).done();



}


export function coalesceIcon(icon: IconProp | undefined, icon2: IconProp | undefined): IconProp | undefined{ //Till the error is fixed

  if (icon === null)
    return undefined;

  if (icon === undefined)
    return icon2

  return icon;
}

