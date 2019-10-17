import * as React from "react";
import { BsColor } from "./Basic";
import { IconProp } from "@fortawesome/fontawesome-svg-core";
import { classes } from "../Globals";
import { JavascriptMessage } from "../Signum.Entities";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";

interface ModalIconProps {
  color?: BsColor;
  classes?: string;
  icon?: IconProp;
  iconAlign?: string;
}

interface ModalHeaderButtonsProps {
  onClose?: () => void;
  onOk?: () => void;
  okDisabled?: boolean;
  onCancel?: () => void;
  closeBeforeTitle?: boolean;
  htmlAttributes?: React.HTMLAttributes<HTMLDivElement>;
  okButtonProps?: ModalIconProps;
  closeButtonProps?: ModalIconProps;
  children?: React.ReactNode;
}

export function ModalHeaderButtons(p: ModalHeaderButtonsProps) {

  var close = p.onClose &&
    <button type="button" className="close" aria-label="Close" onClick={p.onClose}>
      <span aria-hidden="true">×</span>
    </button>;

  return (
    <div className="modal-header" {...p.htmlAttributes}>
      {p.closeBeforeTitle && close}
      <h4 className="modal-title" >
        {p.children}
      </h4>
      {!p.closeBeforeTitle && close}
      {(p.onCancel || p.onOk) &&
        <div className="btn-toolbar" style={{ flexWrap: "nowrap" }}>
          {p.onOk && <button
            className={classes("btn", "btn-" + ((p.okButtonProps && p.okButtonProps.color) || "primary"), "sf-entity-button sf-close-button sf-ok-button", p.okButtonProps && p.okButtonProps.classes)}
            disabled={p.okDisabled} onClick={p.onOk}>
            {renderButton(JavascriptMessage.ok.niceToString(), p.okButtonProps)}
          </button>
          }
          {p.onCancel && <button
            className={classes("btn", "btn-" + ((p.closeButtonProps && p.closeButtonProps.color) || "light"), "sf-entity-button sf-close-button sf-cancel-button", p.closeButtonProps && p.closeButtonProps.classes)}
            onClick={p.onCancel}>
            {renderButton(JavascriptMessage.cancel.niceToString(), p.closeButtonProps)}
          </button>
          }
        </div>
      }
    </div>
  );

  function renderButton(text: string, mip?: ModalIconProps) {
    if (mip && mip.icon) {
      switch (mip.iconAlign) {
        case "right": return (<span>{text} <FontAwesomeIcon icon={mip.icon} fixedWidth /></span>);
        default: return (<span><FontAwesomeIcon icon={mip.icon} fixedWidth /> {text}</span>);
      }
    }
    else
      return text;
  }
}
