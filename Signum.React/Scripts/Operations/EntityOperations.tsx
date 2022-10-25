import * as React from "react"
import { Entity, toLite, JavascriptMessage, OperationMessage, getToString, NormalControlMessage, EntityPack, ModifiableEntity } from '../Signum.Entities';
import { getTypeInfo, OperationType, GraphExplorer, tryGetTypeInfo } from '../Reflection';
import { classes, ifError } from '../Globals';
import { ButtonsContext, IOperationVisible, ButtonBarElement, FunctionalFrameComponent } from '../TypeContext';
import * as Navigator from '../Navigator';
import MessageModal from '../Modals/MessageModal'
import { ValidationError } from '../Services';
import {
  operationInfos, getSettings, EntityOperationSettings, EntityOperationContext, EntityOperationGroup,
  CreateGroup, API, isEntityOperation, AlternativeOperationSetting, getShortcutToString
} from '../Operations'
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { IconProp } from "@fortawesome/fontawesome-svg-core";
import * as Constructor from "../Constructor"
import { Dropdown, ButtonProps, DropdownButton, Button, OverlayTrigger, Tooltip, ButtonGroup } from "react-bootstrap";
import { BsColor } from "../Components";
import { notifySuccess } from "../Operations";
import { FunctionalAdapter } from "../Modals";
import { getTypeNiceName } from "../Finder";


export function getEntityOperationButtons(ctx: ButtonsContext): Array<ButtonBarElement | undefined> | undefined {
  const ti = tryGetTypeInfo(ctx.pack.entity.Type);

  if (ti == undefined)
    return undefined;

  const operations = operationInfos(ti)
    .filter(oi => isEntityOperation(oi.operationType) && (oi.canBeNew || !ctx.pack.entity.isNew))
    .map(oi => {
      const eos = getSettings(oi.key) as EntityOperationSettings<Entity>;

      const eoc = new EntityOperationContext<Entity>(ctx.frame, ctx.pack.entity as Entity, oi);
      eoc.tag = ctx.tag;
      eoc.canExecute = ctx.pack.canExecute[oi.key];
      eoc.settings = eos;

      return eoc;
    })
    .filter(eoc => eoc.isVisibleInButtonBar(ctx));

  operations.forEach(eoc => eoc.complete());

  const groups = operations.groupBy(eoc => eoc.group && eoc.group.key || "");

  const result = groups.flatMap((gr, i) => {
    if (gr.key == "") {
      return gr.elements.flatMap((eoc, j) => eoc.createButton());
    } else {

      const group = gr.elements[0].group!;
      var groupButtons = gr.elements.flatMap(eoc => eoc.createButton(group)).orderBy(a => a.order);

      return [{
        order: group.order != undefined ? group.order : 100,
        shortcut: e => groupButtons.some(bbe => bbe.shortcut != null && bbe.shortcut(e)),
        button: React.cloneElement(
          <DropdownButton title={group.text()} data-key={group.key} key={i} id={group.key} variant={group.outline != false ? ("outline-" + (group.color ?? "secondary")) : group.color ?? "light"}>
          </DropdownButton>,
          undefined,
          ...groupButtons.map(bbe => bbe.button)
        )
      } as ButtonBarElement];
    }
  });

  return result;
}

export function andClose<T extends Entity>(eoc: EntityOperationContext<T>, inDropdown?: boolean): AlternativeOperationSetting<T> {

  return ({
    name: "andClose",
    text: OperationMessage._0AndClose.niceToString(eoc.textOrNiceName()),
    icon: "xmark",
    keyboardShortcut: eoc.keyboardShortcut && { shiftKey: true, ...eoc.keyboardShortcut },
    isVisible: true,
    inDropdown: inDropdown,
    onClick: () => {
      eoc.onExecuteSuccess = pack => {
        notifySuccess();
        Navigator.raiseEntityChanged(pack.entity);
        eoc.frame.onClose(pack);
        return Promise.resolve(undefined);
      };
      eoc.click();
    }
  });
}

export function andNew<T extends Entity>(eoc: EntityOperationContext<T>, inDropdown?: boolean, reloadComponent: boolean = true): AlternativeOperationSetting<T> {

  return ({
    name: "andNew",
    text: OperationMessage._0AndNew.niceToString(eoc.textOrNiceName()),
    icon: "plus",
    keyboardShortcut: eoc.keyboardShortcut && { altKey: true, ...eoc.keyboardShortcut },
    isVisible: eoc.frame!.allowExchangeEntity && eoc.frame.createNew != null && Navigator.isCreable(eoc.entity.Type, { customComponent: true, isSearch: true }),
    inDropdown: inDropdown,
    onClick: () => {
      eoc.onExecuteSuccess = pack => {
        notifySuccess();
        Navigator.raiseEntityChanged(pack.entity);
        return (eoc.frame.createNew!(pack) ?? Promise.resolve(undefined))
          .then(newPack => newPack && eoc.frame.onReload(newPack, reloadComponent));
      };
      eoc.defaultClick();
    }
  });
}

export type OutlineBsColor =
  | 'outline-primary'
  | 'outline-secondary'
  | 'outline-success'
  | 'outline-danger'
  | 'outline-warning'
  | 'outline-info'
  | 'outline-dark'
  | 'outline-light';

interface OperationButtonProps extends ButtonProps {
  eoc: EntityOperationContext<any /*Entity*/> | undefined;
  group?: EntityOperationGroup;
  variant?: BsColor;
  canExecute?: string | null;
  className?: string;
  outline?: boolean;
  color?: BsColor;
  avoidAlternatives?: boolean;
  onOperationClick?: (eoc: EntityOperationContext<any /*Entity*/>, event: React.MouseEvent) => void;
  children?: React.ReactNode;
  hideOnCanExecute?: boolean;
}

export function OperationButton({ group, onOperationClick, canExecute, eoc: eocOrNull, outline, color, avoidAlternatives, hideOnCanExecute,  ...props }: OperationButtonProps): React.ReactElement<any> | null {

  if (eocOrNull == null)
    return null;

  const eoc = eocOrNull;

  if (canExecute === undefined)
    canExecute = eoc.settings?.overrideCanExecute ? eoc.settings.overrideCanExecute(eoc) : eoc.canExecute;

  const disabled = !!canExecute;

  if (hideOnCanExecute && disabled)
    return null;

  var alternatives = avoidAlternatives ? undefined : eoc.alternatives && eoc.alternatives.filter(a => a.isVisible != false);

  if (group) {

    const item =
      <Dropdown.Item
        {...props as any}
        disabled={disabled}
        title={eoc?.keyboardShortcut && getShortcutToString(eoc.keyboardShortcut)}
        className={classes(disabled ? "disabled sf-pointer-events" : undefined, props?.className)}
        onClick={disabled ? undefined : handleOnClick}
        data-operation={eoc.operationInfo.key}>
        {renderChildren()}
      </Dropdown.Item>;

    if (canExecute)
      return (
        <OverlayTrigger overlay={<Tooltip id={eoc.operationInfo.key + "_tooltip"} placement={"right"}>{canExecute}</Tooltip>}>
          {item}
        </OverlayTrigger>
      );

    return (
      <>
        {item}
        {alternatives?.map(a => renderAlternative(a))}
      </>
    );
  }

  if (outline == null)
    outline = eoc.outline;

  if (color == null)
    color = eoc.color;

  var button = <Button variant={(outline ? ("outline-" + color) as OutlineBsColor : color)}
    {...props}
    key="button"
    title={eoc.keyboardShortcut && getShortcutToString(eoc.keyboardShortcut)}
    className={classes(disabled ? "disabled" : undefined, props?.className, eoc.settings && eoc.settings.classes)}
    onClick={disabled ? undefined : handleOnClick}
    data-operation={eoc.operationInfo.key}>
    {renderChildren()}
  </Button>;

  if (canExecute) {
    return (
      <OverlayTrigger overlay={<Tooltip id={eoc.operationInfo.key + "_tooltip"} placement={"bottom"}>{canExecute}</Tooltip>}>
        {button}
      </OverlayTrigger>
    );
  }

  if (alternatives == undefined || alternatives.length == 0)
    return button;

  var buttonAlternatives = alternatives.filter(a => !a.inDropdown);

  if (buttonAlternatives.length) {
    button =
      (
        <div className="btn-group"
          key="buttonGroup">
          {button}
          {buttonAlternatives.map((aos, i) =>
            <Button key={i}
              variant={(outline ? ("outline-" + color) as OutlineBsColor : color)}
              className={classes("dropdown-toggle-split px-1", disabled ? "disabled" : undefined, aos.classes)}
              onClick={() => aos.onClick(eoc)}
              title={aos.text + (aos.keyboardShortcut ? (" (" + getShortcutToString(aos.keyboardShortcut) + ")") : "")}>
              <small><FontAwesomeIcon icon={aos.icon!} color={aos.iconColor} fixedWidth /></small>
            </Button>
          )}
        </div>
      );
  }

  var dropdownAlternatives = alternatives.filter(a => a.inDropdown);
  if (dropdownAlternatives.length == 0)
    return button;

  return (
    <Dropdown as={ButtonGroup}>
      {button}
      <Dropdown.Toggle split color={eoc.color} id={eoc.operationInfo.key + "_split"} />
      <Dropdown.Menu align="end">
        {dropdownAlternatives.map(a => renderAlternative(a))}
      </Dropdown.Menu>
    </Dropdown>
  );


  function renderAlternative(aos: AlternativeOperationSetting<Entity>) {

    return (
      <Dropdown.Item
        color={aos.color}
        className={aos.classes}
        key={aos.name}
        title={aos.keyboardShortcut && getShortcutToString(aos.keyboardShortcut)}
        onClick={() => aos.onClick(eoc!)}
        data-alternative={aos.name}>
        {withIcon(aos.text, aos.icon, aos.iconColor, aos.iconAlign)}
      </Dropdown.Item>
    );
  }

  function renderChildren() {
    if (props.children)
      return props.children;

    let text: string = eoc.settings?.text ? eoc.settings.text(eoc) :
      group?.simplifyName ? group.simplifyName(eoc.operationInfo.niceName) :
        eoc.operationInfo.niceName;

    return withIcon(text, eoc?.icon, eoc?.settings?.iconColor, eoc?.settings?.iconAlign);
  }

  function handleOnClick(event: React.MouseEvent<any>) {
    eoc.event = event;
    event.persist();
    if (onOperationClick)
      onOperationClick(eoc, event);
    else
      eoc.click();
  }
}

export function withIcon(text: string, icon?: IconProp, iconColor?: string, iconAlign?: "start" | "end") {
  if (icon) {
    switch (iconAlign) {
      case "end": return (<span>{text}<FontAwesomeIcon icon={icon} color={iconColor} fixedWidth className="ms-2" /></span>);
      default: return (<span><FontAwesomeIcon icon={icon} color={iconColor} fixedWidth className="me-2"/>{text}</span>);
    }
  }
  else {
    return text;
  }
}

export function defaultOnClick<T extends Entity>(eoc: EntityOperationContext<T>, ...args: any[]): Promise<void> {
  if (!eoc.operationInfo.canBeModified) {
    switch (eoc.operationInfo.operationType) {
      case "ConstructorFrom": return defaultConstructFromLite(eoc, ...args);
      case "Execute": return defaultExecuteLite(eoc, ...args);
      case "Delete": return defaultDeleteLite(eoc, ...args);
    }
  } else {
    switch (eoc.operationInfo.operationType) {
      case "ConstructorFrom": return defaultConstructFromEntity(eoc, ...args);
      case "Execute": return defaultExecuteEntity(eoc, ...args);
      case "Delete": return defaultDeleteEntity(eoc, ...args);
    }
  }

  throw new Error("Unexpected OperationType");
}

export function defaultConstructFromEntity<T extends Entity>(eoc: EntityOperationContext<T>, ...args: any[]) {

  return confirmInNecessary(eoc).then(conf => {
    if (!conf)
      return;

    return API.constructFromEntity(eoc.entity, eoc.operationInfo.key, ...args)
      .then(eoc.onConstructFromSuccess ?? (pack => {
        notifySuccess();
        if (pack?.entity.id != null)
          Navigator.raiseEntityChanged(pack.entity);
        return Navigator.createNavigateOrTab(pack, eoc.event ?? ({} as React.MouseEvent));
      }))
      .catch(ifError(ValidationError, e => eoc.frame.setError(e.modelState, "entity")));
  });
}

export function defaultConstructFromLite<T extends Entity>(eoc: EntityOperationContext<T>, ...args: any[]): Promise<void> {

  return confirmInNecessary(eoc).then(conf => {
    if (!conf)
      return;

    return API.constructFromLite(toLite(eoc.entity), eoc.operationInfo.key, ...args)
      .then(eoc.onConstructFromSuccess ?? (pack => {
        notifySuccess();
        if (pack?.entity.id != null)
          Navigator.raiseEntityChanged(pack.entity);
        return Navigator.createNavigateOrTab(pack, eoc.event ?? ({} as React.MouseEvent));
      }))
      .catch(ifError(ValidationError, e => eoc.frame.setError(e.modelState, "entity")))
  });
}


export function defaultExecuteEntity<T extends Entity>(eoc: EntityOperationContext<T>, ...args: any[]) {

  return confirmInNecessary(eoc).then(conf => {
    if (!conf)
      return;

    return API.executeEntity(eoc.entity, eoc.operationInfo.key, ...args)
      .then(eoc.onExecuteSuccess ?? (pack => {
        eoc.frame.onReload(pack);
        if (pack?.entity.id != null)
          Navigator.raiseEntityChanged(pack.entity);
        notifySuccess();
        return;
      }))
      .catch(ifError(ValidationError, e => eoc.frame.setError(e.modelState, "entity")));
  });
}

export function defaultExecuteLite<T extends Entity>(eoc: EntityOperationContext<T>, ...args: any[]) {

  return confirmInNecessary(eoc).then(conf => {
    if (!conf)
      return;

    return API.executeLite(toLite(eoc.entity), eoc.operationInfo.key, ...args)
      .then(eoc.onExecuteSuccess ?? (pack => {
        eoc.frame.onReload(pack);
        if (pack?.entity.id != null)
          Navigator.raiseEntityChanged(pack.entity);
        notifySuccess();
        return;
      }))
      .catch(ifError(ValidationError, e => eoc.frame.setError(e.modelState, "entity")));
  });
}

export function defaultDeleteEntity<T extends Entity>(eoc: EntityOperationContext<T>, ...args: any[]) {

  return confirmInNecessary(eoc).then(conf => {
    if (!conf)
      return;

    return API.deleteEntity(eoc.entity, eoc.operationInfo.key, ...args)
      .then(eoc.onDeleteSuccess ?? (() => {
        eoc.frame.onClose();
        Navigator.raiseEntityChanged(eoc.entity.Type);
        notifySuccess();
        return;
      }))
      .catch(ifError(ValidationError, e => eoc.frame.setError(e.modelState, "entity")));
  });
}

export function defaultDeleteLite<T extends Entity>(eoc: EntityOperationContext<T>, ...args: any[]) {

  return confirmInNecessary(eoc).then(conf => {
    if (!conf)
      return;

    return API.deleteLite(toLite(eoc.entity), eoc.operationInfo.key, ...args)
      .then(eoc.onDeleteSuccess ?? (() => {
        eoc.frame.onClose();
        Navigator.raiseEntityChanged(eoc.entity.Type);
        notifySuccess();
        return;
      }))
      .catch(ifError(ValidationError, e => eoc.frame.setError(e.modelState, "entity")));
  });
}


export function confirmInNecessary<T extends Entity>(eoc: EntityOperationContext<T>, checkLite = true): Promise<boolean> {

  if (!eoc.operationInfo.canBeModified) {
    GraphExplorer.propagateAll(eoc.entity);

    if (eoc.entity.modified)
      throw new Error(NormalControlMessage.SaveChangesFirst.niceToString());
  }

  const confirmMessage = getConfirmMessage(eoc);

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

function getConfirmMessage<T extends Entity>(eoc: EntityOperationContext<T>) {
  if (eoc.settings && eoc.settings.confirmMessage === null)
    return undefined;

  if (eoc.settings && eoc.settings.confirmMessage != undefined) {
    var result = eoc.settings.confirmMessage(eoc);
    if (result == true)
      return getDefaultConfirmMessage(eoc);

    return result;
  }

  //eoc.settings.confirmMessage === undefined
  if (eoc.operationInfo.operationType == "Delete")
    return getDefaultConfirmMessage(eoc);

  return undefined;
}

function getDefaultConfirmMessage<T extends Entity>(eoc: EntityOperationContext<T>) {

  if (eoc.operationInfo.operationType == "Delete")
    return OperationMessage.PleaseConfirmYouWouldLikeToDelete0FromTheSystem.niceToString().formatHtml(
      <strong>{getToString(eoc.entity)} ({getTypeInfo(eoc.entity.Type).niceName} {eoc.entity.id})</strong>
    );
  else
    return OperationMessage.PleaseConfirmYouWouldLikeTo01.niceToString().formatHtml(
      <strong>{eoc.operationInfo.niceName}</strong>, 
      <strong>{getToString(eoc.entity)} ({getTypeInfo(eoc.entity.Type).niceName} {eoc.entity.id})</strong>
    );

}
