import * as React from "react"
import { Entity, toLite, JavascriptMessage, OperationMessage, getToString, NormalControlMessage, NormalWindowMessage, EntityPack, ModifiableEntity } from '../Signum.Entities';
import { getTypeInfo, OperationType, GraphExplorer } from '../Reflection';
import { classes, ifError } from '../Globals';
import { ButtonsContext, IOperationVisible, ButtonBarElement } from '../TypeContext';
import * as Navigator from '../Navigator';
import Notify from '../Frames/Notify';
import MessageModal from '../Modals/MessageModal'
import { ValidationError } from '../Services';
import {
  operationInfos, getSettings, EntityOperationSettings, EntityOperationContext, EntityOperationGroup,
  CreateGroup, API, isEntityOperation, AlternativeOperationSetting, getShortcutToString, isOperationAllowed
} from '../Operations'
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { IconProp } from "@fortawesome/fontawesome-svg-core";
import * as Constructor from "../Constructor"
import { func } from "prop-types";
import { Dropdown, ButtonProps, DropdownButton, Button, OverlayTrigger, Tooltip, ButtonGroup } from "react-bootstrap";
import { BsColor } from "../Components";


export function getEntityOperationButtons(ctx: ButtonsContext): Array<ButtonBarElement | undefined > | undefined {
  const ti = getTypeInfo(ctx.pack.entity.Type);

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
    .filter(eoc => {
      if (ctx.isOperationVisible && !ctx.isOperationVisible(eoc))
        return false;

      var ov = ctx.frame.entityComponent as any as IOperationVisible;
      if (ov?.isOperationVisible && !ov.isOperationVisible(eoc))
        return false;

      var eos = eoc.settings;
      if (eos?.isVisible && !eos.isVisible(eoc))
        return false;

      if (eos?.hideOnCanExecute && eoc.canExecute)
        return false;

      if (Navigator.isReadOnly(ctx.pack, true) && !(eos?.showOnReadOnly))
        return false;

      return true;
    })
    .map(eoc => eoc!);

  operations.forEach(eoc => eoc.complete());

  const groups = operations.groupBy(eoc => eoc.group && eoc.group.key || "");

  const result = groups.flatMap((gr, i) => {
    if (gr.key == "") {
      return gr.elements.map((eoc, j) => ({
        order: eoc.settings && eoc.settings.order != undefined ? eoc.settings.order : 0,
        shortcut: e => eoc.onKeyDown(e),
        button: <OperationButton eoc={eoc} key={i + "-" + j} />,
      }) as ButtonBarElement);
    } else {

      const group = gr.elements[0].group!;
      
      return [{
        order: group.order != undefined ? group.order : 100,
        shortcut: e => gr.elements.some(eoc => eoc.onKeyDown(e)),
        button: (
          <DropdownButton title={group.text()} data-key={group.key} key={i} id={group.key} variant={group.color || "light"}>
            {
              gr.elements
                .orderBy(a => a.settings && a.settings.order)
                .map((eoc, j) => <OperationButton eoc={eoc} key={j} group={group} />)
            }
          </DropdownButton>
        )
      } as ButtonBarElement];
    }
  });

  return result;
}

export function andClose<T extends Entity>(eoc: EntityOperationContext<T>, inDropdown?: boolean): AlternativeOperationSetting<T> {
  
  return ({
    name: "andClose",
    text: () => OperationMessage._0AndClose.niceToString(eoc.textOrNiceName()),
    icon: "times",
    keyboardShortcut: eoc.keyboardShortcut && { shiftKey: true, ...eoc.keyboardShortcut },
    isVisible: true,
    inDropdown: inDropdown,
    onClick: () => {
      eoc.onExecuteSuccess = pack => {
        notifySuccess();
        eoc.frame.onClose(pack);
      };
      eoc.click();
    }
  });
}

export function andNew<T extends Entity>(eoc: EntityOperationContext<T>, inDropdown?: boolean): AlternativeOperationSetting<T> {

  return ({
    name: "andNew",
    text: () => OperationMessage._0AndNew.niceToString(eoc.textOrNiceName()),
    icon: "plus",
    keyboardShortcut: eoc.keyboardShortcut && { altKey: true, ...eoc.keyboardShortcut },
    isVisible: eoc.frame!.allowChangeEntity && Navigator.isCreable(eoc.entity.Type, true, true),
    inDropdown: inDropdown,
    onClick: () => {
      eoc.onExecuteSuccess = pack => {
        notifySuccess();

        var createNew = eoc.frame.frameComponent.createNew;

        if (createNew)
          (createNew() ?? Promise.resolve(undefined))
            .then(newPack => newPack && eoc.frame.onReload(newPack, true))
            .done();
        else
          Constructor.constructPack(pack.entity.Type)
            .then(newPack => newPack && eoc.frame.onReload(newPack, true))
            .done();
      };
      eoc.defaultClick();
    }
  });
}

interface OperationButtonProps extends ButtonProps {
  eoc: EntityOperationContext<any /*Entity*/>;
  group?: EntityOperationGroup;
  variant?: BsColor;
  canExecute?: string | null;
  className?: string;
  onOperationClick?: (eoc: EntityOperationContext<any /*Entity*/>) => void;
  children?: React.ReactNode
}

export function OperationButton({ eoc, group, onOperationClick, canExecute, ...props }: OperationButtonProps): React.ReactElement<any> | null {

  if (!isOperationAllowed(eoc.operationInfo.key, (eoc.entity as Entity).Type))
    return null;

  if (canExecute === undefined)
    canExecute = eoc.canExecute;

  const disabled = !!canExecute;

  var alternatives = eoc.alternatives && eoc.alternatives.filter(a => a.isVisible != false);

  if (group) {

    const item =
      <Dropdown.Item
        {...props}
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
        {item},
        alternatives?.map(a => renderAlternative(a))
      </>
    );
  }    

  var button = <Button variant={eoc.color}
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
            <Button key={i} color={eoc.color}
              className={classes("dropdown-toggle-split px-1", disabled ? "disabled" : undefined, aos.classes)}
              onClick={() => aos.onClick(eoc)}
              title={aos.text() + (aos.keyboardShortcut ? (" (" + getShortcutToString(aos.keyboardShortcut) + ")") : "")}>
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
      <Dropdown.Menu alignRight>
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
        onClick={() => aos.onClick(eoc)}
        data-alternative={aos.name}>
        {withIcon(aos.text(), aos.icon, aos.iconColor, aos.iconAlign)}
      </Dropdown.Item>
    );
  }

  function renderChildren() {
    if (props.children)
      return props.children;

    let text: string = eoc.settings && eoc.settings.text ? eoc.settings.text() :
      group?.simplifyName ? group.simplifyName(eoc.operationInfo.niceName) :
        eoc.operationInfo.niceName;

    const s = eoc.settings;
    return withIcon(text, s?.icon, s?.iconColor, s?.iconAlign);
  }

  function handleOnClick(event: React.MouseEvent<any>) {
    eoc.event = event;
    event.persist();

    if (onOperationClick)
      onOperationClick(eoc);
    else
      eoc.click();
  }
}

function withIcon(text: string, icon?: IconProp, iconColor?: string, iconAlign?: "start" | "end") {
  if (icon) {
    switch (iconAlign) {
      case "end": return (<span>{text} <FontAwesomeIcon icon={icon} color={iconColor} fixedWidth /></span>);
      default: return (<span><FontAwesomeIcon icon={icon} color={iconColor} fixedWidth /> {text}</span>);
    }
  }
  else {
    return text;
  }
}

export function defaultOnClick<T extends Entity>(eoc: EntityOperationContext<T>, ...args: any[]) {
  if (!eoc.operationInfo.canBeModified) {
    switch (eoc.operationInfo.operationType) {
      case OperationType.ConstructorFrom: defaultConstructFromLite(eoc, ...args); return;
      case OperationType.Execute: defaultExecuteLite(eoc, ...args); return;
      case OperationType.Delete: defaultDeleteLite(eoc, ...args); return;
    }
  } else {
    switch (eoc.operationInfo.operationType) {
      case OperationType.ConstructorFrom: defaultConstructFromEntity(eoc, ...args); return;
      case OperationType.Execute: defaultExecuteEntity(eoc, ...args); return;
      case OperationType.Delete: defaultDeleteEntity(eoc, ...args); return;
    }
  }

  throw new Error("Unexpected OperationType");
}

export function notifySuccess() {
  Notify.singleton && Notify.singleton.notifyTimeout({ text: JavascriptMessage.executed.niceToString(), type: "success" });
}

export function defaultConstructFromEntity<T extends Entity>(eoc: EntityOperationContext<T>, ...args: any[]) {

  confirmInNecessary(eoc).then(conf => {
    if (!conf)
      return;

    API.constructFromEntity(eoc.entity, eoc.operationInfo.key, ...args)
      .then(eoc.onConstructFromSuccess ?? (pack => {
        notifySuccess();
        Navigator.createNavigateOrTab(pack, eoc.event!);
      }))
      .catch(ifError(ValidationError, e => eoc.frame.setError(e.modelState, "entity")))
      .done();
  }).done();
}

export function defaultConstructFromLite<T extends Entity>(eoc: EntityOperationContext<T>, ...args: any[]) {

  confirmInNecessary(eoc).then(conf => {
    if (!conf)
      return;

    API.constructFromLite(toLite(eoc.entity), eoc.operationInfo.key, ...args)
      .then(eoc.onConstructFromSuccess ?? (pack => {
        notifySuccess();
        Navigator.createNavigateOrTab(pack, eoc.event!);
      }))
      .catch(ifError(ValidationError, e => eoc.frame.setError(e.modelState, "entity")))
      .done();
  }).done();
}


export function defaultExecuteEntity<T extends Entity>(eoc: EntityOperationContext<T>, ...args: any[]) {

  confirmInNecessary(eoc).then(conf => {
    if (!conf)
      return;

    API.executeEntity(eoc.entity, eoc.operationInfo.key, ...args)
      .then(eoc.onExecuteSuccess ?? (pack => {
        eoc.frame.onReload(pack);
        notifySuccess();
      }))
      .catch(ifError(ValidationError, e => eoc.frame.setError(e.modelState, "entity")))
      .done();
  }).done();
}

export function defaultExecuteLite<T extends Entity>(eoc: EntityOperationContext<T>, ...args: any[]) {

  confirmInNecessary(eoc).then(conf => {
    if (!conf)
      return;

    API.executeLite(toLite(eoc.entity), eoc.operationInfo.key, ...args)
      .then(eoc.onExecuteSuccess ?? (pack => {
        eoc.frame.onReload(pack);
        notifySuccess();
      }))
      .catch(ifError(ValidationError, e => eoc.frame.setError(e.modelState, "entity")))
      .done();
  }).done();
}

export function defaultDeleteEntity<T extends Entity>(eoc: EntityOperationContext<T>, ...args: any[]) {

  confirmInNecessary(eoc).then(conf => {
    if (!conf)
      return;

    API.deleteEntity(eoc.entity, eoc.operationInfo.key, ...args)
      .then(eoc.onDeleteSuccess ?? (() => {
        eoc.frame.onClose();
        notifySuccess();
      }))
      .catch(ifError(ValidationError, e => eoc.frame.setError(e.modelState, "entity")))
      .done();
  }).done();
}

export function defaultDeleteLite<T extends Entity>(eoc: EntityOperationContext<T>, ...args: any[]) {

  confirmInNecessary(eoc).then(conf => {
    if (!conf)
      return;

    API.deleteLite(toLite(eoc.entity), eoc.operationInfo.key, ...args)
      .then(eoc.onDeleteSuccess ?? (() => {
        eoc.frame.onClose();
        notifySuccess();
      }))
      .catch(ifError(ValidationError, e => eoc.frame.setError(e.modelState, "entity")))
      .done();
  }).done();
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

  if (eoc.settings && eoc.settings.confirmMessage != undefined)
    return eoc.settings.confirmMessage(eoc);

  //eoc.settings.confirmMessage === undefined
  if (eoc.operationInfo.operationType == OperationType.Delete)
    return OperationMessage.PleaseConfirmYouDLikeToDeleteTheEntityFromTheSystem.niceToString(getToString(eoc.entity));

  return undefined;
}
