import * as React from "react"
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { Entity, JavascriptMessage, OperationMessage, SearchMessage, Lite, External } from '../Signum.Entities';
import { getTypeInfo, OperationType } from '../Reflection';
import { classes } from '../Globals';
import * as Navigator from '../Navigator';
import MessageModal from '../Modals/MessageModal'
import {
  operationInfos, getSettings, notifySuccess, API, Defaults, CellOperationContext, EntityOperationSettings
} from '../Operations'
import * as Operations from "../Operations";
import { IconProp } from "@fortawesome/fontawesome-svg-core";
import { Button, ButtonProps, Dropdown, OverlayTrigger, Tooltip } from "react-bootstrap";
import { MultiPropertySetterModal, PropertySetterComponentProps } from "./MultiPropertySetter";
import { BsColor } from "../Components";
import Exception from "../Exceptions/Exception";
import { OutlineBsColor, withIcon } from "./EntityOperations";


export interface CellOperationProps extends ButtonProps {
  icoc: CellOperationContext;
  onOperationClick?: (coc: CellOperationContext) => Promise<void>;
  variant?: BsColor;
  canExecute?: string | null;
  className?: string;
  children?: React.ReactNode;
  color?: BsColor;
  icon?: IconProp;
  iconColor?: string;
  iconAlign?: "start" | "end";
  outline?: boolean;
}

export function CellOperationButton({ icoc: icocOrNull, onOperationClick, canExecute, outline, color, icon, iconColor, iconAlign, ...props }: CellOperationProps) {

  if (icocOrNull == null)
    return null;

  const icoc = icocOrNull;

  if (canExecute === undefined)
    canExecute = icoc.settings?.overrideCanExecute ? icoc.settings.overrideCanExecute(icoc) : icoc.canExecute;

  const disabled = !!canExecute;

  const entityOperationSettings = getSettings(icoc.operationInfo.key) as EntityOperationSettings<Entity> | undefined;

  const hideOnCanExecute = icoc.settings?.hideOnCanExecute ? entityOperationSettings?.hideOnCanExecute : false;

  if (hideOnCanExecute && disabled)
    return null;

  if (color == null)
    color = icoc.settings?.color ?? entityOperationSettings?.color ?? Defaults.getColor(icoc.operationInfo);

  if (icon == null)
    icon = coalesceIcon(icoc.settings?.icon, entityOperationSettings?.icon);

  if (iconColor == null)
    iconColor = icoc.settings?.iconColor || entityOperationSettings?.iconColor;

  if (outline == null)
    outline = icoc.outline ?? icoc.settings?.outline;

  if (iconAlign == null)
    iconAlign = icoc.iconAlign ?? icoc.settings?.iconAlign;

  const operationClickOrDefault = onOperationClick ?? icoc.settings?.onClick ?? defaultCellOperationClick

  const handleOnClick = (me: React.MouseEvent<any>) => {
    icoc.event = me;
    operationClickOrDefault(icoc)
      .done();
  }

  var button = <Button variant={(outline ? ("outline-" + color) as OutlineBsColor : color)}
    {...props}
    key="button"
    //title={icoc.operationInfo.niceName}
    className={classes(disabled ? "disabled" : undefined, props?.className, icoc.settings && icoc.settings.classes)}
    onClick={disabled ? undefined : handleOnClick}
    data-operation={icoc.operationInfo.key}>
    {renderChildren()}
  </Button>;

  if (canExecute) {
    return (
      <OverlayTrigger overlay={<Tooltip id={icoc.operationInfo.key + "_tooltip"} placement={"bottom"}>{canExecute}</Tooltip>}>
        {button}
      </OverlayTrigger>
    );
  }

  return button;

  function renderChildren() {
    if (props.children)
      return props.children;

    let text: string = icoc.settings?.text ? icoc.settings.text(icoc) : icoc.operationInfo.niceName;

    return withIcon(text, icon, iconColor, iconAlign);
  }
}

CellOperationButton.getText = (icoc: CellOperationContext): React.ReactNode => {

  if (icoc.settings && icoc.settings.text)
    return icoc.settings.text(icoc);

  return <>{CellOperationButton.simplifyName(icoc.operationInfo.niceName)}{icoc.operationInfo.canBeModified ? <small className="ms-2">{OperationMessage.MultiSetter.niceToString()}</small> : null}</>;

};

CellOperationButton.simplifyName = (niceName: string) => {
  const array = new RegExp(OperationMessage.CreateFromRegex.niceToString()).exec(niceName);
  return array ? (niceName.tryBefore(array[1]) ?? "") + array[1].firstUpper() : niceName;
}

function confirmInNecessary(coc: CellOperationContext): Promise<boolean> {

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

function getConfirmMessage(coc: CellOperationContext) {
  if (coc.settings && coc.settings.confirmMessage === null)
    return undefined;

  if (coc.settings && coc.settings.confirmMessage != undefined)
    return coc.settings.confirmMessage(coc);

  if (coc.operationInfo.operationType == "Delete") {
    const lite = coc.lite;
    if (lite) {
      return OperationMessage.PleaseConfirmYouWouldLikeToDelete0FromTheSystem.niceToString().formatHtml(<strong>{lite.toStr} ({getTypeInfo(lite.EntityType).niceName} {lite.id})</strong>);;
    }
  }

  return undefined;
}


export function defaultCellOperationClick(icoc: CellOperationContext, ...args: any[]) : Promise<void> {

  icoc.event!.persist();

  return confirmInNecessary(icoc).then(conf => {
    if (!conf)
      return;

    switch (icoc.operationInfo.operationType) {
      case "ConstructorFromMany":
          throw new Error("ConstructorFromMany operation can not be in column");

      case "ConstructorFrom":
        if (icoc.lite) {
          return API.constructFromLite(icoc.lite, icoc.operationInfo.key, ...args)
            .then(icoc.onConstructFromSuccess ?? (pack => {
              if (pack?.entity.id != null)
                Navigator.raiseEntityChanged(pack.entity);
              notifySuccess();
              return Navigator.createNavigateOrTab(pack, icoc.event!)
                .then(() => { })
            }));
        };
      case "Execute":
        return API.executeLite(icoc.lite, icoc.operationInfo.key, ...args)
            .then(icoc.onExecuteSuccess ?? (pack => {
              icoc.raiseEntityChanged();
              notifySuccess();              
            }));
      case "Delete":
        return API.deleteLite(icoc.lite, icoc.operationInfo.key, ...args)
            .then(icoc.onDeleteSuccess ?? (() => {
              icoc.raiseEntityChanged();
              notifySuccess();
            }));
    }
  });

}


export function coalesceIcon(icon: IconProp | undefined, icon2: IconProp | undefined): IconProp | undefined{ //Till the error is fixed

  if (icon === null)
    return undefined;

  if (icon === undefined)
    return icon2

  return icon;
}

