import * as React from "react"
import { Entity, toLite, JavascriptMessage, OperationMessage, getToString, NormalControlMessage, NormalWindowMessage } from '../Signum.Entities';
import { getTypeInfo, OperationType, GraphExplorer } from '../Reflection';
import { classes, ifError } from '../Globals';
import { ButtonsContext, IOperationVisible } from '../TypeContext';
import * as Navigator from '../Navigator';
import * as OrderUtils from '../Frames/OrderUtils';
import Notify from '../Frames/Notify';
import MessageModal from '../Modals/MessageModal'
import { ValidationError } from '../Services';
import {
  operationInfos, getSettings, EntityOperationSettings, EntityOperationContext, EntityOperationGroup,
  CreateGroup, API, isEntityOperation, autoColorFunction, isSave, AlternativeOperationSetting
} from '../Operations'
import { UncontrolledDropdown, DropdownMenu, DropdownToggle, DropdownItem, UncontrolledTooltip, Button, Dropdown } from "../Components";
import { TitleManager } from "../../Scripts/Lines/EntityBase";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { ButtonProps } from "../Components/Button";
import { IconProp } from "@fortawesome/fontawesome-svg-core";
import * as Constructor from "../Constructor"


export function getEntityOperationButtons(ctx: ButtonsContext): Array<React.ReactElement<any> | undefined> | undefined {
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
      if (ov && ov.isOperationVisible && !ov.isOperationVisible(eoc))
        return false;

      var eos = eoc.settings;
      if (eos && eos.isVisible && !eos.isVisible(eoc))
        return false;

      if (eos && eos.hideOnCanExecute && eoc.canExecute)
        return false;

      return true;
    })
    .map(eoc => eoc!);

  const groups = operations.groupBy(eoc => {

    const group = getGroup(eoc);

    if (group == undefined)
      return "";

    return group.key;
  });

  const result = groups.flatMap((gr, i) => {
    if (gr.key == "") {
      return gr.elements.map((eoc, j) => ({
        order: eoc.settings && eoc.settings.order != undefined ? eoc.settings.order : 0,
        button: <OperationButton eoc={eoc} key={i + "-" + j} />
      }));
    } else {

      const group = getGroup(gr.elements[0])!;


      return [{
        order: group.order != undefined ? group.order : 100,
        button: (
          <UncontrolledDropdown key={i}>
            <DropdownToggle data-key={group.key} color={group.color || "light"} className={group.cssClass} caret>
              {group.text()}
            </DropdownToggle>
            <DropdownMenu>
              {gr.elements
                .orderBy(a => a.settings && a.settings.order)
                .map((eoc, j) => <OperationButton eoc={eoc} key={j} group={group} />)
              }
            </DropdownMenu>
          </UncontrolledDropdown >
        )
      }];
    }
  });

  return result.map(a => OrderUtils.setOrder(a.order, a.button));
}

function getGroup(eoc: EntityOperationContext<Entity>) {
  if (eoc.settings != undefined && eoc.settings.group !== undefined) {
    return eoc.settings.group;
  }

  if (eoc.operationInfo.operationType == OperationType.ConstructorFrom)
    return CreateGroup;

  return undefined;
}

function getAlternatives(eoc: EntityOperationContext<Entity>): AlternativeOperationSetting<Entity>[] {
  let alternatives = eoc.settings && eoc.settings.alternatives;

  if (alternatives)
    return alternatives(eoc);

  if (isSave(eoc.operationInfo)) {
    return [
      andClose(eoc),
      andNew(eoc)
    ]
  }

  return [];
}

export function andClose<T extends Entity>(eoc: EntityOperationContext<T>): AlternativeOperationSetting<T> {
  
  return ({
    name: "andClose",
    text: () => OperationMessage._0AndClose.niceToString(eoc.textOrNiceName()),
    icon: "times",
    onClick: () => {
      eoc.onExecuteSuccess = pack => {
        eoc.frame.onReload(pack);
        notifySuccess();
        eoc.frame.onClose(true);
      };
      eoc.defaultClick();
    }
  });
}

export function andNew<T extends Entity>(eoc: EntityOperationContext<T>): AlternativeOperationSetting<T> {
  
  return ({
    name: "andNew",
    text: () => OperationMessage._0AndNew.niceToString(eoc.textOrNiceName()),
    icon: "plus",
    isVisible: eoc.frame!.allowChangeEntity && Navigator.isCreable(eoc.entity.Type, true, true),
    onClick: () => {
      eoc.onExecuteSuccess = pack => {
        notifySuccess();
        Constructor.construct(pack.entity.Type).then(newPack => {
          eoc.frame.onReload(newPack);
        }).done()
      };
      eoc.defaultClick();
    }
  });
}

interface OperationButtonProps extends ButtonProps {
  eoc: EntityOperationContext<any /*Entity*/>;
  group?: EntityOperationGroup;
  canExecute?: string | null;
  onOperationClick?: (eoc: EntityOperationContext<any /*Entity*/>) => void;
}

export class OperationButton extends React.Component<OperationButtonProps> {
  render() {
    let { eoc, group, onOperationClick, canExecute, color, ...props } = this.props;

    if (canExecute === undefined)
      canExecute = eoc.canExecute;

    const bsColor = eoc.settings && eoc.settings.color || autoColorFunction(eoc.operationInfo);

    const disabled = !!canExecute;

    let elem: HTMLElement | null;

    const tooltip = canExecute &&
      (
        <UncontrolledTooltip placement={group ? "right" : "bottom"} target={() => elem!} key="tooltip">
          {canExecute}
        </UncontrolledTooltip>
      );

    var alternatives = getAlternatives(eoc).filter(a => a.isVisible == true || a.isVisible == undefined);

    if (group) {
      var dr

      return [
        <DropdownItem
          {...props}
          key="di"
          innerRef={r => elem = r}
          disabled={disabled}
          onClick={disabled ? undefined : this.handleOnClick}
          data-operation={eoc.operationInfo.key}>
          {this.renderChildren()}
        </DropdownItem>,
        tooltip,
        tooltip == null && alternatives.map(a => this.renderAlternative(a))
      ];
    }


    var button = <Button color={bsColor}
      {...props}
      key="button"
      innerRef={r => elem = r}
      className={classes(disabled ? "disabled" : undefined, props && props.className, eoc.settings && eoc.settings.classes)}
      onClick={disabled ? undefined : this.handleOnClick}
      data-operation={eoc.operationInfo.key}>
      {this.renderChildren()}
    </Button>;

    if (tooltip)
      return [
        button,
        tooltip
      ];

    
    if (alternatives.length == 0)
      return button;

    return (
      <UncontrolledDropdown group>
        {button}
        <DropdownToggle caret split color={bsColor}/>
        <DropdownMenu right>
          {alternatives.map(a => this.renderAlternative(a))}
        </DropdownMenu>
      </UncontrolledDropdown>
    );
  }

  renderAlternative(aos: AlternativeOperationSetting<Entity>) {
    
    return (
      <DropdownItem
        color={aos.color}
        className={aos.classes}
        key={aos.name}
        onClick={() => aos.onClick(this.props.eoc)}
        data-alternative={aos.name}>
        {withIcon(aos.text(), aos.icon, aos.iconColor, aos.iconAlign)}
      </DropdownItem>
    );
  }

  renderChildren() {
    if (this.props.children)
      return this.props.children;

    let text: string;

    const eoc = this.props.eoc;
    if (eoc.settings && eoc.settings.text)
      text = eoc.settings.text();

    const group = this.props.group;
    if (group && group.simplifyName)
      text = group.simplifyName(eoc.operationInfo.niceName);

    text = eoc.operationInfo.niceName;

    const s = eoc.settings;
    return withIcon(text, s && s.icon, s && s.iconColor, s && s.iconAlign);
  }

  handleOnClick = (event: React.MouseEvent<any>) => {
    const eoc = this.props.eoc;
    eoc.event = event;
    event.persist();

    if (this.props.onOperationClick)
      this.props.onOperationClick(eoc);
    else if (eoc.settings && eoc.settings.onClick)
      eoc.settings.onClick(eoc);
    else
      defaultOnClick(eoc);
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
  Notify.singleton.notifyTimeout({ text: JavascriptMessage.executed.niceToString(), type: "success" });
}

export function defaultConstructFromEntity<T extends Entity>(eoc: EntityOperationContext<T>, ...args: any[]) {

  confirmInNecessary(eoc).then(conf => {
    if (!conf)
      return;

    API.constructFromEntity(eoc.entity, eoc.operationInfo.key, ...args)
      .then(eoc.onConstructFromSuccess || (pack => {
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
      .then(eoc.onConstructFromSuccess || (pack => {
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
      .then(eoc.onExecuteSuccess || (pack => {
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
      .then(eoc.onExecuteSuccess || (pack => {
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
      .then(eoc.onDeleteSuccess || (() => {
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
      .then(eoc.onDeleteSuccess || (() => {
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
