import * as React from "react"
import { Entity, toLite, JavascriptMessage, OperationMessage, getToString, NormalControlMessage, EntityPack, ModifiableEntity } from '../Signum.Entities';
import { getTypeInfo, OperationType, GraphExplorer, tryGetTypeInfo } from '../Reflection';
import { classes, ifError } from '../Globals';
import { ButtonsContext, IOperationVisible, ButtonBarElement, FunctionalFrameComponent } from '../TypeContext';
import { Navigator } from '../Navigator';
import MessageModal from '../Modals/MessageModal'
import { ValidationError } from '../Services';
import { Operations, EntityOperationSettings, EntityOperationContext, EntityOperationGroup, AlternativeOperationSetting } from '../Operations'
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { IconProp } from "@fortawesome/fontawesome-svg-core";
import { Dropdown, ButtonProps, DropdownButton, Button, OverlayTrigger, Tooltip, ButtonGroup } from "react-bootstrap";
import { BsColor } from "../Components";

export namespace EntityOperations {
  export function getEntityOperationButtons(ctx: ButtonsContext): Array<ButtonBarElement | undefined> | undefined {
    const ti = tryGetTypeInfo(ctx.pack.entity.Type);

    if (ti == undefined)
      return undefined;

    const operations = Operations.operationInfos(ti)
      .filter(oi => Operations.isEntityOperation(oi.operationType) && (oi.canBeNew || !ctx.pack.entity.isNew))
      .filter(oi => ctx.pack.entity.isNew || oi.key in ctx.pack.canExecute)
      .map(oi => {

        const eos = Operations.getSettings(oi.key) as EntityOperationSettings<Entity>;

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
              {undefined}
            </DropdownButton>,
            undefined,
            ...groupButtons.map(bbe => bbe.button)
          )
        } as ButtonBarElement];
      }
    });

    return result;
  }

  export function andClose<T extends Entity>(eoc: EntityOperationContext<T>, inDropdown?: boolean, isDefault?: boolean): AlternativeOperationSetting<T> {

    return ({
      name: "andClose",
      text: OperationMessage._0AndClose.niceToString(eoc.textOrNiceName()),
      icon: "xmark",
      keyboardShortcut: eoc.keyboardShortcut && { shiftKey: true, ...eoc.keyboardShortcut },
      isVisible: true,
      inDropdown: inDropdown,
      isDefault: isDefault,
      onClick: () => {
        eoc.onExecuteSuccess = pack => {
          Operations.notifySuccess();
          Navigator.raiseEntityChanged(pack.entity);
          eoc.frame.onClose(pack);
          return Promise.resolve(undefined);
        };
        return eoc.click();
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
          Operations.notifySuccess();
          Navigator.raiseEntityChanged(pack.entity);
          return (eoc.frame.createNew!(pack) ?? Promise.resolve(undefined))
            .then(newPack => newPack && eoc.frame.onReload(newPack, reloadComponent));
        };
        return eoc.defaultClick();
      }
    });
  }



  export function withIcon(text: string, icon?: IconProp | React.ReactElement, iconColor?: string, iconAlign?: "start" | "end"): string | React.ReactElement {
    if (icon) {
      var m = iconAlign == "end" ? "ms-2" : "me-2";

      var iconEleme = React.isValidElement<React.AllHTMLAttributes<any>>(icon) ? React.cloneElement(icon, { className: classes(icon.props.className, m) }) :
        <FontAwesomeIcon aria-hidden={true} icon={icon as IconProp} color={iconColor} className={classes("fa-fw", m)} />;

      switch (iconAlign) {
        case "end": return (<span>{text}{iconEleme}</span>);
        default: return (<span>{iconEleme} {text}</span>);
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

  export function defaultConstructFromEntity<T extends Entity>(eoc: EntityOperationContext<T>, ...args: any[]): Promise<void | undefined> {

    return confirmInNecessary(eoc).then(conf => {
      if (!conf)
        return;

      return Operations.API.constructFromEntity(eoc.entity, eoc.operationInfo.key, ...args)
        .then(eoc.onConstructFromSuccess ?? eoc.onConstructFromSuccess_Default)
        .catch(ifError(ValidationError, e => eoc.frame.setError(e.modelState, "entity")));
    });
  }

  export function defaultConstructFromLite<T extends Entity>(eoc: EntityOperationContext<T>, ...args: any[]): Promise<void> {

    return confirmInNecessary(eoc).then(conf => {
      if (!conf)
        return;

      return Operations.API.constructFromLite(toLite(eoc.entity), eoc.operationInfo.key, ...args)
        .then(eoc.onConstructFromSuccess ?? eoc.onConstructFromSuccess_Default)
        .catch(ifError(ValidationError, e => eoc.frame.setError(e.modelState, "entity")))
    });
  }


  export function defaultExecuteEntity<T extends Entity>(eoc: EntityOperationContext<T>, ...args: any[]): Promise<void | undefined> {

    return confirmInNecessary(eoc).then(conf => {
      if (!conf)
        return;

      return Operations.API.executeEntity(eoc.entity, eoc.operationInfo.key, ...args)
        .then(eoc.onExecuteSuccess ?? eoc.onExecuteSuccess_Default)
        .catch(ifError(ValidationError, e => eoc.frame.setError(e.modelState, "entity")));
    });
  }

  export function defaultExecuteLite<T extends Entity>(eoc: EntityOperationContext<T>, ...args: any[]): Promise<void | undefined> {

    return confirmInNecessary(eoc).then(conf => {
      if (!conf)
        return;

      var promise = eoc.progressModalOptions ?
        Operations.API.executeLiteWithProgress(toLite(eoc.entity), eoc.operationInfo.key, eoc.progressModalOptions, ...args) :
        Operations.API.executeLite(toLite(eoc.entity), eoc.operationInfo.key, ...args);

      return promise
        .then(eoc.onExecuteSuccess ?? eoc.onExecuteSuccess_Default)
        .catch(ifError(ValidationError, e => eoc.frame.setError(e.modelState, "entity")));
    });
  }

  export function defaultDeleteEntity<T extends Entity>(eoc: EntityOperationContext<T>, ...args: any[]): Promise<void | undefined> {

    return confirmInNecessary(eoc).then(conf => {
      if (!conf)
        return;

      return Operations.API.deleteEntity(eoc.entity, eoc.operationInfo.key, ...args)
        .then(eoc.onDeleteSuccess ?? eoc.onDeleteSuccess_Default)
        .catch(ifError(ValidationError, e => eoc.frame.setError(e.modelState, "entity")));
    });
  }

  export function defaultDeleteLite<T extends Entity>(eoc: EntityOperationContext<T>, ...args: any[]): Promise<void | undefined> {

    return confirmInNecessary(eoc).then(conf => {
      if (!conf)
        return;

      return Operations.API.deleteLite(toLite(eoc.entity), eoc.operationInfo.key, ...args)
        .then(eoc.onDeleteSuccess ?? eoc.onDeleteSuccess_Default)
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
}

export type OutlineBsColor =
  | 'outline-primary'
  | 'tertiary'
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
  textInTitle?: boolean;
  avoidAlternatives?: boolean;
  onOperationClick?: (eoc: EntityOperationContext<any /*Entity*/>) => Promise<void>;
  children?: React.ReactNode;
  hideOnCanExecute?: boolean;
}


export function OperationButton({ group, onOperationClick, canExecute, eoc: eocOrNull, outline, color,
  avoidAlternatives, hideOnCanExecute, textInTitle, ...props }: OperationButtonProps): React.ReactElement | null {

  if (eocOrNull == null)
    return null;

  const eoc = eocOrNull;

  if (canExecute === undefined)
    canExecute = eoc.settings?.overrideCanExecute ? eoc.settings.overrideCanExecute(eoc) : eoc.canExecute;

  const disabled = !!canExecute || eoc.frame?.isExecuting();

  if (hideOnCanExecute && disabled)
    return null;

  var alternatives = avoidAlternatives ? undefined : eoc.alternatives?.filter(a => a.isVisible != false); //Clone

  var main: AlternativeOperationSetting<any> = {
    name: "main",
    onClick: eoc => {
      if (onOperationClick)
        return onOperationClick(eoc);
      else
        return eoc.click();
    },
    text: eoc.settings?.text ? eoc.settings.text(eoc) :
      group?.simplifyName ? group.simplifyName(eoc.operationInfo.niceName) :
        eoc.operationInfo.niceName,
    keyboardShortcut: eoc.keyboardShortcut,
    color: color ?? eoc.color,
    classes: classes(props.className, eoc.settings?.classes),
    icon: eoc.icon,
    iconColor: eoc.settings?.iconColor,
    iconAlign: eoc.settings?.iconAlign,
  };

  if (alternatives && alternatives.some(a => a.isDefault)) {
    const newMain = alternatives.single(a => a.isDefault == true);
    main.inDropdown = newMain.inDropdown;
    alternatives.remove(newMain);
    alternatives.insertAt(0, main);
    main = newMain;
  }

  if (group) {

    const item =
      <Dropdown.Item
        {...props as any}
        disabled={disabled}
        title={props.title ?? (main?.keyboardShortcut && Operations.getShortcutToString(main.keyboardShortcut))}
        className={classes(disabled ? "disabled sf-pointer-events" : undefined, main?.classes, (main.color ? "text-" + main.color : undefined))}
        onClick={disabled ? undefined : e => { eoc.event = e; main.onClick(eoc); }}
        data-operation={eoc.operationInfo.key}>
        {props.children ?? EntityOperations.withIcon(main.text, main.icon, main.iconColor, main.iconAlign)}
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

  var button = <Button variant={(outline ? ("outline-" + main.color) as OutlineBsColor : main.color)}
    {...props}
    key="button"
    title={[(textInTitle ? main.text : undefined), main.keyboardShortcut && Operations.getShortcutToString(main.keyboardShortcut)].notNull().join(" ")}
    className={classes(disabled ? "disabled" : undefined, main.classes)}
    onClick={disabled ? undefined : e => { eoc.event = e; main.onClick(eoc); }}
    data-operation={eoc.operationInfo.key}>
    {props.children ?? EntityOperations.withIcon(main.text, main.icon, main.iconColor, main.iconAlign)}
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
              variant={(outline ? ("outline-" + (color ?? main.color)) as OutlineBsColor : (color ?? main.color))}
              className={classes("dropdown-toggle-split px-1", disabled ? "disabled" : undefined, aos.classes)}
              onClick={() => aos.onClick(eoc)}
              title={aos.text + (aos.keyboardShortcut ? (" (" + Operations.getShortcutToString(aos.keyboardShortcut) + ")") : "")}>
              <small>{React.isValidElement(aos.icon) ? aos.icon : <FontAwesomeIcon aria-hidden={true} icon={aos.icon!} color={aos.iconColor} className="fa-fw" />}</small>
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
      <Dropdown.Menu align="start">
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
        title={aos.keyboardShortcut && Operations.getShortcutToString(aos.keyboardShortcut)}
        onClick={() => aos.onClick(eoc!)}
        data-alternative={aos.name}>
        {EntityOperations.withIcon(aos.text, aos.icon, aos.iconColor, aos.iconAlign)}
      </Dropdown.Item>
    );
  }
}
